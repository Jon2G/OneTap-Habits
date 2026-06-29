using OneTapHabits.Calendar;
using OneTapHabits.Models;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Services;

public sealed class LogService : ILogService
{
	private readonly IAuthService _auth;
	private readonly IFirebaseAuth _firebaseAuth;
	private readonly IFirebaseFirestore _firestore;
	private readonly ILocalGuestStore _guestStore;

	public LogService(
		IAuthService auth,
		IFirebaseAuth firebaseAuth,
		IFirebaseFirestore firestore,
		ILocalGuestStore guestStore)
	{
		_auth = auth;
		_firebaseAuth = firebaseAuth;
		_firestore = firestore;
		_guestStore = guestStore;
	}

	public async Task<HabitLog?> GetLogAsync(string habitId, DateOnly date)
	{
		if (_auth.IsGuest)
		{
			var guest = await _guestStore.LoadAsync();
			var dateKey = date.ToString("yyyy-MM-dd");
			var entry = guest.Logs.FirstOrDefault(l => l.HabitId == habitId && l.Date == dateKey);
			if (entry is null)
			{
				return null;
			}

			return new HabitLog
			{
				Id = HabitLog.CreateId(date, habitId),
				HabitId = habitId,
				Date = date,
				IsCompleted = entry.IsCompleted
			};
		}

		var id = HabitLog.CreateId(date, habitId);
		var snapshot = await CloudLogsCollection().GetDocument(id).GetDocumentSnapshotAsync<LogDto>();
		if (snapshot.Data is null)
		{
			return null;
		}

		return snapshot.Data.ToModel(id, habitId, date);
	}

	public async Task SetCompletedAsync(string habitId, DateOnly date, bool isCompleted)
	{
		if (_auth.IsGuest)
		{
			var guest = await _guestStore.LoadAsync();
			var dateKey = date.ToString("yyyy-MM-dd");
			guest.Logs.RemoveAll(l => l.HabitId == habitId && l.Date == dateKey);
			if (isCompleted)
			{
				guest.Logs.Add(new GuestLogEntry
				{
					HabitId = habitId,
					Date = dateKey,
					IsCompleted = true
				});
			}

			await _guestStore.SaveAsync(guest);
			return;
		}

		var id = HabitLog.CreateId(date, habitId);
		var dto = new LogDto
		{
			HabitId = habitId,
			Date = date.ToString("O"),
			IsCompleted = isCompleted
		};

		await CloudLogsCollection().GetDocument(id).SetDataAsync(dto);
	}

	public async Task<IReadOnlyDictionary<string, bool>> GetCompletionMapForDateAsync(DateOnly date)
	{
		if (_auth.IsGuest)
		{
			var guest = await _guestStore.LoadAsync();
			var dateKey = date.ToString("yyyy-MM-dd");
			return guest.Logs
				.Where(l => l.Date == dateKey && l.IsCompleted)
				.GroupBy(l => l.HabitId)
				.ToDictionary(g => g.Key, _ => true);
		}

		var prefix = date.ToString("yyyy-MM-dd");
		var snapshot = await CloudLogsCollection().GetDocumentsAsync<LogDto>();
		return snapshot.Documents
			.Where(d => d.Data is not null && d.Reference.Id.StartsWith(prefix, StringComparison.Ordinal))
			.ToDictionary(d => d.Data!.HabitId, d => d.Data!.IsCompleted);
	}

	public async Task<IReadOnlyList<HabitLog>> GetCompletedLogsInRangeAsync(
		DateOnly startInclusive,
		DateOnly endInclusive)
	{
		if (endInclusive < startInclusive)
		{
			return [];
		}

		if (_auth.IsGuest)
		{
			var guest = await _guestStore.LoadAsync();
			return GuestLogQuery.FilterCompletedInRange(guest, startInclusive, endInclusive);
		}

		var snapshot = await CloudLogsCollection().GetDocumentsAsync<LogDto>();
		return snapshot.Documents
			.Where(d => d.Data is not null && d.Data.IsCompleted)
			.Select(d => ToLogFromSnapshot(d.Reference.Id, d.Data!))
			.Where(l => l is not null && l.Date >= startInclusive && l.Date <= endInclusive)
			.Cast<HabitLog>()
			.ToList();
	}

	private static HabitLog? ToLogFromSnapshot(string id, LogDto dto)
	{
		if (TryParseDateFromLogId(id, out var dateFromId))
		{
			return dto.ToModel(id, dto.HabitId, dateFromId);
		}

		if (DateOnly.TryParse(dto.Date, out var parsedDate))
		{
			return dto.ToModel(id, dto.HabitId, parsedDate);
		}

		return null;
	}

	private static bool TryParseDateFromLogId(string id, out DateOnly date)
	{
		var separator = id.IndexOf('_', StringComparison.Ordinal);
		if (separator <= 0)
		{
			date = default;
			return false;
		}

		return DateOnly.TryParseExact(id[..separator], "yyyy-MM-dd", out date);
	}

	private ICollectionReference CloudLogsCollection()
	{
		var userId = _firebaseAuth.CurrentUser?.Uid
			?? throw new InvalidOperationException("User must be signed in.");

		return _firestore.GetCollection($"users/{userId}/logs");
	}

	private sealed class LogDto
	{
		public string HabitId { get; set; } = string.Empty;
		public string Date { get; set; } = string.Empty;
		public bool IsCompleted { get; set; }

		public HabitLog ToModel(string id, string habitId, DateOnly date) => new()
		{
			Id = id,
			HabitId = habitId,
			Date = date,
			IsCompleted = IsCompleted
		};
	}
}
