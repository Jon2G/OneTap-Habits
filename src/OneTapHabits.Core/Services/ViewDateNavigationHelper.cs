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
}
