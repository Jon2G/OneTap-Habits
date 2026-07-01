namespace OneTapHabits.Calendar;

using OneTapHabits.Models;

public sealed class CalendarInsightsSnapshot
{
	public bool ShowInsights { get; init; }

	public string? AccentColorHex { get; init; }

	public string HeadlineKey { get; init; } = "CalendarMotivationKeepGoing";

	public object[] HeadlineArgs { get; init; } = [];

	public IReadOnlyList<CalendarInsightMetric> Metrics { get; init; } = [];
}

public sealed class CalendarInsightMetric
{
	public string LabelKey { get; init; } = string.Empty;

	public string Value { get; init; } = string.Empty;

	public string? ValueKey { get; init; }

	public string? AccentColorHex { get; init; }
}

public sealed class CalendarInsightsData
{
	public bool IsSingleHabit { get; init; }

	public Habit? FilteredHabit { get; init; }

	public int CurrentStreak { get; init; }

	public int LongestStreak { get; init; }

	public string? LongestStreakHabitName { get; init; }

	public int HabitsOnStreak { get; init; }

	public int MonthCompletedDays { get; init; }

	public int MonthScheduledDays { get; init; }

	public int MonthTotalCompletions { get; init; }

	public int TodayCompletedHabits { get; init; }

	public int TodayScheduledHabits { get; init; }

	public int WeekCompletions { get; init; }

	public int WeekTarget { get; init; }

	public bool HasHistory { get; init; }

	public string? TodayStatusKey { get; init; }

	public double MonthPace =>
		MonthScheduledDays > 0 ? (double)MonthCompletedDays / MonthScheduledDays : 0;

	public bool AllDoneToday =>
		TodayScheduledHabits > 0 && TodayCompletedHabits >= TodayScheduledHabits;
}
