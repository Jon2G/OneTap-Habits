using System.Text.Json;
using OneTapHabits.Models;
using OneTapHabits.Widget;

namespace OneTapHabits.Platforms.Windows.Services;

public static class WidgetSnapshotFileStore
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false
	};

	public static string GetFilePath() =>
		Path.Combine(FileSystem.AppDataDirectory, "widget_snapshot.json");

	public static WidgetSnapshot Load()
	{
		var path = GetFilePath();
		if (!File.Exists(path))
		{
			return WidgetSnapshot.NotSignedIn();
		}

		try
		{
			var json = File.ReadAllText(path);
			return JsonSerializer.Deserialize<WidgetSnapshot>(json, JsonOptions) ?? WidgetSnapshot.NotSignedIn();
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
		var json = JsonSerializer.Serialize(snapshot, JsonOptions);
		File.WriteAllText(path, json);
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

	public static void Clear() => Save(WidgetSnapshot.NotSignedIn());

	public static void SignalRefresh()
	{
		var path = Path.Combine(Path.GetDirectoryName(GetFilePath())!, "widget_refresh.signal");
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, DateTimeOffset.UtcNow.ToString("O"));
	}
}

public static class WidgetTapAnimationFileStore
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false
	};

	public static string GetFilePath() =>
		Path.Combine(FileSystem.AppDataDirectory, "widget_tap_animation.json");

	public static void Set(int cellIndex, WidgetTapAnimationKind kind)
	{
		var path = GetFilePath();
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		var json = JsonSerializer.Serialize(new WidgetTapAnimationState
		{
			CellIndex = cellIndex,
			Kind = kind
		}, JsonOptions);
		File.WriteAllText(path, json);
	}

	public static WidgetTapAnimationState? Load()
	{
		var path = GetFilePath();
		if (!File.Exists(path))
		{
			return null;
		}

		try
		{
			var state = JsonSerializer.Deserialize<WidgetTapAnimationState>(File.ReadAllText(path), JsonOptions);
			return state?.Kind == WidgetTapAnimationKind.None ? null : state;
		}
		catch
		{
			return null;
		}
	}

	public static void Clear()
	{
		var path = GetFilePath();
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}
}
