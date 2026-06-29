using OneTapHabits.Models;
using OneTapHabits.Services;
using Xunit;

namespace OneTapHabits.Tests;

public class StreakServiceTests
{
	private readonly StreakService _sut = new();

	[Fact]
	public void CalculateCurrentStreak_ReturnsZero_WhenTodayNotCompleted()
	{
		var habit = DailyHabit();
		var today = new DateOnly(2026, 6, 28); // Saturday

		var streak = _sut.CalculateCurrentStreak(habit, new Dictionary<DateOnly, bool>(), today);

		Assert.Equal(0, streak);
	}

	[Fact]
	public void CalculateCurrentStreak_CountsConsecutiveCompletedDays()
	{
		var habit = DailyHabit();
		var today = new DateOnly(2026, 6, 28);
		var map = new Dictionary<DateOnly, bool>
		{
			[today] = true,
			[today.AddDays(-1)] = true,
			[today.AddDays(-2)] = false
		};

		var streak = _sut.CalculateCurrentStreak(habit, map, today);

		Assert.Equal(2, streak);
	}

	[Fact]
	public void CalculateCurrentStreak_SkipsNonTargetDaysWithoutBreaking()
	{
		var habit = new Habit { TargetDays = [1, 2, 3, 4, 5] }; // weekdays only
		var today = new DateOnly(2026, 6, 30); // Monday
		var map = new Dictionary<DateOnly, bool>
		{
			[today] = true,
			[new DateOnly(2026, 6, 29)] = false, // Sunday — skipped
			[new DateOnly(2026, 6, 28)] = false  // Saturday — skipped
		};

		var streak = _sut.CalculateCurrentStreak(habit, map, today);

		Assert.Equal(1, streak);
	}

	private static Habit DailyHabit() => new()
	{
		TargetDays = [1, 2, 3, 4, 5, 6, 7]
	};
}
