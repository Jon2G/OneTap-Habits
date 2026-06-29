using System.Text.Json;
using Android.Content;
using OneTapHabits.Models;

namespace OneTapHabits.Platforms.Android.Services;

public static class WidgetSnapshotStore
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false
	};

	public static WidgetSnapshot Load(Context context)
	{
		var prefs = context.GetSharedPreferences(AppWidgets.WidgetConstants.PrefsName, FileCreationMode.Private);
		var json = prefs?.GetString(AppWidgets.WidgetConstants.PrefsKeyJson, null);
		if (string.IsNullOrWhiteSpace(json))
		{
			return WidgetSnapshot.NotSignedIn();
		}

		try
		{
			return JsonSerializer.Deserialize<WidgetSnapshot>(json, JsonOptions) ?? WidgetSnapshot.NotSignedIn();
		}
		catch
		{
			return WidgetSnapshot.NotSignedIn();
		}
	}

	public static void Save(Context context, WidgetSnapshot snapshot)
	{
		var prefs = context.GetSharedPreferences(AppWidgets.WidgetConstants.PrefsName, FileCreationMode.Private);
		var json = JsonSerializer.Serialize(snapshot, JsonOptions);
		prefs?.Edit()?.PutString(AppWidgets.WidgetConstants.PrefsKeyJson, json)?.Apply();
	}

	public static void RemoveHabit(Context context, string habitId)
	{
		var snapshot = Load(context);
		if (!snapshot.IsSignedIn)
		{
			return;
		}

		snapshot.Habits.RemoveAll(h => h.Id == habitId);
		Save(context, snapshot);
	}

	public static void Clear(Context context)
	{
		Save(context, WidgetSnapshot.NotSignedIn());
	}
}
