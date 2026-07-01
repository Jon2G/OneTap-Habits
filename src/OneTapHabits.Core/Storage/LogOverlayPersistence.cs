using System.Text.Json;
using OneTapHabits.Models;

namespace OneTapHabits.Storage;

public static class LogOverlayPersistence
{
	public const string FileName = "log_overlay.json";

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false
	};

	public static string GetFilePath(string appDataDirectory) =>
		Path.Combine(appDataDirectory, FileName);

	public static int GetCount(string filePath, string userId, string habitId, DateOnly date)
	{
		var snapshot = LoadFromPath(filePath);
		return FindEntry(snapshot, userId, habitId, date)?.Count ?? 0;
	}

	public static int IncrementCount(string filePath, string userId, string habitId, DateOnly date)
	{
		var snapshot = LoadFromPath(filePath);
		var existing = FindEntry(snapshot, userId, habitId, date);
		var next = (existing?.Count ?? 0) + 1;
		RemoveEntry(snapshot, userId, habitId, date);
		snapshot.Entries.Add(ToEntry(userId, habitId, date, next));
		SaveToPath(filePath, snapshot);
		return next;
	}

	public static void SetCount(string filePath, string userId, string habitId, DateOnly date, int count)
	{
		var snapshot = LoadFromPath(filePath);
		RemoveEntry(snapshot, userId, habitId, date);
		if (count > 0)
		{
			snapshot.Entries.Add(ToEntry(userId, habitId, date, count));
		}

		SaveToPath(filePath, snapshot);
	}

	public static IReadOnlyDictionary<string, int> GetCountMapForDate(string filePath, string userId, DateOnly date)
	{
		var snapshot = LoadFromPath(filePath);
		var dateKey = date.ToString("yyyy-MM-dd");
		return snapshot.Entries
			.Where(e => e.UserId == userId && e.Date == dateKey && e.Count > 0)
			.GroupBy(e => e.HabitId)
			.ToDictionary(g => g.Key, g => g.Max(e => e.Count));
	}

	public static IReadOnlyList<GuestLogEntry> GetLogEntriesInRange(
		string filePath,
		string userId,
		DateOnly startInclusive,
		DateOnly endInclusive)
	{
		var snapshot = LoadFromPath(filePath);
		return snapshot.Entries
			.Where(e => e.UserId == userId && e.Count > 0)
			.Where(e => DateOnly.TryParse(e.Date, out var parsed) && parsed >= startInclusive && parsed <= endInclusive)
			.Select(e => new GuestLogEntry
			{
				HabitId = e.HabitId,
				Date = e.Date,
				IsCompleted = true,
				Count = e.Count
			})
			.ToList();
	}

	public static void ClearForUser(string filePath, string userId)
	{
		var snapshot = LoadFromPath(filePath);
		snapshot.Entries.RemoveAll(e => e.UserId == userId);
		SaveToPath(filePath, snapshot);
	}

	public static LogOverlaySnapshot LoadFromPath(string filePath)
	{
		if (!File.Exists(filePath))
		{
			return new LogOverlaySnapshot();
		}

		try
		{
			var json = File.ReadAllText(filePath);
			return JsonSerializer.Deserialize<LogOverlaySnapshot>(json, JsonOptions) ?? new LogOverlaySnapshot();
		}
		catch
		{
			return new LogOverlaySnapshot();
		}
	}

	public static void SaveToPath(string filePath, LogOverlaySnapshot snapshot)
	{
		var directory = Path.GetDirectoryName(filePath);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}

		var json = JsonSerializer.Serialize(snapshot, JsonOptions);
		File.WriteAllText(filePath, json);
	}

	private static LogOverlayEntry ToEntry(string userId, string habitId, DateOnly date, int count) => new()
	{
		UserId = userId,
		HabitId = habitId,
		Date = date.ToString("yyyy-MM-dd"),
		Count = count
	};

	private static LogOverlayEntry? FindEntry(LogOverlaySnapshot snapshot, string userId, string habitId, DateOnly date)
	{
		var dateKey = date.ToString("yyyy-MM-dd");
		return snapshot.Entries.FirstOrDefault(e =>
			e.UserId == userId && e.HabitId == habitId && e.Date == dateKey);
	}

	private static void RemoveEntry(LogOverlaySnapshot snapshot, string userId, string habitId, DateOnly date)
	{
		var dateKey = date.ToString("yyyy-MM-dd");
		snapshot.Entries.RemoveAll(e =>
			e.UserId == userId && e.HabitId == habitId && e.Date == dateKey);
	}
}
