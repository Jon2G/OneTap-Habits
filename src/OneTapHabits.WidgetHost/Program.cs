using System.Text.Json;
using OneTapHabits.Models;

namespace OneTapHabits.WidgetHost;

internal static class WidgetSnapshotFileStore
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public static string GetFilePath()
	{
		var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
		return Path.Combine(localFolder, "widget_snapshot.json");
	}

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
}

internal static class WidgetCardBuilder
{
	public static string LoadTemplate() =>
		File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Templates", "HabitsTemplate.json"));

	public static string BuildData(WidgetSnapshot snapshot)
	{
		if (!snapshot.IsSignedIn)
		{
			return JsonSerializer.Serialize(new { title = "OneTap Habits", subtitle = "Open the app to get started." });
		}

		if (snapshot.Habits.Count == 0)
		{
			var done = snapshot.OverflowCount > 0
				? $"{snapshot.OverflowCount} more habits done today."
				: "All habits done for today.";
			return JsonSerializer.Serialize(new { title = "OneTap Habits", subtitle = done });
		}

		var lines = snapshot.Habits
			.Select(h =>
			{
				var progress = h.TimesPerDay > 1 ? $" ({h.Count}/{h.TimesPerDay})" : string.Empty;
				return $"• {h.Name}{progress}";
			});
		var subtitle = string.Join("\n", lines);
		if (snapshot.OverflowCount > 0)
		{
			subtitle += $"\n+{snapshot.OverflowCount} more";
		}

		return JsonSerializer.Serialize(new { title = "Today's habits", subtitle });
	}
}

internal sealed class HabitsWidgetProvider
{
	public string CreateTemplate() => WidgetCardBuilder.LoadTemplate();

	public string CreateData() => WidgetCardBuilder.BuildData(WidgetSnapshotFileStore.Load());
}

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		Console.WriteLine("OneTap Habits WidgetHost started.");
		Console.WriteLine(WidgetCardBuilder.BuildData(WidgetSnapshotFileStore.Load()));
		Console.WriteLine("Press ENTER to exit.");
		Console.ReadLine();
	}
}
