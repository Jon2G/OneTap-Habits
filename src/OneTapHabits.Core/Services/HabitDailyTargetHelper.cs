using OneTapHabits.Models;

namespace OneTapHabits.Services;

public static class HabitDailyTargetHelper
{
	public static int GetDailyTarget(Habit habit) =>
		habit.ScheduleMode == HabitScheduleMode.SpecificDays
			? Math.Clamp(habit.TimesPerDay, 1, 10)
			: 1;

	public static bool IsDailyTargetMet(Habit habit, int count) =>
		count >= GetDailyTarget(habit);
}
