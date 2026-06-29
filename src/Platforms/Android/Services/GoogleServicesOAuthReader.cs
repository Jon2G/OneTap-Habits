using System.Text.Json;
using Android.Content;

namespace OneTapHabits.Platforms.Android.Services;

public static class GoogleServicesOAuthReader
{
	private const int WebClientType = 3;

	public static string? TryGetWebClientId(Context context)
	{
		var resourceId = context.Resources?.GetIdentifier(
			"default_web_client_id",
			"string",
			context.PackageName);

		if (resourceId is not null and not 0)
		{
			var fromResource = context.GetString(resourceId.Value);
			if (!string.IsNullOrWhiteSpace(fromResource))
			{
				return fromResource;
			}
		}

		return TryGetWebClientIdFromJson(context);
	}

	private static string? TryGetWebClientIdFromJson(Context context)
	{
		try
		{
			using var stream = context.Assets?.Open("google-services.json");
			if (stream is null)
			{
				return null;
			}

			using var document = JsonDocument.Parse(stream);
			if (!document.RootElement.TryGetProperty("client", out var clients))
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
		}
		catch
		{
			// Fall through to null when config is missing or malformed.
		}

		return null;
	}
}
