using OneTapHabits.Models;
using OneTapHabits.Services;
using Xunit;

namespace OneTapHabits.Tests;

public class StreakWithCountTests
{
	[Fact]
	public void StreakCountsWhenCountIsOneOfThree()
	{
		var habit = new Habit
		{
			Id = "brush",
			TargetDays = [1, 2, 3, 4, 5, 6, 7],
			ScheduleMode = HabitScheduleMode.SpecificDays,
			TimesPerDay = 3
		};

		var today = new DateOnly(2026, 6, 28);
		var logs = new List<HabitLog>
		{
			new()
			{
				HabitId = "brush",
				Date = today,
				IsCompleted = true,
				Count = 1
			}
		};

		var completionMap = CompletionMapBuilder.BuildForHabit(habit.Id, logs);
		var streak = new StreakService().CalculateCurrentStreak(habit, completionMap, today);

		Assert.Equal(1, streak);
		Assert.True(completionMap[today]);
	}
}
