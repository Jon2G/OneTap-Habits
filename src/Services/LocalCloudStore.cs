using OneTapHabits.Models;
using OneTapHabits.Storage;

namespace OneTapHabits.Services;

public sealed class LocalCloudStore : ILocalCloudStore
{
	private readonly string _filePath;
	private readonly object _lock = new();

	public LocalCloudStore()
	{
		_filePath = CloudCachePersistence.GetFilePath(FileSystem.AppDataDirectory);
	}

	public Task<GuestDataSnapshot> LoadAsync(string userId, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		lock (_lock)
		{
			var file = CloudCachePersistence.LoadFromPath(_filePath);
			return Task.FromResult(CloudCachePersistence.GetUserSnapshot(file, userId));
		}
	}

	public Task SaveAsync(string userId, GuestDataSnapshot snapshot, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		lock (_lock)
		{
			var file = CloudCachePersistence.LoadFromPath(_filePath);
			CloudCachePersistence.SaveUserSnapshot(file, userId, snapshot);
			CloudCachePersistence.SaveToPath(_filePath, file);
		}

		return Task.CompletedTask;
	}

	public Task<IReadOnlyList<Habit>> GetActiveHabitsAsync(string userId, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		lock (_lock)
		{
			var file = CloudCachePersistence.LoadFromPath(_filePath);
			return Task.FromResult(CloudCachePersistence.GetActiveHabits(file, userId));
		}
	}

	public Task<Habit?> GetHabitAsync(string userId, string habitId, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		lock (_lock)
		{
			var file = CloudCachePersistence.LoadFromPath(_filePath);
			return Task.FromResult(CloudCachePersistence.GetHabit(file, userId, habitId));
		}
	}

	public Task UpsertHabitAsync(string userId, Habit habit, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		lock (_lock)
		{
			var file = CloudCachePersistence.LoadFromPath(_filePath);
			CloudCachePersistence.UpsertHabit(file, userId, habit);
			CloudCachePersistence.SaveToPath(_filePath, file);
		}

		return Task.CompletedTask;
	}

	public Task<int> GetCountAsync(string userId, string habitId, DateOnly date, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		lock (_lock)
		{
			var file = CloudCachePersistence.LoadFromPath(_filePath);
			return Task.FromResult(CloudCachePersistence.GetCount(file, userId, habitId, date));
		}
	}

	public Task<int> IncrementCountAsync(string userId, string habitId, DateOnly date, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		lock (_lock)
		{
			var file = CloudCachePersistence.LoadFromPath(_filePath);
			var next = CloudCachePersistence.IncrementCount(file, userId, habitId, date);
			CloudCachePersistence.SaveToPath(_filePath, file);
			return Task.FromResult(next);
		}
	}

	public Task SetCountAsync(string userId, string habitId, DateOnly date, int count, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		lock (_lock)
		{
			var file = CloudCachePersistence.LoadFromPath(_filePath);
			CloudCachePersistence.SetCount(file, userId, habitId, date, count);
			CloudCachePersistence.SaveToPath(_filePath, file);
		}

		return Task.CompletedTask;
	}

	public Task<IReadOnlyDictionary<string, int>> GetCountMapForDateAsync(
		string userId,
		DateOnly date,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		lock (_lock)
		{
			var file = CloudCachePersistence.LoadFromPath(_filePath);
			return Task.FromResult(CloudCachePersistence.GetCountMapForDate(file, userId, date));
		}
	}

	public Task<IReadOnlyList<HabitLog>> GetLogsInRangeAsync(
		string userId,
		DateOnly startInclusive,
		DateOnly endInclusive,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		lock (_lock)
		{
			var file = CloudCachePersistence.LoadFromPath(_filePath);
			return Task.FromResult(CloudCachePersistence.GetLogsInRange(file, userId, startInclusive, endInclusive));
		}
	}

	public Task<bool> MergeFromCloudAsync(
		string userId,
		IReadOnlyList<Habit> cloudHabits,
		IReadOnlyList<GuestLogEntry> cloudLogs,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		lock (_lock)
		{
			var file = CloudCachePersistence.LoadFromPath(_filePath);
			var changed = CloudCachePersistence.MergeFromCloud(file, userId, cloudHabits, cloudLogs);
			CloudCachePersistence.SaveToPath(_filePath, file);
			return Task.FromResult(changed);
		}
	}

	public Task ClearForUserAsync(string userId, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		lock (_lock)
		{
			var file = CloudCachePersistence.LoadFromPath(_filePath);
			CloudCachePersistence.ClearUser(file, userId);
			CloudCachePersistence.SaveToPath(_filePath, file);
		}

		return Task.CompletedTask;
	}

	public static int IncrementCount(string appDataDirectory, string userId, string habitId, DateOnly date)
	{
		var filePath = CloudCachePersistence.GetFilePath(appDataDirectory);
		var file = CloudCachePersistence.LoadFromPath(filePath);
		var next = CloudCachePersistence.IncrementCount(file, userId, habitId, date);
		CloudCachePersistence.SaveToPath(filePath, file);
		return next;
	}
}
