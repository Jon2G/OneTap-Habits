using OneTapHabits.Calendar;
using OneTapHabits.Models;
using OneTapHabits.Services;
using Xunit;

namespace OneTapHabits.Tests;

public class CalendarInsightsBuilderTests
{
	private readonly StreakService _streak = new();
	private readonly WeeklyProgressService _weekly = new();

	[Fact]
	public void Build_returns_no_insights_when_no_habits()
	{
		var snapshot = CalendarInsightsBuilder.Build(
			[],
			[],
			new DateOnly(2026, 6, 1),
			new DateOnly(2026, 6, 28),
			null,
			_streak,
			_weekly);

		Assert.False(snapshot.ShowInsights);
	}

	[Fact]
	public void Build_single_daily_habit_includes_streak_and_month_progress()
	{
		var habit = new Habit
		{
			Id = "read",
			Name = "Read",
			TargetDays = [1, 2, 3, 4, 5, 6, 7],
			ScheduleMode = HabitScheduleMode.SpecificDays
		};

		var today = new DateOnly(2026, 6, 28);
		var logs = new List<HabitLog>
		{
			new() { HabitId = "read", Date = today, IsCompleted = true, Count = 1 },
			new() { HabitId = "read", Date = today.AddDays(-1), IsCompleted = true, Count = 1 },
			new() { HabitId = "read", Date = today.AddDays(-2), IsCompleted = true, Count = 1 }
		};

		var snapshot = CalendarInsightsBuilder.Build(
			[habit],
			logs,
			new DateOnly(2026, 6, 1),
			today,
			"read",
			_streak,
			_weekly);

		Assert.True(snapshot.ShowInsights);
		Assert.Contains(snapshot.Metrics, m => m.LabelKey == "CalendarMetricCurrentStreak" && m.Value == "3");
		Assert.Contains(snapshot.Metrics, m => m.LabelKey == "CalendarMetricMonthProgress");
		Assert.Equal(habit.ColorHex, snapshot.AccentColorHex);
	}

	[Fact]
	public void Build_weekly_habit_includes_week_streak_and_progress()
	{
		var habit = new Habit
		{
			Id = "gym",
			Name = "Gym",
			ScheduleMode = HabitScheduleMode.TimesPerWeek,
			TimesPerWeek = 2
		};

		var today = new DateOnly(2026, 6, 18);
		var logs = new List<HabitLog>
		{
			new() { HabitId = "gym", Date = new DateOnly(2026, 6, 16), IsCompleted = true },
			new() { HabitId = "gym", Date = new DateOnly(2026, 6, 17), IsCompleted = true },
			new() { HabitId = "gym", Date = new DateOnly(2026, 6, 9), IsCompleted = true },
			new() { HabitId = "gym", Date = new DateOnly(2026, 6, 10), IsCompleted = true }
		};

		var snapshot = CalendarInsightsBuilder.Build(
			[habit],
			logs,
			new DateOnly(2026, 6, 1),
			today,
			"gym",
			_streak,
			_weekly);

		Assert.Contains(snapshot.Metrics, m => m.LabelKey == "CalendarMetricCurrentStreak" && m.Value == "2");
		Assert.Contains(snapshot.Metrics, m => m.LabelKey == "CalendarMetricWeekProgress" && m.Value == "2/2");
	}

	[Fact]
	public void Build_all_habits_includes_longest_streak_and_today_overview()
	{
		var daily = new Habit
		{
			Id = "a",
			Name = "Alpha",
			TargetDays = [1, 2, 3, 4, 5, 6, 7],
			ScheduleMode = HabitScheduleMode.SpecificDays
		};
		var other = new Habit
		{
			Id = "b",
			Name = "Beta",
			TargetDays = [1, 2, 3, 4, 5, 6, 7],
			ScheduleMode = HabitScheduleMode.SpecificDays
		};

		var today = new DateOnly(2026, 6, 28);
		var logs = new List<HabitLog>
		{
			new() { HabitId = "a", Date = today, IsCompleted = true, Count = 1 },
			new() { HabitId = "a", Date = today.AddDays(-1), IsCompleted = true, Count = 1 },
			new() { HabitId = "a", Date = today.AddDays(-2), IsCompleted = true, Count = 1 },
			new() { HabitId = "b", Date = today, IsCompleted = true, Count = 1 }
		};

		var snapshot = CalendarInsightsBuilder.Build(
			[daily, other],
			logs,
			new DateOnly(2026, 6, 1),
			today,
			null,
			_streak,
			_weekly);

		Assert.Contains(snapshot.Metrics, m => m.LabelKey == "CalendarMetricTodayOverview" && m.Value == "2/2");
		Assert.Contains(snapshot.Metrics, m => m.LabelKey == "CalendarMetricLongestStreak" && m.Value.Contains("Alpha"));
		Assert.Contains(snapshot.Metrics, m => m.LabelKey == "CalendarMetricHabitsOnStreak" && m.Value == "2");
	}
}
