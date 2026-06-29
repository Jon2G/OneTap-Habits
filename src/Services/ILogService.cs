using OneTapHabits.Models;

namespace OneTapHabits.Services;

public interface ILogService
{
	Task<HabitLog?> GetLogAsync(string habitId, DateOnly date);
	Task SetCompletedAsync(string habitId, DateOnly date, bool isCompleted);
	Task<IReadOnlyDictionary<string, bool>> GetCompletionMapForDateAsync(DateOnly date);
	Task<IReadOnlyList<HabitLog>> GetCompletedLogsInRangeAsync(DateOnly startInclusive, DateOnly endInclusive);
}
