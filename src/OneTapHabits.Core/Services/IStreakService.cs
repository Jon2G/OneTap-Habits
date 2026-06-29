using OneTapHabits.Models;

namespace OneTapHabits.Services;

public interface IStreakService
{
	int CalculateCurrentStreak(Habit habit, IReadOnlyDictionary<DateOnly, bool> completionByDate, DateOnly today);
}
