namespace OneTapHabits.Calendar;

public sealed class CalendarCompletionLine
{
	public string HabitId { get; init; } = string.Empty;

	public string ColorHex { get; init; } = string.Empty;
}

public sealed class CalendarDayCell
{
	public DateOnly Date { get; init; }

	public bool IsCurrentMonth { get; init; }

	public bool IsToday { get; init; }

	public IReadOnlyList<CalendarCompletionLine> CompletionLines { get; init; } = [];

	public int OverflowCount { get; init; }

	public IReadOnlyList<CalendarCompletionLine> VisibleLines =>
		OverflowCount > 0 ? CompletionLines.Take(4).ToList() : CompletionLines;
}

public sealed class CalendarWeek
{
	public IReadOnlyList<CalendarDayCell> Days { get; init; } = [];
}

public sealed class CalendarMonthGrid
{
	public DateOnly MonthStart { get; init; }

	public DateOnly MonthEnd { get; init; }

	public IReadOnlyList<CalendarWeek> Weeks { get; init; } = [];

	public bool HasAnyCompletions =>
		Weeks.SelectMany(w => w.Days).Any(d => d.CompletionLines.Count > 0);
}
