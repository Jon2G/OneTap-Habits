using OneTapHabits.Models;
using OneTapHabits.Services;
using Xunit;

namespace OneTapHabits.Tests;

public class HabitReminderScheduleHelperTests
{
	[Fact]
	public void GetNextReminderLocalDateTime_SkipsNonScheduledDays()
	{
		var habit = new Habit
		{
			ReminderEnabled = true,
			ReminderTime = new TimeOnly(9, 0),
			TargetDays = [1],
			ScheduleMode = HabitScheduleMode.SpecificDays
		};

		var mondayMorning = new DateTime(2026, 6, 29, 8, 0, 0, DateTimeKind.Local);
		var next = HabitReminderScheduleHelper.GetNextReminderLocalDateTime(habit, mondayMorning);

		Assert.NotNull(next);
		Assert.Equal(DayOfWeek.Monday, next!.Value.DayOfWeek);
		Assert.Equal(9, next.Value.Hour);
	}

	[Fact]
	public void GetNotificationId_IsStableForSameHabit()
	{
		var first = HabitReminderScheduleHelper.GetNotificationId("habit-abc");
		var second = HabitReminderScheduleHelper.GetNotificationId("habit-abc");
		Assert.Equal(first, second);
	}
}
