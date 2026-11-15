using System.Text.Json;

namespace OneTapHabits.Services;

public static class GoogleServicesJsonParser
{
	private const int WebClientType = 3;

	public static string? TryGetWebClientId(string json) =>
		TryReadRoot(json, TryGetWebClientId);

	public static string? TryGetProjectId(string json) =>
		TryReadRoot(json, TryGetProjectId);

	public static string? TryGetApiKey(string json) =>
		TryReadRoot(json, TryGetApiKey);

	public static string? TryGetWebClientId(JsonElement root)
	{
		if (!root.TryGetProperty("client", out var clients))
		{
			return null;
		}

		foreach (var client in clients.EnumerateArray())
		{
			var fromOauth = TryGetWebClientIdFromOauthArray(client, "oauth_client");
			if (!string.IsNullOrWhiteSpace(fromOauth))
			{
				return fromOauth;
			}

			// Firebase often places the Web client (type 3) only under
			// services.appinvite_service.other_platform_oauth_client once Android
			// OAuth clients exist — not inside oauth_client.
			if (client.TryGetProperty("services", out var services) &&
			    services.TryGetProperty("appinvite_service", out var appInvite))
			{
				var fromOther = TryGetWebClientIdFromOauthArray(appInvite, "other_platform_oauth_client");
				if (!string.IsNullOrWhiteSpace(fromOther))
				{
					return fromOther;
				}
			}
		}

		return null;
	}

	private static string? TryGetWebClientIdFromOauthArray(JsonElement parent, string arrayPropertyName)
	{
		if (!parent.TryGetProperty(arrayPropertyName, out var oauthClients))
		{
			return null;
		}

		foreach (var oauthClient in oauthClients.EnumerateArray())
		{
			if (!oauthClient.TryGetProperty("client_type", out var clientType) ||
			    clientType.GetInt32() != WebClientType)
			{
				continue;
			}

			if (!oauthClient.TryGetProperty("client_id", out var clientId))
			{
				continue;
			}

			var value = clientId.GetString();
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value;
			}
		}

		return null;
	}

	public static string? TryGetProjectId(JsonElement root)
	{
		if (!root.TryGetProperty("project_info", out var projectInfo) ||
		    !projectInfo.TryGetProperty("project_id", out var projectId))
		{
			return null;
		}

		var value = projectId.GetString();
		return string.IsNullOrWhiteSpace(value) ? null : value;
	}

	public static string? TryGetApiKey(JsonElement root)
	{
		if (!root.TryGetProperty("client", out var clients))
		{
			return null;
		}

		foreach (var client in clients.EnumerateArray())
		{
			if (!client.TryGetProperty("api_key", out var apiKeys))
			{
				continue;
			}

			foreach (var apiKey in apiKeys.EnumerateArray())
			{
				if (!apiKey.TryGetProperty("current_key", out var currentKey))
				{
					continue;
				}

				var value = currentKey.GetString();
				if (!string.IsNullOrWhiteSpace(value))
				{
					return value;
				}
			}
		}

		return null;
	}

	private static string? TryReadRoot(string json, Func<JsonElement, string?> read)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}

		try
		{
			using var document = JsonDocument.Parse(json);
			return read(document.RootElement);
		}
		catch
		{
			return null;
		}
	}
}
