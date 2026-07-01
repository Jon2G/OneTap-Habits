using System.Globalization;
using OneTapHabits.Services;
using OneTapHabits.Services.Widget;
using Xunit;

namespace OneTapHabits.Tests;

public class ViewDateNavigationHelperTests
{
	[Fact]
	public void ClampToTodayOrPast_ReturnsSameDate_WhenOnOrBeforeToday()
	{
		var today = new DateOnly(2026, 6, 30);
		Assert.Equal(new DateOnly(2026, 6, 28), ViewDateNavigationHelper.ClampToTodayOrPast(new DateOnly(2026, 6, 28), today));
		Assert.Equal(today, ViewDateNavigationHelper.ClampToTodayOrPast(today, today));
	}

	[Fact]
	public void ClampToTodayOrPast_ClampsFutureDates_ToToday()
	{
		var today = new DateOnly(2026, 6, 30);
		Assert.Equal(today, ViewDateNavigationHelper.ClampToTodayOrPast(new DateOnly(2026, 7, 1), today));
	}

	[Fact]
	public void CanGoToNextDay_IsFalse_WhenSelectedIsToday()
	{
		var today = new DateOnly(2026, 6, 30);
		Assert.False(ViewDateNavigationHelper.CanGoToNextDay(today, today));
	}

	[Fact]
	public void CanGoToNextDay_IsTrue_WhenSelectedIsBeforeToday()
	{
		var today = new DateOnly(2026, 6, 30);
		Assert.True(ViewDateNavigationHelper.CanGoToNextDay(new DateOnly(2026, 6, 29), today));
	}

	[Theory]
	[InlineData("2026-06-28", true)]
	[InlineData("2026/06/28", false)]
	[InlineData("", false)]
	public void TryParseQueryDate_ParsesIsoFormatOnly(string input, bool expected)
	{
		var parsed = ViewDateNavigationHelper.TryParseQueryDate(input, out var date);
		Assert.Equal(expected, parsed);
		if (expected)
		{
			Assert.Equal(new DateOnly(2026, 6, 28), date);
		}
	}

	[Fact]
	public void FormatDayTitle_ReturnsTodayTitle_WhenSelectedIsToday()
	{
		var today = new DateOnly(2026, 6, 30);
		Assert.Equal("Today", ViewDateNavigationHelper.FormatDayTitle(today, today, "Today", "Yesterday"));
	}

	[Fact]
	public void FormatDayTitle_ReturnsYesterdayTitle_WhenSelectedIsYesterday()
	{
		var today = new DateOnly(2026, 7, 1); // Wednesday
		var yesterday = new DateOnly(2026, 6, 30);
		Assert.Equal("Yesterday", ViewDateNavigationHelper.FormatDayTitle(yesterday, today, "Today", "Yesterday"));
	}

	[Fact]
	public void FormatDayTitle_ReturnsWeekdayName_WhenWithinSevenDaysBack()
	{
		var today = new DateOnly(2026, 7, 1); // Wednesday
		var threeDaysBack = new DateOnly(2026, 6, 28); // Sunday
		var culture = new CultureInfo("en-US");
		Assert.Equal("Sunday", ViewDateNavigationHelper.FormatDayTitle(
			threeDaysBack, today, "Today", "Yesterday", culture));
	}

	[Fact]
	public void FormatDayTitle_ReturnsFormattedDate_WhenMoreThanSevenDaysBack()
	{
		var today = new DateOnly(2026, 7, 1);
		var eightDaysBack = new DateOnly(2026, 6, 23);
		var title = ViewDateNavigationHelper.FormatDayTitle(
			eightDaysBack, today, "Today", "Yesterday", CultureInfo.InvariantCulture);
		Assert.Contains("2026", title);
		Assert.Contains("23", title);
	}
}

public class WidgetProgressFormatterTests
{
	[Theory]
	[InlineData(0, 1, null)]
	[InlineData(1, 1, null)]
	[InlineData(0, 3, "0/3")]
	[InlineData(2, 5, "2/5")]
	public void FormatProgress_ShowsFractionOnlyForMultiCount(int count, int timesPerDay, string? expected)
	{
		Assert.Equal(expected, WidgetProgressFormatter.FormatProgress(count, timesPerDay));
	}
}
