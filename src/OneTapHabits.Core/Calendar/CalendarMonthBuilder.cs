using OneTapHabits.Models;
using OneTapHabits.Services;

namespace OneTapHabits.Calendar;

public static class CalendarMonthBuilder
{
	private const int MaxVisibleLines = 4;

	public static CalendarMonthGrid Build(
		DateOnly month,
		IReadOnlyList<Habit> habits,
		IReadOnlyList<HabitLog> completedLogs,
		string? habitIdFilter,
		DateOnly today)
	{
		var monthStart = new DateOnly(month.Year, month.Month, 1);
		var monthEnd = monthStart.AddMonths(1).AddDays(-1);
		var gridStart = StartOfWeek(monthStart);
		var gridEnd = EndOfWeek(monthEnd);

		var habitMap = habits
			.Where(h => h.IsActive)
			.Where(h => habitIdFilter is null || h.Id == habitIdFilter)
			.ToDictionary(h => h.Id);

		var logsByDate = completedLogs
			.Where(l => l.IsCompleted && habitMap.ContainsKey(l.HabitId))
			.GroupBy(l => l.Date)
			.ToDictionary(g => g.Key, g => g.ToList());

		var weeks = new List<CalendarWeek>();
		var cursor = gridStart;

		while (cursor <= gridEnd)
		{
			var days = new List<CalendarDayCell>(7);
			for (var i = 0; i < 7; i++)
			{
				var date = cursor.AddDays(i);
				var lines = BuildLinesForDate(date, habitMap, logsByDate, today);
				var overflow = Math.Max(0, lines.Count - MaxVisibleLines);

				days.Add(new CalendarDayCell
				{
					Date = date,
					IsCurrentMonth = date.Month == monthStart.Month && date.Year == monthStart.Year,
					IsToday = date == today,
					CompletionLines = lines,
					OverflowCount = overflow
				});
			}

			weeks.Add(new CalendarWeek { Days = days });
			cursor = cursor.AddDays(7);
		}

		return new CalendarMonthGrid
		{
			MonthStart = monthStart,
			MonthEnd = monthEnd,
			Weeks = weeks
		};
	}

	private static List<CalendarCompletionLine> BuildLinesForDate(
		DateOnly date,
		IReadOnlyDictionary<string, Habit> habitMap,
		IReadOnlyDictionary<DateOnly, List<HabitLog>> logsByDate,
		DateOnly today)
	{
		if (date > today)
		{
			return [];
		}

		if (!logsByDate.TryGetValue(date, out var logs))
		{
			return [];
		}

		var lines = new List<CalendarCompletionLine>();

		foreach (var habit in habitMap.Values.OrderBy(h => h.Name, StringComparer.OrdinalIgnoreCase))
		{
			if (!HabitScheduleHelper.IsScheduledForCalendarLine(habit, date))
			{
				continue;
			}

			if (logs.Any(l => l.HabitId == habit.Id && l.IsCompleted))
			{
				lines.Add(new CalendarCompletionLine
				{
					HabitId = habit.Id,
					ColorHex = habit.ColorHex
				});
			}
		}

		return lines;
	}

	internal static int ToDotNetDayOfWeek(DateOnly date)
	{
		var dayOfWeek = (int)date.DayOfWeek;
		return dayOfWeek == 0 ? 7 : dayOfWeek;
	}

	public static DateOnly StartOfWeek(DateOnly date)
	{
		var dotnetDay = ToDotNetDayOfWeek(date);
		return date.AddDays(-(dotnetDay - 1));
	}

	public static DateOnly EndOfWeek(DateOnly date)
	{
		var dotnetDay = ToDotNetDayOfWeek(date);
		return date.AddDays(7 - dotnetDay);
	}
}
