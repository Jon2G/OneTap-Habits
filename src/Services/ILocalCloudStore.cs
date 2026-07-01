using OneTapHabits.Models;

namespace OneTapHabits.Services;

public interface ILocalCloudStore
{
	Task<GuestDataSnapshot> LoadAsync(string userId, CancellationToken cancellationToken = default);

	Task SaveAsync(string userId, GuestDataSnapshot snapshot, CancellationToken cancellationToken = default);

	Task<IReadOnlyList<Habit>> GetActiveHabitsAsync(string userId, CancellationToken cancellationToken = default);

	Task<Habit?> GetHabitAsync(string userId, string habitId, CancellationToken cancellationToken = default);

	Task UpsertHabitAsync(string userId, Habit habit, CancellationToken cancellationToken = default);

	Task<int> GetCountAsync(string userId, string habitId, DateOnly date, CancellationToken cancellationToken = default);

	Task<int> IncrementCountAsync(string userId, string habitId, DateOnly date, CancellationToken cancellationToken = default);

	Task SetCountAsync(string userId, string habitId, DateOnly date, int count, CancellationToken cancellationToken = default);

	Task<IReadOnlyDictionary<string, int>> GetCountMapForDateAsync(
		string userId,
		DateOnly date,
		CancellationToken cancellationToken = default);

	Task<IReadOnlyList<HabitLog>> GetLogsInRangeAsync(
		string userId,
		DateOnly startInclusive,
		DateOnly endInclusive,
		CancellationToken cancellationToken = default);

	Task<bool> MergeFromCloudAsync(
		string userId,
		IReadOnlyList<Habit> cloudHabits,
		IReadOnlyList<GuestLogEntry> cloudLogs,
		CancellationToken cancellationToken = default);

	Task ClearForUserAsync(string userId, CancellationToken cancellationToken = default);
}
