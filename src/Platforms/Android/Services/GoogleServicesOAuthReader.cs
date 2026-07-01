using Android.Content;
using OneTapHabits.Services;

namespace OneTapHabits.Platforms.Android.Services;

public static class GoogleServicesOAuthReader
{
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

			using var reader = new StreamReader(stream);
			return GoogleServicesJsonParser.TryGetWebClientId(reader.ReadToEnd());
		}
		catch
		{
			return null;
		}
	}
}
