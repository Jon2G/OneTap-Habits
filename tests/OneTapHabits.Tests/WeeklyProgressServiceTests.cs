using OneTapHabits.Models;
using OneTapHabits.Services;
using Xunit;

namespace OneTapHabits.Tests;

public class WeeklyProgressServiceTests
{
	private readonly WeeklyProgressService _sut = new();

	[Fact]
	public void CountCompletionsInWeek_counts_only_that_week()
	{
		var weekStart = new DateOnly(2026, 6, 15);
		var logs = new List<HabitLog>
		{
			new() { HabitId = "gym", Date = new DateOnly(2026, 6, 15), IsCompleted = true },
			new() { HabitId = "gym", Date = new DateOnly(2026, 6, 17), IsCompleted = true },
			new() { HabitId = "gym", Date = new DateOnly(2026, 6, 22), IsCompleted = true }
		};

		Assert.Equal(2, _sut.CountCompletionsInWeek("gym", weekStart, logs));
	}

	[Fact]
	public void CalculateWeeklyStreak_counts_consecutive_weeks()
	{
		var habit = new Habit
		{
			Id = "gym",
			ScheduleMode = HabitScheduleMode.TimesPerWeek,
			TimesPerWeek = 2
		};

		var today = new DateOnly(2026, 6, 18);
		var logs = new List<HabitLog>
		{
			new() { HabitId = "gym", Date = new DateOnly(2026, 6, 16), IsCompleted = true },
			new() { HabitId = "gym", Date = new DateOnly(2026, 6, 17), IsCompleted = true },
			new() { HabitId = "gym", Date = new DateOnly(2026, 6, 9), IsCompleted = true },
			new() { HabitId = "gym", Date = new DateOnly(2026, 6, 10), IsCompleted = true },
			new() { HabitId = "gym", Date = new DateOnly(2026, 6, 1), IsCompleted = true }
		};

		var streak = _sut.CalculateWeeklyStreak(habit, logs, today);
		Assert.Equal(2, streak);
	}

	[Fact]
	public void CalculateWeeklyStreak_partial_current_week_does_not_break()
	{
		var habit = new Habit
		{
			Id = "gym",
			ScheduleMode = HabitScheduleMode.TimesPerWeek,
			TimesPerWeek = 3
		};

		var today = new DateOnly(2026, 6, 18);
		var logs = new List<HabitLog>
		{
			new() { HabitId = "gym", Date = new DateOnly(2026, 6, 16), IsCompleted = true },
			new() { HabitId = "gym", Date = new DateOnly(2026, 6, 9), IsCompleted = true },
			new() { HabitId = "gym", Date = new DateOnly(2026, 6, 10), IsCompleted = true },
			new() { HabitId = "gym", Date = new DateOnly(2026, 6, 11), IsCompleted = true }
		};

		var streak = _sut.CalculateWeeklyStreak(habit, logs, today);
		Assert.Equal(1, streak);
	}
}
