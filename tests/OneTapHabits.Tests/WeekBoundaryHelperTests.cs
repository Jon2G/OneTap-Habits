using OneTapHabits.Models;
using OneTapHabits.Services;
using Xunit;

namespace OneTapHabits.Tests;

public class WeekBoundaryHelperTests
{
	[Fact]
	public void GetWeekStart_returns_monday()
	{
		var wednesday = new DateOnly(2026, 6, 17);
		Assert.Equal(new DateOnly(2026, 6, 15), WeekBoundaryHelper.GetWeekStart(wednesday));
	}

	[Fact]
	public void GetWeekStart_monday_stays_same()
	{
		var monday = new DateOnly(2026, 6, 15);
		Assert.Equal(monday, WeekBoundaryHelper.GetWeekStart(monday));
	}

	[Fact]
	public void GetWeekEnd_returns_sunday()
	{
		var wednesday = new DateOnly(2026, 6, 17);
		Assert.Equal(new DateOnly(2026, 6, 21), WeekBoundaryHelper.GetWeekEnd(wednesday));
	}
}
