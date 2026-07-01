using OneTapHabits.Storage;
using Xunit;

namespace OneTapHabits.Tests;

public class LocalLogOverlayStoreTests
{
	[Fact]
	public void IncrementCount_persists_and_returns_next_value()
	{
		var dir = CreateTempDirectory();
		var filePath = LogOverlayPersistence.GetFilePath(dir);
		var date = new DateOnly(2026, 6, 28);

		Assert.Equal(1, LogOverlayPersistence.IncrementCount(filePath, "user-a", "habit-1", date));
		Assert.Equal(2, LogOverlayPersistence.IncrementCount(filePath, "user-a", "habit-1", date));
		Assert.Equal(2, LogOverlayPersistence.GetCount(filePath, "user-a", "habit-1", date));
	}

	[Fact]
	public void GetCountMapForDate_returns_counts_for_user_and_date_only()
	{
		var dir = CreateTempDirectory();
		var filePath = LogOverlayPersistence.GetFilePath(dir);
		var date = new DateOnly(2026, 6, 28);
		var otherDate = new DateOnly(2026, 6, 27);

		LogOverlayPersistence.IncrementCount(filePath, "user-a", "habit-1", date);
		LogOverlayPersistence.IncrementCount(filePath, "user-a", "habit-1", date);
		LogOverlayPersistence.IncrementCount(filePath, "user-a", "habit-2", date);
		LogOverlayPersistence.IncrementCount(filePath, "user-b", "habit-1", date);
		LogOverlayPersistence.IncrementCount(filePath, "user-a", "habit-1", otherDate);

		var map = LogOverlayPersistence.GetCountMapForDate(filePath, "user-a", date);

		Assert.Equal(2, map.Count);
		Assert.Equal(2, map["habit-1"]);
		Assert.Equal(1, map["habit-2"]);
	}

	[Fact]
	public void ClearForUser_removes_only_matching_user_entries()
	{
		var dir = CreateTempDirectory();
		var filePath = LogOverlayPersistence.GetFilePath(dir);
		var date = new DateOnly(2026, 6, 28);

		LogOverlayPersistence.IncrementCount(filePath, "user-a", "habit-1", date);
		LogOverlayPersistence.IncrementCount(filePath, "user-b", "habit-1", date);

		LogOverlayPersistence.ClearForUser(filePath, "user-a");

		Assert.Equal(0, LogOverlayPersistence.GetCount(filePath, "user-a", "habit-1", date));
		Assert.Equal(1, LogOverlayPersistence.GetCount(filePath, "user-b", "habit-1", date));
	}

	[Fact]
	public void SetCount_zero_removes_entry()
	{
		var dir = CreateTempDirectory();
		var filePath = LogOverlayPersistence.GetFilePath(dir);
		var date = new DateOnly(2026, 6, 28);

		LogOverlayPersistence.IncrementCount(filePath, "user-a", "habit-1", date);
		LogOverlayPersistence.SetCount(filePath, "user-a", "habit-1", date, 0);

		Assert.Equal(0, LogOverlayPersistence.GetCount(filePath, "user-a", "habit-1", date));
		Assert.Empty(LogOverlayPersistence.LoadFromPath(filePath).Entries);
	}

	private static string CreateTempDirectory()
	{
		var dir = Path.Combine(Path.GetTempPath(), "onetaphabits-overlay-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dir);
		return dir;
	}
}
