using OneTapHabits.Calendar;
using OneTapHabits.Models;
using Xunit;

namespace OneTapHabits.Tests;

public class CalendarMonthBuilderTests
{
	private static readonly DateOnly Today = new(2026, 6, 15);

	[Fact]
	public void Build_shows_line_when_scheduled_and_completed()
	{
		var habit = new Habit
		{
			Id = "gym",
			Name = "Gym",
			ColorHex = "#F97316",
			TargetDays = [1, 2, 3, 4, 5, 6, 7]
		};

		var logs = new List<HabitLog>
		{
			new() { HabitId = "gym", Date = new DateOnly(2026, 6, 10), IsCompleted = true }
		};

		var grid = CalendarMonthBuilder.Build(new DateOnly(2026, 6, 1), [habit], logs, null, Today);
		var day = FindDay(grid, new DateOnly(2026, 6, 10));

		Assert.Single(day.CompletionLines);
		Assert.Equal("#F97316", day.CompletionLines[0].ColorHex);
	}

	[Fact]
	public void Build_skips_line_when_not_scheduled()
	{
		var habit = new Habit
		{
			Id = "gym",
			Name = "Gym",
			ColorHex = "#F97316",
			TargetDays = [1, 2, 3, 4, 5]
		};

		var saturday = new DateOnly(2026, 6, 14);
		var logs = new List<HabitLog>
		{
			new() { HabitId = "gym", Date = saturday, IsCompleted = true }
		};

		var grid = CalendarMonthBuilder.Build(new DateOnly(2026, 6, 1), [habit], logs, null, Today);
		var day = FindDay(grid, saturday);

		Assert.Empty(day.CompletionLines);
	}

	[Fact]
	public void Build_filters_by_habit()
	{
		var gym = new Habit { Id = "gym", Name = "Gym", ColorHex = "#F97316", TargetDays = [1, 2, 3, 4, 5, 6, 7] };
		var meds = new Habit { Id = "meds", Name = "Meds", ColorHex = "#3B82F6", TargetDays = [1, 2, 3, 4, 5, 6, 7] };
		var date = new DateOnly(2026, 6, 10);
		var logs = new List<HabitLog>
		{
			new() { HabitId = "gym", Date = date, IsCompleted = true },
			new() { HabitId = "meds", Date = date, IsCompleted = true }
		};

		var grid = CalendarMonthBuilder.Build(new DateOnly(2026, 6, 1), [gym, meds], logs, "meds", Today);
		var day = FindDay(grid, date);

		Assert.Single(day.CompletionLines);
		Assert.Equal("meds", day.CompletionLines[0].HabitId);
	}

	[Fact]
	public void Build_pads_weeks_to_full_rows()
	{
		var grid = CalendarMonthBuilder.Build(new DateOnly(2026, 6, 1), [], [], null, Today);

		Assert.True(grid.Weeks.Count >= 4);
		Assert.All(grid.Weeks, w => Assert.Equal(7, w.Days.Count));
	}

	[Fact]
	public void Build_includes_all_completion_lines_when_many_habits()
	{
		var habits = Enumerable.Range(1, 5).Select(i => new Habit
		{
			Id = $"h{i}",
			Name = $"Habit {i}",
			ColorHex = "#22C55E",
			TargetDays = [1, 2, 3, 4, 5, 6, 7]
		}).ToList();

		var date = new DateOnly(2026, 6, 10);
		var logs = habits.Select(h => new HabitLog
		{
			HabitId = h.Id,
			Date = date,
			IsCompleted = true
		}).ToList();

		var grid = CalendarMonthBuilder.Build(new DateOnly(2026, 6, 1), habits, logs, null, Today);
		var day = FindDay(grid, date);

		Assert.Equal(5, day.CompletionLines.Count);
	}

	[Fact]
	public void Build_does_not_show_lines_for_future_dates()
	{
		var habit = new Habit { Id = "gym", Name = "Gym", ColorHex = "#F97316", TargetDays = [1, 2, 3, 4, 5, 6, 7] };
		var future = Today.AddDays(1);
		var logs = new List<HabitLog>
		{
			new() { HabitId = "gym", Date = future, IsCompleted = true }
		};

		var grid = CalendarMonthBuilder.Build(new DateOnly(2026, 6, 1), [habit], logs, null, Today);
		var day = FindDay(grid, future);

		Assert.Empty(day.CompletionLines);
	}

	[Fact]
	public void Build_times_per_week_shows_line_on_weekend()
	{
		var habit = new Habit
		{
			Id = "gym",
			Name = "Gym",
			ColorHex = "#F97316",
			ScheduleMode = HabitScheduleMode.TimesPerWeek,
			TimesPerWeek = 3,
			TargetDays = [1, 2, 3, 4, 5]
		};

		var saturday = new DateOnly(2026, 6, 14);
		var logs = new List<HabitLog>
		{
			new() { HabitId = "gym", Date = saturday, IsCompleted = true }
		};

		var grid = CalendarMonthBuilder.Build(new DateOnly(2026, 6, 1), [habit], logs, null, Today);
		var day = FindDay(grid, saturday);

		Assert.Single(day.CompletionLines);
	}

	private static CalendarDayCell FindDay(CalendarMonthGrid grid, DateOnly date) =>
		grid.Weeks.SelectMany(w => w.Days).First(d => d.Date == date);
}
