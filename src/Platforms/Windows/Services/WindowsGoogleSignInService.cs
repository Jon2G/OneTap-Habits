#if WINDOWS
using System.Net.Http.Json;
using System.Text.Json;
using OneTapHabits.Services;

using OneTapHabits.Services.Firebase;



namespace OneTapHabits.Platforms.Windows.Services;



public sealed class WindowsGoogleSignInService : IGoogleSignInService

{

	private const string RedirectUri = "http://127.0.0.1:53123/";

	private static readonly JsonSerializerOptions FirebaseJsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	private readonly WindowsFirebaseAuthGateway _authGateway;

	private readonly IDiagnosticLogService _diagnosticLog;



	public WindowsGoogleSignInService(

		WindowsFirebaseAuthGateway authGateway,

		IDiagnosticLogService diagnosticLog)

	{

		_authGateway = authGateway;

		_diagnosticLog = diagnosticLog;

	}



	public bool IsSupported

	{

		get

		{

			var config = FirebaseConfig.Load();

			return IsConfigured(config);

		}

	}



	public async Task AuthenticateAsync(CancellationToken cancellationToken = default)

	{

		var config = FirebaseConfig.Load();

		if (!IsConfigured(config))

		{

			throw new InvalidOperationException(

				"Google Sign-In is not configured. Add firebase-config.json with apiKey and webClientId.");

		}

		if (string.IsNullOrWhiteSpace(config.WebClientSecret))

		{

			throw new InvalidOperationException(

				"Google OAuth web client secret is not configured. Release builds need the GOOGLE_OAUTH_CLIENT_SECRET GitHub secret.");

		}



		_diagnosticLog.LogInfo("GoogleSignIn", "Launching Windows loopback OAuth flow.");

		string idToken;

		try

		{

			idToken = await WindowsGoogleOAuthLoopbackClient.GetIdTokenAsync(

				config.WebClientId,

				config.WebClientSecret,

				RedirectUri,

				cancellationToken);

		}

		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)

		{

			throw new OperationCanceledException("Google Sign-In timed out waiting for browser callback.");

		}



		_diagnosticLog.LogInfo("GoogleSignIn", "Google id_token acquired. Exchanging with Firebase.");



		using var http = new HttpClient();

		var payload = new

		{

			postBody = $"id_token={Uri.EscapeDataString(idToken)}&providerId=google.com",

			requestUri = "http://localhost",

			returnSecureToken = true,

			returnIdpCredential = true

		};



		var response = await http.PostAsJsonAsync(

			$"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={config.ApiKey}",

			payload,

			cancellationToken);



		var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

		if (!response.IsSuccessStatusCode)

		{

			_diagnosticLog.LogError(

				"GoogleSignIn",

				new InvalidOperationException(responseBody),

				"Firebase signInWithIdp failed.");

			throw new InvalidOperationException($"Firebase sign-in failed: {responseBody}");

		}



		var session = JsonSerializer.Deserialize<FirebaseSignInResponse>(responseBody, FirebaseJsonOptions)

			?? throw new InvalidOperationException("Firebase sign-in failed.");



		if (string.IsNullOrWhiteSpace(session.LocalId) || string.IsNullOrWhiteSpace(session.IdToken))

		{

			throw new InvalidOperationException("Firebase sign-in did not return a user.");

		}



		_authGateway.SetSession(

			new FirebaseUserInfo { Uid = session.LocalId, Email = session.Email },

			session.IdToken,

			session.RefreshToken ?? string.Empty);

		_diagnosticLog.LogInfo("GoogleSignIn", "Firebase session established.");

	}



	private static bool IsConfigured(FirebaseConfig config) =>

		!string.IsNullOrWhiteSpace(config.WebClientId) &&

		!string.IsNullOrWhiteSpace(config.ApiKey) &&

		!config.WebClientId.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) &&

		!config.ApiKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);



	private sealed class FirebaseSignInResponse

	{

		public string LocalId { get; set; } = string.Empty;

		public string? Email { get; set; }

		public string IdToken { get; set; } = string.Empty;

		public string? RefreshToken { get; set; }

	}

}

#endif

