namespace OneTapHabits.Calendar;

public static class CalendarMotivationResolver
{
	private static readonly int[] StreakMilestones = [100, 60, 30, 14, 7];

	public static (string Key, object[] Args) Resolve(CalendarInsightsData data)
	{
		if (data.IsSingleHabit && data.FilteredHabit is not null)
		{
			return ResolveSingleHabit(data);
		}

		return ResolveAllHabits(data);
	}

	private static (string Key, object[] Args) ResolveSingleHabit(CalendarInsightsData data)
	{
		var habit = data.FilteredHabit!;
		var streak = habit.ScheduleMode == Models.HabitScheduleMode.TimesPerWeek
			? data.CurrentStreak
			: data.CurrentStreak;

		foreach (var milestone in StreakMilestones)
		{
			if (streak >= milestone)
			{
				return ("CalendarMotivationStreakMilestone", [milestone]);
			}
		}

		if (data.MonthScheduledDays > 0 && data.MonthPace >= 0.8)
		{
			var percent = (int)Math.Round(data.MonthPace * 100);
			return ("CalendarMotivationStrongMonth", [percent]);
		}

		if (data.AllDoneToday)
		{
			return ("CalendarMotivationAllDoneToday", []);
		}

		if (habit.ScheduleMode == Models.HabitScheduleMode.TimesPerWeek &&
		    data.WeekTarget > 0 &&
		    data.WeekCompletions == data.WeekTarget - 1)
		{
			return ("CalendarMotivationAlmostWeeklyGoal", [1]);
		}

		if (streak == 0 && data.HasHistory)
		{
			return ("CalendarMotivationRestart", []);
		}

		return ("CalendarMotivationKeepGoing", []);
	}

	private static (string Key, object[] Args) ResolveAllHabits(CalendarInsightsData data)
	{
		if (data.LongestStreak >= 7)
		{
			foreach (var milestone in StreakMilestones)
			{
				if (data.LongestStreak >= milestone)
				{
					return ("CalendarMotivationStreakMilestone", [milestone]);
				}
			}
		}

		if (data.MonthScheduledDays > 0 && data.MonthPace >= 0.8)
		{
			var percent = (int)Math.Round(data.MonthPace * 100);
			return ("CalendarMotivationStrongMonth", [percent]);
		}

		if (data.AllDoneToday)
		{
			return ("CalendarMotivationAllDoneToday", []);
		}

		if (data.LongestStreak == 0 && data.HasHistory)
		{
			return ("CalendarMotivationRestart", []);
		}

		return ("CalendarMotivationKeepGoing", []);
	}
}
