using System.Text.Json;
using OneTapHabits.Models;

namespace OneTapHabits.WidgetHost;

internal static class WidgetSnapshotFileStore
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public static string GetAppDataDirectory() =>
		Windows.Storage.ApplicationData.Current.LocalFolder.Path;

	public static string GetFilePath() =>
		Path.Combine(GetAppDataDirectory(), "widget_snapshot.json");

	public static WidgetSnapshot Load()
	{
		var path = GetFilePath();
		if (!File.Exists(path))
		{
			return WidgetSnapshot.NotSignedIn();
		}

		try
		{
			return JsonSerializer.Deserialize<WidgetSnapshot>(File.ReadAllText(path), JsonOptions)
				?? WidgetSnapshot.NotSignedIn();
		}
		catch
		{
			return WidgetSnapshot.NotSignedIn();
		}
	}

	public static void Save(WidgetSnapshot snapshot)
	{
		var path = GetFilePath();
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, JsonSerializer.Serialize(snapshot, JsonOptions));
	}

	public static void RemoveHabit(string habitId)
	{
		var snapshot = Load();
		if (!snapshot.IsSignedIn)
		{
			return;
		}

		snapshot.Habits.RemoveAll(h => h.Id == habitId);
		Save(snapshot);
	}

	public static void UpdateHabitCount(string habitId, int newCount)
	{
		var snapshot = Load();
		if (!snapshot.IsSignedIn)
		{
			return;
		}

		var entry = snapshot.Habits.FirstOrDefault(h => h.Id == habitId);
		if (entry is null)
		{
			return;
		}

		entry.Count = newCount;
		Save(snapshot);
	}
}
