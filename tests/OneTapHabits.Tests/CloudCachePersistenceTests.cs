using OneTapHabits.Models;
using OneTapHabits.Storage;
using Xunit;

namespace OneTapHabits.Tests;

public class CloudCachePersistenceTests
{
	[Fact]
	public void UpsertHabit_and_GetActiveHabits_round_trip()
	{
		var filePath = CreateCacheFilePath();
		var file = new CloudCacheFile();
		var habit = CreateHabit("habit-1");

		CloudCachePersistence.UpsertHabit(file, "user-a", habit);
		CloudCachePersistence.SaveToPath(filePath, file);

		var loaded = CloudCachePersistence.LoadFromPath(filePath);
		var habits = CloudCachePersistence.GetActiveHabits(loaded, "user-a");

		Assert.Single(habits);
		Assert.Equal("habit-1", habits[0].Id);
	}

	[Fact]
	public void IncrementCount_persists_and_returns_next_value()
	{
		var filePath = CreateCacheFilePath();
		var file = new CloudCacheFile();
		var date = new DateOnly(2026, 6, 28);

		Assert.Equal(1, CloudCachePersistence.IncrementCount(file, "user-a", "habit-1", date));
		Assert.Equal(2, CloudCachePersistence.IncrementCount(file, "user-a", "habit-1", date));
		CloudCachePersistence.SaveToPath(filePath, file);

		var loaded = CloudCachePersistence.LoadFromPath(filePath);
		Assert.Equal(2, CloudCachePersistence.GetCount(loaded, "user-a", "habit-1", date));
	}

	[Fact]
	public void GetCountMapForDate_returns_counts_for_user_and_date_only()
	{
		var file = new CloudCacheFile();
		var date = new DateOnly(2026, 6, 28);
		var otherDate = new DateOnly(2026, 6, 27);

		CloudCachePersistence.IncrementCount(file, "user-a", "habit-1", date);
		CloudCachePersistence.IncrementCount(file, "user-a", "habit-1", date);
		CloudCachePersistence.IncrementCount(file, "user-a", "habit-2", date);
		CloudCachePersistence.IncrementCount(file, "user-b", "habit-1", date);
		CloudCachePersistence.IncrementCount(file, "user-a", "habit-1", otherDate);

		var map = CloudCachePersistence.GetCountMapForDate(file, "user-a", date);

		Assert.Equal(2, map.Count);
		Assert.Equal(2, map["habit-1"]);
		Assert.Equal(1, map["habit-2"]);
	}

	[Fact]
	public void ClearUser_removes_only_matching_user_data()
	{
		var file = new CloudCacheFile();
		var date = new DateOnly(2026, 6, 28);

		CloudCachePersistence.IncrementCount(file, "user-a", "habit-1", date);
		CloudCachePersistence.IncrementCount(file, "user-b", "habit-1", date);

		CloudCachePersistence.ClearUser(file, "user-a");

		Assert.Equal(0, CloudCachePersistence.GetCount(file, "user-a", "habit-1", date));
		Assert.Equal(1, CloudCachePersistence.GetCount(file, "user-b", "habit-1", date));
	}

	[Fact]
	public void MergeFromCloud_keeps_higher_local_log_count()
	{
		var file = new CloudCacheFile();
		var date = new DateOnly(2026, 6, 28);
		CloudCachePersistence.SetCount(file, "user-a", "habit-1", date, 3);

		var changed = CloudCachePersistence.MergeFromCloud(
			file,
			"user-a",
			[CreateHabit("habit-1")],
			[
				new GuestLogEntry
				{
					HabitId = "habit-1",
					Date = date.ToString("yyyy-MM-dd"),
					IsCompleted = true,
					Count = 2
				}
			]);

		Assert.True(changed);
		Assert.Equal(3, CloudCachePersistence.GetCount(file, "user-a", "habit-1", date));
	}

	[Fact]
	public void SetCount_zero_removes_log_entry()
	{
		var file = new CloudCacheFile();
		var date = new DateOnly(2026, 6, 28);

		CloudCachePersistence.IncrementCount(file, "user-a", "habit-1", date);
		CloudCachePersistence.SetCount(file, "user-a", "habit-1", date, 0);

		Assert.Equal(0, CloudCachePersistence.GetCount(file, "user-a", "habit-1", date));
		Assert.Empty(CloudCachePersistence.GetUserSnapshot(file, "user-a").Logs);
	}

	private static Habit CreateHabit(string id) => new()
	{
		Id = id,
		Name = "Test",
		ColorHex = "#FF5722",
		IsActive = true,
		SortOrder = 0
	};

	private static string CreateCacheFilePath()
	{
		var dir = Path.Combine(Path.GetTempPath(), "onetaphabits-cloud-cache-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dir);
		return CloudCachePersistence.GetFilePath(dir);
	}
}
