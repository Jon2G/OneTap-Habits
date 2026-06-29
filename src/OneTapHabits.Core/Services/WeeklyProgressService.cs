using OneTapHabits.Models;

namespace OneTapHabits.Services;

public sealed class WeeklyProgressService : IWeeklyProgressService
{
	private const int MaxWeeksToScan = 104;

	public int CountCompletionsInWeek(string habitId, DateOnly weekStart, IReadOnlyList<HabitLog> logs)
	{
		var weekEnd = weekStart.AddDays(6);
		return logs.Count(l =>
			l.HabitId == habitId &&
			l.IsCompleted &&
			l.Date >= weekStart &&
			l.Date <= weekEnd);
	}

	public int CalculateWeeklyStreak(Habit habit, IReadOnlyList<HabitLog> logs, DateOnly today)
	{
		if (habit.ScheduleMode != HabitScheduleMode.TimesPerWeek || habit.TimesPerWeek < 1)
		{
			return 0;
		}

		var streak = 0;
		var weekStart = WeekBoundaryHelper.GetWeekStart(today);

		if (CountCompletionsInWeek(habit.Id, weekStart, logs) >= habit.TimesPerWeek)
		{
			streak++;
		}

		weekStart = weekStart.AddDays(-7);

		foreach (var pastWeekStart in WeekBoundaryHelper.EnumerateWeeksBackward(weekStart, MaxWeeksToScan))
		{
			if (CountCompletionsInWeek(habit.Id, pastWeekStart, logs) >= habit.TimesPerWeek)
			{
				streak++;
			}
			else
			{
				break;
			}
		}

		return streak;
	}
}
