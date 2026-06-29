using OneTapHabits.Models;
using OneTapHabits.Services;
using Xunit;

namespace OneTapHabits.Tests;

public class HabitScheduleHelperTests
{
	[Fact]
	public void IsVisibleOnDate_times_per_week_always_true()
	{
		var habit = new Habit { ScheduleMode = HabitScheduleMode.TimesPerWeek, TargetDays = [1, 2, 3] };
		Assert.True(HabitScheduleHelper.IsVisibleOnDate(habit, new DateOnly(2026, 6, 14)));
	}

	[Fact]
	public void IsVisibleOnDate_specific_days_respects_target()
	{
		var habit = new Habit { ScheduleMode = HabitScheduleMode.SpecificDays, TargetDays = [1, 2, 3, 4, 5] };
		Assert.True(HabitScheduleHelper.IsVisibleOnDate(habit, new DateOnly(2026, 6, 15)));
		Assert.False(HabitScheduleHelper.IsVisibleOnDate(habit, new DateOnly(2026, 6, 14)));
	}

	[Fact]
	public void IsScheduledForCalendarLine_times_per_week_any_day()
	{
		var habit = new Habit { ScheduleMode = HabitScheduleMode.TimesPerWeek, TargetDays = [1] };
		Assert.True(HabitScheduleHelper.IsScheduledForCalendarLine(habit, new DateOnly(2026, 6, 14)));
	}
}
