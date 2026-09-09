#if WINDOWS
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneTapHabits.Services.Firestore;

namespace OneTapHabits.Services.Firebase;

public sealed class FirebaseConfig
{
	public string ProjectId { get; init; } = "onetap-habits";

	public string ApiKey { get; init; } = string.Empty;

	public string WebClientId { get; init; } = string.Empty;

	public string? WebClientSecret { get; init; }

	public static FirebaseConfig Load()
	{
		try
		{
			using var stream = FileSystem.OpenAppPackageFileAsync("firebase-config.json").GetAwaiter().GetResult();
			using var reader = new StreamReader(stream);
			var json = reader.ReadToEnd();
			return JsonSerializer.Deserialize<FirebaseConfig>(json, JsonOptions) ?? new FirebaseConfig();
		}
		catch
		{
			return new FirebaseConfig();
		}
	}

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};
}

public sealed class WindowsFirebaseAuthGateway : IFirebaseAuthGateway
{
	private const string SessionPreferenceKey = "windows_firebase_session_json";

	private FirebaseUserInfo? _currentUser;
	private string? _idToken;
	private string? _refreshToken;

	public WindowsFirebaseAuthGateway()
	{
		RestoreSession();
	}

	public FirebaseUserInfo? CurrentUser => _currentUser;

	public string? IdToken => _idToken;

	internal void SetSession(FirebaseUserInfo user, string idToken, string refreshToken)
	{
		_currentUser = user;
		_idToken = idToken;
		_refreshToken = refreshToken;
		Preferences.Default.Set(SessionPreferenceKey, JsonSerializer.Serialize(new SessionState
		{
			Uid = user.Uid,
			Email = user.Email,
			IdToken = idToken,
			RefreshToken = refreshToken
		}));
	}

	public Task SignOutAsync(CancellationToken cancellationToken = default)
	{
		_currentUser = null;
		_idToken = null;
		_refreshToken = null;
		Preferences.Default.Remove(SessionPreferenceKey);
		return Task.CompletedTask;
	}

	internal async Task<string> GetValidIdTokenAsync(CancellationToken cancellationToken = default)
	{
		if (!string.IsNullOrEmpty(_idToken))
		{
			return _idToken;
		}

		if (string.IsNullOrEmpty(_refreshToken))
		{
			throw new InvalidOperationException("Not signed in.");
		}

		var config = FirebaseConfig.Load();
		using var http = new HttpClient();
		var response = await http.PostAsync(
			$"https://securetoken.googleapis.com/v1/token?key={config.ApiKey}",
			new FormUrlEncodedContent(new Dictionary<string, string>
			{
				["grant_type"] = "refresh_token",
				["refresh_token"] = _refreshToken
			}),
			cancellationToken);

		response.EnsureSuccessStatusCode();
		var payload = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>(cancellationToken: cancellationToken)
			?? throw new InvalidOperationException("Token refresh failed.");
		_idToken = payload.IdToken;
		_refreshToken = payload.RefreshToken;
		if (_currentUser is not null)
		{
			SetSession(_currentUser, _idToken, _refreshToken);
		}

		return _idToken;
	}

	private void RestoreSession()
	{
		var json = Preferences.Default.Get(SessionPreferenceKey, string.Empty);
		if (string.IsNullOrWhiteSpace(json))
		{
			return;
		}

		try
		{
			var state = JsonSerializer.Deserialize<SessionState>(json);
			if (state is null)
			{
				return;
			}

			_currentUser = new FirebaseUserInfo { Uid = state.Uid, Email = state.Email };
			_idToken = state.IdToken;
			_refreshToken = state.RefreshToken;
		}
		catch
		{
			Preferences.Default.Remove(SessionPreferenceKey);
		}
	}

	private sealed class SessionState
	{
		public string Uid { get; set; } = string.Empty;
		public string? Email { get; set; }
		public string IdToken { get; set; } = string.Empty;
		public string RefreshToken { get; set; } = string.Empty;
	}

	private sealed class RefreshTokenResponse
	{
		public string IdToken { get; set; } = string.Empty;
		public string RefreshToken { get; set; } = string.Empty;
	}
}

public sealed class WindowsFirestoreGateway : IFirestoreGateway
{
	private readonly WindowsFirebaseAuthGateway _auth;
	private readonly HttpClient _http = new();

	public WindowsFirestoreGateway(WindowsFirebaseAuthGateway auth) => _auth = auth;

	public async Task<IReadOnlyList<FirestoreDocument<T>>> GetDocumentsAsync<T>(
		string collectionPath,
		CancellationToken cancellationToken = default)
		where T : class, new()
	{
		var token = await _auth.GetValidIdTokenAsync(cancellationToken);
		var config = FirebaseConfig.Load();
		var baseUrl =
			$"https://firestore.googleapis.com/v1/projects/{config.ProjectId}/databases/(default)/documents/{collectionPath}";

		var results = new List<FirestoreDocument<T>>();
		string? pageToken = null;

		do
		{
			var url = string.IsNullOrEmpty(pageToken)
				? baseUrl
				: $"{baseUrl}?pageToken={Uri.EscapeDataString(pageToken)}";

			using var request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			var response = await _http.SendAsync(request, cancellationToken);
			response.EnsureSuccessStatusCode();
			var payload = await response.Content.ReadFromJsonAsync<CollectionResponse>(cancellationToken: cancellationToken)
				?? new CollectionResponse();

			if (payload.Documents is not null)
			{
				foreach (var doc in payload.Documents)
				{
					results.Add(new FirestoreDocument<T>
					{
						Id = GetDocumentId(doc.Name),
						Data = DeserializeDocument<T>(doc)
					});
				}
			}

			pageToken = payload.NextPageToken;
		}
		while (!string.IsNullOrEmpty(pageToken));

		return results;
	}

	public async Task SetDocumentAsync<T>(
		string documentPath,
		T data,
		CancellationToken cancellationToken = default)
		where T : class
	{
		var token = await _auth.GetValidIdTokenAsync(cancellationToken);
		var config = FirebaseConfig.Load();
		var url = $"https://firestore.googleapis.com/v1/projects/{config.ProjectId}/databases/(default)/documents/{documentPath}";
		using var request = new HttpRequestMessage(HttpMethod.Patch, url);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
		request.Content = JsonContent.Create(BuildFields(data));
		var response = await _http.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}

	public async Task DeleteDocumentAsync(string documentPath, CancellationToken cancellationToken = default)
	{
		var token = await _auth.GetValidIdTokenAsync(cancellationToken);
		var config = FirebaseConfig.Load();
		var url = $"https://firestore.googleapis.com/v1/projects/{config.ProjectId}/databases/(default)/documents/{documentPath}";
		using var request = new HttpRequestMessage(HttpMethod.Delete, url);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
		var response = await _http.SendAsync(request, cancellationToken);
		response.EnsureSuccessStatusCode();
	}

	private static string GetDocumentId(string fullName)
	{
		var lastSlash = fullName.LastIndexOf('/');
		return lastSlash < 0 ? fullName : fullName[(lastSlash + 1)..];
	}

	private static object BuildFields<T>(T data) => data switch
	{
		HabitFirestoreDto habit => new { fields = HabitFields(habit) },
		LogFirestoreDto log => new { fields = LogFields(log) },
		Dictionary<string, object> dict => new { fields = dict },
		_ => throw new NotSupportedException($"Unsupported Firestore document type: {typeof(T).Name}")
	};

	private static T DeserializeDocument<T>(DocumentResponse doc) where T : class, new()
	{
		if (typeof(T) == typeof(HabitFirestoreDto))
		{
			return (T)(object)ReadHabit(doc.Fields);
		}

		if (typeof(T) == typeof(LogFirestoreDto))
		{
			return (T)(object)ReadLog(doc.Fields);
		}

		if (typeof(T) == typeof(Dictionary<string, object>))
		{
			return (T)(object)new Dictionary<string, object>();
		}

		return new T();
	}

	private static Dictionary<string, object> HabitFields(HabitFirestoreDto dto) => new()
	{
		["name"] = Field(dto.Name),
		["color_hex"] = Field(dto.ColorHex),
		["show_in_widget"] = Field(dto.ShowInWidget),
		["target_days"] = ArrayField(dto.TargetDays.Select(i => (long)i)),
		["schedule_mode"] = Field((long)dto.ScheduleMode),
		["times_per_week"] = Field((long)dto.TimesPerWeek),
		["times_per_day"] = Field((long)dto.TimesPerDay),
		["sort_order"] = Field((long)dto.SortOrder),
		["reminder_enabled"] = Field(dto.ReminderEnabled),
		["reminder_time"] = Field(dto.ReminderTime ?? string.Empty),
		["created_at"] = Field(dto.CreatedAt),
		["is_active"] = Field(dto.IsActive)
	};

	private static Dictionary<string, object> LogFields(LogFirestoreDto dto) => new()
	{
		["habit_id"] = Field(dto.HabitId),
		["date"] = Field(dto.Date),
		["is_completed"] = Field(dto.IsCompleted),
		["count"] = Field((long)dto.Count)
	};

	private static HabitFirestoreDto ReadHabit(Dictionary<string, JsonElement>? fields)
	{
		return new HabitFirestoreDto
		{
			Name = ReadString(fields, "name"),
			ColorHex = ReadString(fields, "color_hex"),
			ShowInWidget = ReadBool(fields, "show_in_widget"),
			TargetDays = ReadIntList(fields, "target_days"),
			ScheduleMode = ReadInt(fields, "schedule_mode"),
			TimesPerWeek = ReadInt(fields, "times_per_week"),
			TimesPerDay = ReadInt(fields, "times_per_day"),
			SortOrder = ReadInt(fields, "sort_order"),
			ReminderEnabled = ReadBool(fields, "reminder_enabled"),
			ReminderTime = ReadString(fields, "reminder_time"),
			CreatedAt = ReadString(fields, "created_at"),
			IsActive = ReadBool(fields, "is_active")
		};
	}

	private static LogFirestoreDto ReadLog(Dictionary<string, JsonElement>? fields) => new()
	{
		HabitId = ReadString(fields, "habit_id"),
		Date = ReadString(fields, "date"),
		IsCompleted = ReadBool(fields, "is_completed"),
		Count = ReadInt(fields, "count")
	};

	private static Dictionary<string, object> Field(string value) => new() { ["stringValue"] = value };
	private static Dictionary<string, object> Field(bool value) => new() { ["booleanValue"] = value };
	private static Dictionary<string, object> Field(long value) => new() { ["integerValue"] = value.ToString() };
	private static Dictionary<string, object> ArrayField(IEnumerable<long> values) => new()
	{
		["arrayValue"] = new Dictionary<string, object>
		{
			["values"] = values.Select(v => Field(v)).ToArray()
		}
	};

	private static string ReadString(Dictionary<string, JsonElement>? fields, string key) =>
		fields is not null && fields.TryGetValue(key, out var value) && value.TryGetProperty("stringValue", out var s)
			? s.GetString() ?? string.Empty
			: string.Empty;

	private static bool ReadBool(Dictionary<string, JsonElement>? fields, string key) =>
		fields is not null && fields.TryGetValue(key, out var value) && value.TryGetProperty("booleanValue", out var b)
			&& b.GetBoolean();

	private static int ReadInt(Dictionary<string, JsonElement>? fields, string key)
	{
		if (fields is null || !fields.TryGetValue(key, out var value))
		{
			return 0;
		}

		if (value.TryGetProperty("integerValue", out var i) && int.TryParse(i.GetString(), out var parsed))
		{
			return parsed;
		}

		return 0;
	}

	private static List<int> ReadIntList(Dictionary<string, JsonElement>? fields, string key)
	{
		var list = new List<int>();
		if (fields is null || !fields.TryGetValue(key, out var value) || !value.TryGetProperty("arrayValue", out var array))
		{
			return list;
		}

		if (!array.TryGetProperty("values", out var values))
		{
			return list;
		}

		foreach (var item in values.EnumerateArray())
		{
			if (item.TryGetProperty("integerValue", out var iv) && int.TryParse(iv.GetString(), out var number))
			{
				list.Add(number);
			}
		}

		return list;
	}

	private sealed class CollectionResponse
	{
		public List<DocumentResponse>? Documents { get; set; }

		[JsonPropertyName("nextPageToken")]
		public string? NextPageToken { get; set; }
	}

	private sealed class DocumentResponse
	{
		public string Name { get; set; } = string.Empty;
		public Dictionary<string, JsonElement>? Fields { get; set; }
	}
}
#endif
