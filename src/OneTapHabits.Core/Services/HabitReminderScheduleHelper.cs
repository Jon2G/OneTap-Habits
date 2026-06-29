using OneTapHabits.Models;

namespace OneTapHabits.Services;

public static class HabitReminderScheduleHelper
{
	public static DateTime? GetNextReminderLocalDateTime(Habit habit, DateTime fromLocal)
	{
		if (!habit.ReminderEnabled || habit.ReminderTime is null)
		{
			return null;
		}

		for (var dayOffset = 0; dayOffset < 14; dayOffset++)
		{
			var date = DateOnly.FromDateTime(fromLocal.Date.AddDays(dayOffset));
			if (!HabitScheduleHelper.IsVisibleOnDate(habit, date))
			{
				continue;
			}

			var candidate = date.ToDateTime(habit.ReminderTime.Value, DateTimeKind.Local);
			if (candidate > fromLocal)
			{
				return candidate;
			}
		}

		return null;
	}

	public static int GetNotificationId(string habitId)
	{
		unchecked
		{
			var hash = habitId.GetHashCode(StringComparison.Ordinal);
			return Math.Abs(hash % 900_000) + 1000;
		}
	}
}
