using OneTapHabits.Models;

namespace OneTapHabits.Services;

public static class HabitScheduleHelper
{
	public static bool IsVisibleOnDate(Habit habit, DateOnly date)
	{
		if (habit.ScheduleMode == HabitScheduleMode.TimesPerWeek)
		{
			return true;
		}

		var dotnetDay = WeekBoundaryHelper.ToDotNetDayOfWeek(date);
		return habit.TargetDays.Contains(dotnetDay);
	}

	public static bool IsScheduledForCalendarLine(Habit habit, DateOnly date)
	{
		if (habit.ScheduleMode == HabitScheduleMode.TimesPerWeek)
		{
			return true;
		}

		var dotnetDay = WeekBoundaryHelper.ToDotNetDayOfWeek(date);
		return habit.TargetDays.Contains(dotnetDay);
	}
}
