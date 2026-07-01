using System.Text.Json;

namespace OneTapHabits.Services;

public static class GoogleServicesJsonParser
{
	private const int WebClientType = 3;

	public static string? TryGetWebClientId(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}

		try
		{
			using var document = JsonDocument.Parse(json);
			return TryGetWebClientId(document.RootElement);
		}
		catch
		{
			return null;
		}
	}

	public static string? TryGetWebClientId(JsonElement root)
	{
		if (!root.TryGetProperty("client", out var clients))
		{
			return null;
		}

		foreach (var client in clients.EnumerateArray())
		{
			if (!client.TryGetProperty("oauth_client", out var oauthClients))
			{
				continue;
			}

			foreach (var oauthClient in oauthClients.EnumerateArray())
			{
				if (!oauthClient.TryGetProperty("client_type", out var clientType) ||
				    clientType.GetInt32() != WebClientType)
				{
					continue;
				}

				if (oauthClient.TryGetProperty("client_id", out var clientId))
				{
					var value = clientId.GetString();
					if (!string.IsNullOrWhiteSpace(value))
					{
						return value;
					}
				}
			}
		}

		return null;
	}
}
