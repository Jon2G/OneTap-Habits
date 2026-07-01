using System.Text.Json;
using OneTapHabits.Calendar;
using OneTapHabits.Models;

namespace OneTapHabits.Storage;

public static class CloudCachePersistence
{
	public const string FileName = "cloud_cache.json";

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false
	};

	public static string GetFilePath(string appDataDirectory) =>
		Path.Combine(appDataDirectory, FileName);

	public static GuestDataSnapshot GetUserSnapshot(CloudCacheFile file, string userId)
	{
		var user = FindUser(file, userId);
		return new GuestDataSnapshot
		{
			Habits = user?.Habits.ToList() ?? [],
			Logs = user?.Logs.ToList() ?? []
		};
	}

	public static void SaveUserSnapshot(CloudCacheFile file, string userId, GuestDataSnapshot snapshot)
	{
		var user = FindOrCreateUser(file, userId);
		user.Habits = snapshot.Habits.ToList();
		user.Logs = snapshot.Logs.ToList();
	}

	public static void ClearUser(CloudCacheFile file, string userId) =>
		file.Users.RemoveAll(u => u.UserId == userId);

	public static IReadOnlyList<Habit> GetActiveHabits(CloudCacheFile file, string userId) =>
		GetUserSnapshot(file, userId).Habits.Where(h => h.IsActive).ToList();

	public static Habit? GetHabit(CloudCacheFile file, string userId, string habitId) =>
		GetUserSnapshot(file, userId).Habits.FirstOrDefault(h => h.Id == habitId);

	public static void UpsertHabit(CloudCacheFile file, string userId, Habit habit)
	{
		var user = FindOrCreateUser(file, userId);
		user.Habits.RemoveAll(h => h.Id == habit.Id);
		user.Habits.Add(habit);
	}

	public static int GetCount(CloudCacheFile file, string userId, string habitId, DateOnly date)
	{
		var dateKey = date.ToString("yyyy-MM-dd");
		var entry = FindOrCreateUser(file, userId).Logs
			.FirstOrDefault(l => l.HabitId == habitId && l.Date == dateKey);
		return entry?.Count ?? 0;
	}

	public static int IncrementCount(CloudCacheFile file, string userId, string habitId, DateOnly date)
	{
		var next = GetCount(file, userId, habitId, date) + 1;
		SetCount(file, userId, habitId, date, next);
		return next;
	}

	public static void SetCount(CloudCacheFile file, string userId, string habitId, DateOnly date, int count)
	{
		var user = FindOrCreateUser(file, userId);
		var dateKey = date.ToString("yyyy-MM-dd");
		user.Logs.RemoveAll(l => l.HabitId == habitId && l.Date == dateKey);
		if (count > 0)
		{
			user.Logs.Add(new GuestLogEntry
			{
				HabitId = habitId,
				Date = dateKey,
				IsCompleted = true,
				Count = count
			});
		}
	}

	public static IReadOnlyDictionary<string, int> GetCountMapForDate(CloudCacheFile file, string userId, DateOnly date)
	{
		var dateKey = date.ToString("yyyy-MM-dd");
		return FindOrCreateUser(file, userId).Logs
			.Where(l => l.Date == dateKey && l.Count > 0)
			.GroupBy(l => l.HabitId)
			.ToDictionary(g => g.Key, g => g.Max(l => l.Count));
	}

	public static IReadOnlyList<HabitLog> GetLogsInRange(
		CloudCacheFile file,
		string userId,
		DateOnly startInclusive,
		DateOnly endInclusive) =>
		GuestLogQuery.FilterCompletedInRange(GetUserSnapshot(file, userId), startInclusive, endInclusive);

	public static bool MergeFromCloud(
		CloudCacheFile file,
		string userId,
		IReadOnlyList<Habit> cloudHabits,
		IReadOnlyList<GuestLogEntry> cloudLogs)
	{
		var user = FindOrCreateUser(file, userId);
		var habitsChanged = !HabitListsEqual(user.Habits, cloudHabits);
		user.Habits = cloudHabits.ToList();

		var logsChanged = MergeLogs(user, cloudLogs);
		return habitsChanged || logsChanged;
	}

	public static CloudCacheFile LoadFromPath(string filePath)
	{
		if (!File.Exists(filePath))
		{
			return new CloudCacheFile();
		}

		try
		{
			var json = File.ReadAllText(filePath);
			return JsonSerializer.Deserialize<CloudCacheFile>(json, JsonOptions) ?? new CloudCacheFile();
		}
		catch
		{
			return new CloudCacheFile();
		}
	}

	public static void SaveToPath(string filePath, CloudCacheFile file)
	{
		var directory = Path.GetDirectoryName(filePath);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}

		var json = JsonSerializer.Serialize(file, JsonOptions);
		File.WriteAllText(filePath, json);
	}

	private static bool MergeLogs(CloudCacheUserData user, IReadOnlyList<GuestLogEntry> cloudLogs)
	{
		var byKey = user.Logs.ToDictionary(l => (l.HabitId, l.Date));
		var changed = false;

		foreach (var cloud in cloudLogs)
		{
			var key = (cloud.HabitId, cloud.Date);
			if (byKey.TryGetValue(key, out var local))
			{
				var merged = Math.Max(local.Count, cloud.Count);
				if (merged != local.Count)
				{
					local.Count = merged;
					local.IsCompleted = merged > 0;
					changed = true;
				}
			}
			else if (cloud.Count > 0)
			{
				user.Logs.Add(new GuestLogEntry
				{
					HabitId = cloud.HabitId,
					Date = cloud.Date,
					IsCompleted = true,
					Count = cloud.Count
				});
				changed = true;
			}
		}

		return changed;
	}

	private static bool HabitListsEqual(IReadOnlyList<Habit> local, IReadOnlyList<Habit> cloud)
	{
		if (local.Count != cloud.Count)
		{
			return false;
		}

		var cloudById = cloud.ToDictionary(h => h.Id);
		foreach (var habit in local)
		{
			if (!cloudById.TryGetValue(habit.Id, out var other))
			{
				return false;
			}

			if (habit.Name != other.Name ||
			    habit.SortOrder != other.SortOrder ||
			    habit.IsActive != other.IsActive ||
			    habit.ShowInWidget != other.ShowInWidget)
			{
				return false;
			}
		}

		return true;
	}

	private static CloudCacheUserData FindOrCreateUser(CloudCacheFile file, string userId)
	{
		var user = FindUser(file, userId);
		if (user is not null)
		{
			return user;
		}

		user = new CloudCacheUserData { UserId = userId };
		file.Users.Add(user);
		return user;
	}

	private static CloudCacheUserData? FindUser(CloudCacheFile file, string userId) =>
		file.Users.FirstOrDefault(u => u.UserId == userId);
}
