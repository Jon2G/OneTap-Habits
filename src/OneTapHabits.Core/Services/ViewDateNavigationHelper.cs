using System.Globalization;

namespace OneTapHabits.Services;

public static class ViewDateNavigationHelper
{
	public static DateOnly ClampToTodayOrPast(DateOnly date, DateOnly today) =>
		date > today ? today : date;

	public static bool CanGoToNextDay(DateOnly selectedDate, DateOnly today) =>
		selectedDate < today;

	public static bool TryParseQueryDate(string? dateText, out DateOnly date) =>
		DateOnly.TryParseExact(
			dateText,
			"yyyy-MM-dd",
			CultureInfo.InvariantCulture,
			DateTimeStyles.None,
			out date);

	public static string FormatDayTitle(
		DateOnly selectedDate,
		DateOnly today,
		string todayTitle,
		string yesterdayTitle,
		CultureInfo? culture = null)
	{
		var daysBack = today.DayNumber - selectedDate.DayNumber;
		if (daysBack == 0)
		{
			return todayTitle;
		}

		if (daysBack == 1)
		{
			return yesterdayTitle;
		}

		culture ??= CultureInfo.CurrentUICulture;

		if (daysBack <= MaxWeekdayTitleDaysBack)
		{
			return culture.DateTimeFormat.GetDayName(selectedDate.DayOfWeek);
		}

		return selectedDate.ToDateTime(TimeOnly.MinValue).ToString("D", culture);
	}

	private const int MaxWeekdayTitleDaysBack = 7;
}
