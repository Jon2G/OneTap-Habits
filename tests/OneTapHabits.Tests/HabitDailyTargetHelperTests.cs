using OneTapHabits.Models;
using OneTapHabits.Services;
using Xunit;

namespace OneTapHabits.Tests;

public class HabitDailyTargetHelperTests
{
	[Fact]
	public void GetDailyTarget_SpecificDays_UsesTimesPerDay()
	{
		var habit = new Habit { ScheduleMode = HabitScheduleMode.SpecificDays, TimesPerDay = 3 };
		Assert.Equal(3, HabitDailyTargetHelper.GetDailyTarget(habit));
	}

	[Fact]
	public void GetDailyTarget_TimesPerWeek_ReturnsOne()
	{
		var habit = new Habit { ScheduleMode = HabitScheduleMode.TimesPerWeek, TimesPerWeek = 4 };
		Assert.Equal(1, HabitDailyTargetHelper.GetDailyTarget(habit));
	}

	[Theory]
	[InlineData(2, 3, false)]
	[InlineData(3, 3, true)]
	[InlineData(4, 3, true)]
	public void IsDailyTargetMet_Works(int count, int target, bool expected)
	{
		var habit = new Habit { ScheduleMode = HabitScheduleMode.SpecificDays, TimesPerDay = target };
		Assert.Equal(expected, HabitDailyTargetHelper.IsDailyTargetMet(habit, count));
	}
}
