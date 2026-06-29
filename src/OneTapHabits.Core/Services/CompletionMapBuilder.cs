using OneTapHabits.Models;

namespace OneTapHabits.Services;

public static class CompletionMapBuilder
{
	public static Dictionary<DateOnly, bool> BuildForHabit(string habitId, IReadOnlyList<HabitLog> logs)
	{
		return logs
			.Where(l => l.HabitId == habitId && l.IsCompleted)
			.GroupBy(l => l.Date)
			.ToDictionary(g => g.Key, _ => true);
	}
}
