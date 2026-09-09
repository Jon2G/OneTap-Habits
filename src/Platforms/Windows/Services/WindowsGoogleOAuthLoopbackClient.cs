#if WINDOWS
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneTapHabits.Platforms.Windows.Services;

internal static class WindowsGoogleOAuthLoopbackClient
{
	private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
	private static readonly TimeSpan CallbackTimeout = TimeSpan.FromMinutes(5);

	public static async Task<string> GetIdTokenAsync(
		string clientId,
		string? clientSecret,
		string redirectUri,
		CancellationToken cancellationToken)
	{
		var listenerPrefix = EnsureTrailingSlash(redirectUri);
		var codeVerifier = GenerateCodeVerifier();
		var codeChallenge = CreateCodeChallenge(codeVerifier);
		var state = GenerateState();
		var authUrl = BuildAuthorizationUrl(clientId, redirectUri, codeChallenge, state);

		using var listener = new HttpListener();
		listener.Prefixes.Add(listenerPrefix);
		listener.Start();

		try
		{
			OpenBrowser(new Uri(authUrl));

			using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			timeoutCts.CancelAfter(CallbackTimeout);

			var context = await listener.GetContextAsync().WaitAsync(timeoutCts.Token);
			await WriteSuccessResponseAsync(context.Response, cancellationToken);

			var query = ParseQueryString(context.Request.Url?.Query);
			if (query.TryGetValue("error", out var oauthError))
			{
				var description = query.GetValueOrDefault("error_description");
				throw new InvalidOperationException(
					$"Google OAuth error: {oauthError}. {description}".Trim());
			}

			if (!query.TryGetValue("state", out var returnedState) || returnedState != state)
			{
				throw new InvalidOperationException("Google OAuth state mismatch.");
			}

			if (!query.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
			{
				throw new InvalidOperationException("Google OAuth did not return an authorization code.");
			}

			return await ExchangeCodeForIdTokenAsync(
				clientId,
				clientSecret,
				redirectUri,
				code,
				codeVerifier,
				cancellationToken);
		}
		finally
		{
			listener.Stop();
			listener.Close();
		}
	}

	private static string BuildAuthorizationUrl(
		string clientId,
		string redirectUri,
		string codeChallenge,
		string state) =>
		"https://accounts.google.com/o/oauth2/v2/auth" +
		$"?client_id={Uri.EscapeDataString(clientId)}" +
		$"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
		"&response_type=code" +
		"&scope=openid%20email%20profile" +
		$"&state={Uri.EscapeDataString(state)}" +
		$"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
		"&code_challenge_method=S256" +
		"&access_type=online" +
		"&prompt=select_account";

	private static async Task<string> ExchangeCodeForIdTokenAsync(
		string clientId,
		string? clientSecret,
		string redirectUri,
		string code,
		string codeVerifier,
		CancellationToken cancellationToken)
	{
		var form = new Dictionary<string, string>
		{
			["code"] = code,
			["client_id"] = clientId,
			["redirect_uri"] = redirectUri,
			["grant_type"] = "authorization_code",
			["code_verifier"] = codeVerifier
		};

		if (!string.IsNullOrWhiteSpace(clientSecret))
		{
			form["client_secret"] = clientSecret;
		}

		using var http = new HttpClient();
		using var response = await http.PostAsync(
			TokenEndpoint,
			new FormUrlEncodedContent(form),
			cancellationToken);

		var payload = await response.Content.ReadAsStringAsync(cancellationToken);
		var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(payload)
			?? throw new InvalidOperationException("Google token exchange returned an empty response.");

		if (!response.IsSuccessStatusCode)
		{
			var error = tokenResponse.Error ?? response.ReasonPhrase ?? "unknown_error";
			var description = tokenResponse.ErrorDescription ?? payload;
			throw new InvalidOperationException($"Google token exchange failed: {error}. {description}");
		}

		if (string.IsNullOrWhiteSpace(tokenResponse.IdToken))
		{
			throw new InvalidOperationException("Google token exchange did not return an id_token.");
		}

		return tokenResponse.IdToken;
	}

	private static async Task WriteSuccessResponseAsync(HttpListenerResponse response, CancellationToken cancellationToken)
	{
		const string html = """
			<html>
			  <body style="font-family:Segoe UI,sans-serif;padding:24px;">
			    <h2>Signed in</h2>
			    <p>You can close this tab and return to OneTap Habits.</p>
			  </body>
			</html>
			""";
		var buffer = Encoding.UTF8.GetBytes(html);
		response.StatusCode = 200;
		response.ContentType = "text/html; charset=utf-8";
		response.ContentLength64 = buffer.Length;
		await response.OutputStream.WriteAsync(buffer, cancellationToken);
		response.OutputStream.Close();
	}

	private static void OpenBrowser(Uri authUrl)
	{
		try
		{
			Process.Start(new ProcessStartInfo(authUrl.AbsoluteUri) { UseShellExecute = true });
		}
		catch
		{
			Launcher.Default.OpenAsync(authUrl).GetAwaiter().GetResult();
		}
	}

	private static string EnsureTrailingSlash(string redirectUri) =>
		redirectUri.EndsWith('/') ? redirectUri : $"{redirectUri}/";

	private static string GenerateCodeVerifier()
	{
		var bytes = RandomNumberGenerator.GetBytes(32);
		return Base64UrlEncode(bytes);
	}

	private static string CreateCodeChallenge(string codeVerifier)
	{
		var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
		return Base64UrlEncode(hash);
	}

	private static string GenerateState()
	{
		var bytes = RandomNumberGenerator.GetBytes(16);
		return Base64UrlEncode(bytes);
	}

	private static string Base64UrlEncode(byte[] bytes) =>
		Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

	private static Dictionary<string, string> ParseQueryString(string? query)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(query))
		{
			return result;
		}

		var trimmed = query.StartsWith('?') ? query[1..] : query;
		foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
		{
			var parts = pair.Split('=', 2);
			if (parts.Length != 2)
			{
				continue;
			}

			result[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
		}

		return result;
	}

	private sealed class GoogleTokenResponse
	{
		[JsonPropertyName("id_token")]
		public string? IdToken { get; set; }

		[JsonPropertyName("error")]
		public string? Error { get; set; }

		[JsonPropertyName("error_description")]
		public string? ErrorDescription { get; set; }
	}
}
#endif
