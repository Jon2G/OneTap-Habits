using OneTapHabits.Models;

namespace OneTapHabits.Services;

public interface ILogService
{
	Task<HabitLog?> GetLogAsync(string habitId, DateOnly date);
	Task SetCompletedAsync(string habitId, DateOnly date, bool isCompleted);
	Task<int> GetCountAsync(string habitId, DateOnly date);
	Task<int> IncrementCountAsync(string habitId, DateOnly date);
	Task<IReadOnlyDictionary<string, bool>> GetCompletionMapForDateAsync(DateOnly date);
	Task<IReadOnlyDictionary<string, int>> GetCountMapForDateAsync(DateOnly date);
	Task<IReadOnlyList<HabitLog>> GetCompletedLogsInRangeAsync(DateOnly startInclusive, DateOnly endInclusive);
}
