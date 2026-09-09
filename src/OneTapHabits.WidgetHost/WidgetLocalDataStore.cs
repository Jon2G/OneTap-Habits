using System.Text.Json;
using OneTapHabits.Models;
using OneTapHabits.Storage;

namespace OneTapHabits.WidgetHost;

internal static class WidgetLocalDataStore
{
	private const string GuestFileName = "guest_data.json";

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public static int IncrementGuestCount(string appDataDirectory, string habitId, DateOnly date) =>
		AdjustGuestCount(appDataDirectory, habitId, date, +1);

	public static int DecrementGuestCount(string appDataDirectory, string habitId, DateOnly date) =>
		AdjustGuestCount(appDataDirectory, habitId, date, -1);

	public static int IncrementCloudCount(string appDataDirectory, string userId, string habitId, DateOnly date)
	{
		var filePath = CloudCachePersistence.GetFilePath(appDataDirectory);
		var file = CloudCachePersistence.LoadFromPath(filePath);
		var next = CloudCachePersistence.IncrementCount(file, userId, habitId, date);
		CloudCachePersistence.SaveToPath(filePath, file);
		return next;
	}

	public static int DecrementCloudCount(string appDataDirectory, string userId, string habitId, DateOnly date)
	{
		var filePath = CloudCachePersistence.GetFilePath(appDataDirectory);
		var file = CloudCachePersistence.LoadFromPath(filePath);
		var next = CloudCachePersistence.DecrementCount(file, userId, habitId, date);
		CloudCachePersistence.SaveToPath(filePath, file);
		return next;
	}

	private static int AdjustGuestCount(string appDataDirectory, string habitId, DateOnly date, int delta)
	{
		var filePath = Path.Combine(appDataDirectory, GuestFileName);
		var snapshot = LoadGuestSnapshot(filePath);
		var next = AdjustGuestLogCount(snapshot, habitId, date, delta);
		SaveGuestSnapshot(filePath, snapshot);
		return next;
	}

	private static GuestDataSnapshot LoadGuestSnapshot(string filePath)
	{
		if (!File.Exists(filePath))
		{
			return new GuestDataSnapshot();
		}

		try
		{
			return JsonSerializer.Deserialize<GuestDataSnapshot>(File.ReadAllText(filePath), JsonOptions)
				?? new GuestDataSnapshot();
		}
		catch
		{
			return new GuestDataSnapshot();
		}
	}

	private static void SaveGuestSnapshot(string filePath, GuestDataSnapshot snapshot)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
		File.WriteAllText(filePath, JsonSerializer.Serialize(snapshot, JsonOptions));
	}

	private static int AdjustGuestLogCount(GuestDataSnapshot snapshot, string habitId, DateOnly date, int delta)
	{
		var dateKey = date.ToString("yyyy-MM-dd");
		var current = snapshot.Logs.FirstOrDefault(l => l.HabitId == habitId && l.Date == dateKey)?.Count ?? 0;
		var next = Math.Max(0, current + delta);

		snapshot.Logs.RemoveAll(l => l.HabitId == habitId && l.Date == dateKey);
		if (next > 0)
		{
			snapshot.Logs.Add(new GuestLogEntry
			{
				HabitId = habitId,
				Date = dateKey,
				IsCompleted = true,
				Count = next
			});
		}

		return next;
	}
}
