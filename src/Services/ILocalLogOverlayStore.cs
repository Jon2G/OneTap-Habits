using OneTapHabits.Models;

namespace OneTapHabits.Services;

public interface ILocalLogOverlayStore
{
	Task<int> GetCountAsync(string userId, string habitId, DateOnly date, CancellationToken cancellationToken = default);

	Task<int> IncrementCountAsync(string userId, string habitId, DateOnly date, CancellationToken cancellationToken = default);

	Task SetCountAsync(string userId, string habitId, DateOnly date, int count, CancellationToken cancellationToken = default);

	Task<IReadOnlyDictionary<string, int>> GetCountMapForDateAsync(string userId, DateOnly date, CancellationToken cancellationToken = default);

	Task<IReadOnlyList<GuestLogEntry>> GetLogEntriesInRangeAsync(
		string userId,
		DateOnly startInclusive,
		DateOnly endInclusive,
		CancellationToken cancellationToken = default);

	Task ClearForUserAsync(string userId, CancellationToken cancellationToken = default);
}
