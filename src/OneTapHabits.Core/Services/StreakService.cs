using OneTapHabits.Models;

namespace OneTapHabits.Services;

public sealed class StreakService : IStreakService
{
	public int CalculateCurrentStreak(Habit habit, IReadOnlyDictionary<DateOnly, bool> completionByDate, DateOnly today)
	{
		var streak = 0;
		var cursor = today;

		while (true)
		{
			var dayOfWeek = (int)cursor.DayOfWeek;
			var dotnetDay = dayOfWeek == 0 ? 7 : dayOfWeek;

			if (!habit.TargetDays.Contains(dotnetDay))
			{
				cursor = cursor.AddDays(-1);
				continue;
			}

			if (completionByDate.TryGetValue(cursor, out var completed) && completed)
			{
				streak++;
				cursor = cursor.AddDays(-1);
				continue;
			}

			break;
		}

		return streak;
	}
}
