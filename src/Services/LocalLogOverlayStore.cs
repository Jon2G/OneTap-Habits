using OneTapHabits.Models;
using OneTapHabits.Storage;

namespace OneTapHabits.Services;

public sealed class LocalLogOverlayStore : ILocalLogOverlayStore
{
	private readonly string _filePath;
	private readonly object _lock = new();

	public LocalLogOverlayStore()
	{
		_filePath = LogOverlayPersistence.GetFilePath(FileSystem.AppDataDirectory);
	}

	public Task<int> GetCountAsync(string userId, string habitId, DateOnly date, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		lock (_lock)
		{
			return Task.FromResult(LogOverlayPersistence.GetCount(_filePath, userId, habitId, date));
		}
	}

	public Task<int> IncrementCountAsync(string userId, string habitId, DateOnly date, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		lock (_lock)
		{
			var next = LogOverlayPersistence.IncrementCount(_filePath, userId, habitId, date);
			return Task.FromResult(next);
		}
	}

	public Task SetCountAsync(string userId, string habitId, DateOnly date, int count, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		lock (_lock)
		{
			LogOverlayPersistence.SetCount(_filePath, userId, habitId, date, count);
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
			var map = LogOverlayPersistence.GetCountMapForDate(_filePath, userId, date);
			return Task.FromResult(map);
		}
	}

	public Task<IReadOnlyList<GuestLogEntry>> GetLogEntriesInRangeAsync(
		string userId,
		DateOnly startInclusive,
		DateOnly endInclusive,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		lock (_lock)
		{
			var entries = LogOverlayPersistence.GetLogEntriesInRange(_filePath, userId, startInclusive, endInclusive);
			return Task.FromResult(entries);
		}
	}

	public Task ClearForUserAsync(string userId, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		lock (_lock)
		{
			LogOverlayPersistence.ClearForUser(_filePath, userId);
		}

		return Task.CompletedTask;
	}

	public static int IncrementCount(string appDataDirectory, string userId, string habitId, DateOnly date)
	{
		var filePath = LogOverlayPersistence.GetFilePath(appDataDirectory);
		return LogOverlayPersistence.IncrementCount(filePath, userId, habitId, date);
	}

	public static void SetCount(string appDataDirectory, string userId, string habitId, DateOnly date, int count)
	{
		var filePath = LogOverlayPersistence.GetFilePath(appDataDirectory);
		LogOverlayPersistence.SetCount(filePath, userId, habitId, date, count);
	}
}
