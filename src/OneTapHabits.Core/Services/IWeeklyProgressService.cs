using OneTapHabits.Models;

namespace OneTapHabits.Services;

public interface IWeeklyProgressService
{
	int CountCompletionsInWeek(string habitId, DateOnly weekStart, IReadOnlyList<HabitLog> logs);

	int CalculateWeeklyStreak(Habit habit, IReadOnlyList<HabitLog> logs, DateOnly today);
}
