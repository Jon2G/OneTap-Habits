using OneTapHabits.Models;

namespace OneTapHabits.Services;

public interface IHabitService
{
	Task<IReadOnlyList<Habit>> GetActiveHabitsAsync();
	Task<Habit?> GetHabitAsync(string habitId);
	Task SaveHabitAsync(Habit habit);
	Task DeleteHabitAsync(string habitId);
	Task<IReadOnlyList<Habit>> GetTodayHabitsAsync(DateOnly today);
}
