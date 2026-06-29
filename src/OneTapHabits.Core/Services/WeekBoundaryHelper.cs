namespace OneTapHabits.Services;

public static class WeekBoundaryHelper
{
	public static DateOnly GetWeekStart(DateOnly date)
	{
		var dotnetDay = ToDotNetDayOfWeek(date);
		return date.AddDays(-(dotnetDay - 1));
	}

	public static DateOnly GetWeekEnd(DateOnly date)
	{
		var dotnetDay = ToDotNetDayOfWeek(date);
		return date.AddDays(7 - dotnetDay);
	}

	public static IEnumerable<DateOnly> EnumerateWeeksBackward(DateOnly fromWeekStart, int maxWeeks)
	{
		var cursor = fromWeekStart;
		for (var i = 0; i < maxWeeks; i++)
		{
			yield return cursor;
			cursor = cursor.AddDays(-7);
		}
	}

	public static int ToDotNetDayOfWeek(DateOnly date)
	{
		var dayOfWeek = (int)date.DayOfWeek;
		return dayOfWeek == 0 ? 7 : dayOfWeek;
	}
}
