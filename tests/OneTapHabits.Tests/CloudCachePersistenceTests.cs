using OneTapHabits.Models;
using OneTapHabits.Storage;
using Xunit;

namespace OneTapHabits.Tests;

public class CloudCachePersistenceTests
{
	[Fact]
	public void HasUserCacheData_IsFalse_WhenEmpty()
	{
		var file = new CloudCacheFile();
		Assert.False(CloudCachePersistence.HasUserCacheData(file, "user1"));
	}

	[Fact]
	public void HasUserCacheData_IsTrue_WhenHabitsPresent()
	{
		var file = new CloudCacheFile();
		CloudCachePersistence.SaveUserSnapshot(file, "user1", new GuestDataSnapshot
		{
			Habits = [new Habit { Id = "h1", IsActive = true }]
		});
		Assert.True(CloudCachePersistence.HasUserCacheData(file, "user1"));
	}

	[Fact]
	public void HasUserCacheData_IsTrue_WhenLogsPresent()
	{
		var file = new CloudCacheFile();
		CloudCachePersistence.SaveUserSnapshot(file, "user1", new GuestDataSnapshot
		{
			Logs = [new GuestLogEntry { HabitId = "h1", Date = "2026-06-30", Count = 1, IsCompleted = true }]
		});
		Assert.True(CloudCachePersistence.HasUserCacheData(file, "user1"));
	}

	[Fact]
	public void MergeFromCloud_PreservesLocalHabits_WhenCloudHabitsEmpty()
	{
		var file = new CloudCacheFile();
		CloudCachePersistence.SaveUserSnapshot(file, "user1", new GuestDataSnapshot
		{
			Habits =
			[
				new Habit { Id = "local1", Name = "Gym", IsActive = true, SortOrder = 0 }
			]
		});

		var changed = CloudCachePersistence.MergeFromCloud(file, "user1", [], []);

		Assert.False(changed);
		var snapshot = CloudCachePersistence.GetUserSnapshot(file, "user1");
		Assert.Single(snapshot.Habits);
		Assert.Equal("local1", snapshot.Habits[0].Id);
	}

	[Fact]
	public void MergeFromCloud_ReplacesLocalHabits_WhenCloudHasData()
	{
		var file = new CloudCacheFile();
		CloudCachePersistence.SaveUserSnapshot(file, "user1", new GuestDataSnapshot
		{
			Habits = [new Habit { Id = "local1", Name = "Old", IsActive = true }]
		});

		var cloudHabits = new List<Habit> { new() { Id = "cloud1", Name = "New", IsActive = true } };
		var changed = CloudCachePersistence.MergeFromCloud(file, "user1", cloudHabits, []);

		Assert.True(changed);
		var snapshot = CloudCachePersistence.GetUserSnapshot(file, "user1");
		Assert.Single(snapshot.Habits);
		Assert.Equal("cloud1", snapshot.Habits[0].Id);
	}

	[Fact]
	public void MergeFromCloud_AppliesCloudHabits_WhenLocalEmpty()
	{
		var file = new CloudCacheFile();
		var cloudHabits = new List<Habit> { new() { Id = "cloud1", Name = "Run", IsActive = true } };

		var changed = CloudCachePersistence.MergeFromCloud(file, "user1", cloudHabits, []);

		Assert.True(changed);
		var snapshot = CloudCachePersistence.GetUserSnapshot(file, "user1");
		Assert.Equal("cloud1", snapshot.Habits[0].Id);
	}

	[Fact]
	public void MergeFromCloud_MergesLogsWithoutRemovingLocal()
	{
		var file = new CloudCacheFile();
		CloudCachePersistence.SaveUserSnapshot(file, "user1", new GuestDataSnapshot
		{
			Habits = [new Habit { Id = "h1", IsActive = true }],
			Logs = [new GuestLogEntry { HabitId = "h1", Date = "2026-06-28", Count = 2, IsCompleted = true }]
		});

		var cloudLogs = new List<GuestLogEntry>
		{
			new() { HabitId = "h1", Date = "2026-06-28", Count = 5, IsCompleted = true }
		};

		CloudCachePersistence.MergeFromCloud(file, "user1", [], cloudLogs);

		var snapshot = CloudCachePersistence.GetUserSnapshot(file, "user1");
		Assert.Equal(5, snapshot.Logs.Single(l => l.Date == "2026-06-28").Count);
		Assert.Single(snapshot.Habits);
	}
}
