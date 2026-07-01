using OneTapHabits.Calendar;
using OneTapHabits.Models;
using Xunit;

namespace OneTapHabits.Tests;

public class CalendarMotivationResolverTests
{
	[Fact]
	public void Resolve_picks_streak_milestone_at_seven_days()
	{
		var data = new CalendarInsightsData
		{
			IsSingleHabit = true,
			FilteredHabit = new Habit { ScheduleMode = HabitScheduleMode.SpecificDays },
			CurrentStreak = 7
		};

		var (key, args) = CalendarMotivationResolver.Resolve(data);

		Assert.Equal("CalendarMotivationStreakMilestone", key);
		Assert.Equal([7], args);
	}

	[Fact]
	public void Resolve_falls_back_to_keep_going()
	{
		var data = new CalendarInsightsData
		{
			IsSingleHabit = true,
			FilteredHabit = new Habit { ScheduleMode = HabitScheduleMode.SpecificDays },
			CurrentStreak = 2,
			MonthScheduledDays = 10,
			MonthCompletedDays = 3
		};

		var (key, args) = CalendarMotivationResolver.Resolve(data);

		Assert.Equal("CalendarMotivationKeepGoing", key);
		Assert.Empty(args);
	}

	[Fact]
	public void Resolve_all_done_today_for_aggregate()
	{
		var data = new CalendarInsightsData
		{
			IsSingleHabit = false,
			TodayScheduledHabits = 3,
			TodayCompletedHabits = 3
		};

		var (key, _) = CalendarMotivationResolver.Resolve(data);

		Assert.Equal("CalendarMotivationAllDoneToday", key);
	}
}
