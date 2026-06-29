using OneTapHabits.Calendar;
using OneTapHabits.Models;
using Xunit;

namespace OneTapHabits.Tests;

public class GuestLogQueryTests
{
	[Fact]
	public void FilterCompletedInRange_returns_logs_in_month()
	{
		var guest = new GuestDataSnapshot
		{
			Logs =
			[
				new GuestLogEntry { HabitId = "a", Date = "2026-06-01", IsCompleted = true },
				new GuestLogEntry { HabitId = "a", Date = "2026-06-15", IsCompleted = true },
				new GuestLogEntry { HabitId = "a", Date = "2026-07-01", IsCompleted = true },
				new GuestLogEntry { HabitId = "b", Date = "2026-06-10", IsCompleted = false }
			]
		};

		var logs = GuestLogQuery.FilterCompletedInRange(
			guest,
			new DateOnly(2026, 6, 1),
			new DateOnly(2026, 6, 30));

		Assert.Equal(2, logs.Count);
		Assert.All(logs, l => Assert.Equal(6, l.Date.Month));
	}
}
