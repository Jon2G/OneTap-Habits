using OneTapHabits.Models;
using OneTapHabits.Services;

namespace OneTapHabits.Calendar;

public static class CalendarInsightsBuilder
{
	private const string PositiveAccentHex = "#22C55E";

	public static CalendarInsightsSnapshot Build(
		IReadOnlyList<Habit> habits,
		IReadOnlyList<HabitLog> historyLogs,
		DateOnly displayedMonth,
		DateOnly today,
		string? habitIdFilter,
		IStreakService streakService,
		IWeeklyProgressService weeklyProgress)
	{
		var active = habits.Where(h => h.IsActive).ToList();
		if (active.Count == 0)
		{
			return new CalendarInsightsSnapshot { ShowInsights = false };
		}

		var filtered = habitIdFilter is null
			? active
			: active.Where(h => h.Id == habitIdFilter).ToList();

		if (filtered.Count == 0)
		{
			return new CalendarInsightsSnapshot { ShowInsights = false };
		}

		var monthStart = new DateOnly(displayedMonth.Year, displayedMonth.Month, 1);
		var monthEnd = monthStart.AddMonths(1).AddDays(-1);
		var monthRangeEnd = monthEnd > today ? today : monthEnd;

		var data = habitIdFilter is not null && filtered.Count == 1
			? BuildSingleHabitData(filtered[0], historyLogs, monthStart, monthRangeEnd, today, streakService, weeklyProgress)
			: BuildAllHabitsData(active, historyLogs, monthStart, monthRangeEnd, today, streakService, weeklyProgress);

		var (headlineKey, headlineArgs) = CalendarMotivationResolver.Resolve(data);
		var metrics = BuildMetrics(data);
		var accent = data.IsSingleHabit ? data.FilteredHabit?.ColorHex : null;

		return new CalendarInsightsSnapshot
		{
			ShowInsights = true,
			AccentColorHex = accent,
			HeadlineKey = headlineKey,
			HeadlineArgs = headlineArgs,
			Metrics = metrics
		};
	}

	private static CalendarInsightsData BuildSingleHabitData(
		Habit habit,
		IReadOnlyList<HabitLog> historyLogs,
		DateOnly monthStart,
		DateOnly monthRangeEnd,
		DateOnly today,
		IStreakService streakService,
		IWeeklyProgressService weeklyProgress)
	{
		if (habit.ScheduleMode == HabitScheduleMode.TimesPerWeek)
		{
			var weekStart = WeekBoundaryHelper.GetWeekStart(today);
			var weekCompletions = weeklyProgress.CountCompletionsInWeek(habit.Id, weekStart, historyLogs);
			var weekStreak = weeklyProgress.CalculateWeeklyStreak(habit, historyLogs, today);
			var monthCompletions = CountCompletionsInRange(habit.Id, historyLogs, monthStart, monthRangeEnd);

			return new CalendarInsightsData
			{
				IsSingleHabit = true,
				FilteredHabit = habit,
				CurrentStreak = weekStreak,
				WeekCompletions = weekCompletions,
				WeekTarget = habit.TimesPerWeek,
				MonthTotalCompletions = monthCompletions,
				MonthCompletedDays = monthCompletions,
				MonthScheduledDays = Math.Max(habit.TimesPerWeek, 1),
				HasHistory = historyLogs.Any(l => l.HabitId == habit.Id && l.IsCompleted)
			};
		}

		var completionByDate = CompletionMapBuilder.BuildForHabit(habit.Id, historyLogs);
		var streak = streakService.CalculateCurrentStreak(habit, completionByDate, today);
		var (monthDone, monthScheduled) = CountMonthDailyProgress(habit, historyLogs, monthStart, monthRangeEnd);
		var weekStartDaily = WeekBoundaryHelper.GetWeekStart(today);
		var weekEndDaily = weekStartDaily.AddDays(6);
		var weekCompletionsDaily = CountCompletionsInRange(habit.Id, historyLogs, weekStartDaily, weekEndDaily);
		var todayCount = GetCountForDate(habit.Id, today, historyLogs);
		var dailyTarget = HabitDailyTargetHelper.GetDailyTarget(habit);
		var todayStatusKey = todayCount >= dailyTarget
			? "CalendarTodayDone"
			: todayCount > 0
				? "CalendarTodayPartial"
				: "CalendarTodayPending";

		return new CalendarInsightsData
		{
			IsSingleHabit = true,
			FilteredHabit = habit,
			CurrentStreak = streak,
			MonthCompletedDays = monthDone,
			MonthScheduledDays = monthScheduled,
			WeekCompletions = weekCompletionsDaily,
			WeekTarget = CountScheduledDays(habit, weekStartDaily, weekEndDaily),
			TodayStatusKey = todayStatusKey,
			HasHistory = historyLogs.Any(l => l.HabitId == habit.Id && l.IsCompleted)
		};
	}

	private static CalendarInsightsData BuildAllHabitsData(
		IReadOnlyList<Habit> habits,
		IReadOnlyList<HabitLog> historyLogs,
		DateOnly monthStart,
		DateOnly monthRangeEnd,
		DateOnly today,
		IStreakService streakService,
		IWeeklyProgressService weeklyProgress)
	{
		var monthTotal = 0;
		var monthScheduled = 0;
		var monthCompleted = 0;
		var todayScheduled = 0;
		var todayCompleted = 0;
		var habitsOnStreak = 0;
		var longestStreak = 0;
		string? longestName = null;

		foreach (var habit in habits)
		{
			if (habit.ScheduleMode == HabitScheduleMode.TimesPerWeek)
			{
				var weekStreak = weeklyProgress.CalculateWeeklyStreak(habit, historyLogs, today);
				if (weekStreak > 0)
				{
					habitsOnStreak++;
				}

				if (weekStreak > longestStreak)
				{
					longestStreak = weekStreak;
					longestName = habit.Name;
				}

				monthTotal += CountCompletionsInRange(habit.Id, historyLogs, monthStart, monthRangeEnd);

				if (IsWeeklyTargetMetToday(habit, historyLogs, today))
				{
					todayCompleted++;
				}

				todayScheduled++;
				continue;
			}

			var completionByDate = CompletionMapBuilder.BuildForHabit(habit.Id, historyLogs);
			var streak = streakService.CalculateCurrentStreak(habit, completionByDate, today);
			if (streak > 0)
			{
				habitsOnStreak++;
			}

			if (streak > longestStreak)
			{
				longestStreak = streak;
				longestName = habit.Name;
			}

			var (done, scheduled) = CountMonthDailyProgress(habit, historyLogs, monthStart, monthRangeEnd);
			monthCompleted += done;
			monthScheduled += scheduled;
			monthTotal += done;

			if (HabitScheduleHelper.IsVisibleOnDate(habit, today))
			{
				todayScheduled++;
				var count = GetCountForDate(habit.Id, today, historyLogs);
				if (HabitDailyTargetHelper.IsDailyTargetMet(habit, count))
				{
					todayCompleted++;
				}
			}
		}

		return new CalendarInsightsData
		{
			IsSingleHabit = false,
			LongestStreak = longestStreak,
			LongestStreakHabitName = longestName,
			HabitsOnStreak = habitsOnStreak,
			MonthTotalCompletions = monthTotal,
			MonthCompletedDays = monthCompleted,
			MonthScheduledDays = monthScheduled,
			TodayCompletedHabits = todayCompleted,
			TodayScheduledHabits = todayScheduled,
			HasHistory = historyLogs.Any(l => l.IsCompleted)
		};
	}

	private static List<CalendarInsightMetric> BuildMetrics(CalendarInsightsData data)
	{
		var metrics = new List<CalendarInsightMetric>();

		if (data.IsSingleHabit && data.FilteredHabit is not null)
		{
			var habit = data.FilteredHabit;
			if (habit.ScheduleMode == HabitScheduleMode.TimesPerWeek)
			{
				metrics.Add(new CalendarInsightMetric
				{
					LabelKey = "CalendarMetricCurrentStreak",
					Value = data.CurrentStreak.ToString(),
					AccentColorHex = data.CurrentStreak > 0 ? PositiveAccentHex : null
				});
				metrics.Add(new CalendarInsightMetric
				{
					LabelKey = "CalendarMetricWeekProgress",
					Value = $"{data.WeekCompletions}/{data.WeekTarget}"
				});
				metrics.Add(new CalendarInsightMetric
				{
					LabelKey = "CalendarMetricMonthCompletions",
					Value = data.MonthTotalCompletions.ToString()
				});
			}
			else
			{
				metrics.Add(new CalendarInsightMetric
				{
					LabelKey = "CalendarMetricCurrentStreak",
					Value = data.CurrentStreak.ToString(),
					AccentColorHex = data.CurrentStreak > 0 ? PositiveAccentHex : null
				});
				metrics.Add(new CalendarInsightMetric
				{
					LabelKey = "CalendarMetricMonthProgress",
					Value = $"{data.MonthCompletedDays}/{data.MonthScheduledDays}"
				});
				metrics.Add(new CalendarInsightMetric
				{
					LabelKey = "CalendarMetricWeekProgress",
					Value = data.WeekCompletions.ToString()
				});
				if (data.TodayStatusKey is not null)
				{
					metrics.Add(new CalendarInsightMetric
					{
						LabelKey = "CalendarMetricTodayStatus",
						ValueKey = data.TodayStatusKey
					});
				}
			}

			return metrics;
		}

		metrics.Add(new CalendarInsightMetric
		{
			LabelKey = "CalendarMetricMonthCompletions",
			Value = data.MonthTotalCompletions.ToString()
		});
		metrics.Add(new CalendarInsightMetric
		{
			LabelKey = "CalendarMetricTodayOverview",
			Value = $"{data.TodayCompletedHabits}/{data.TodayScheduledHabits}",
			AccentColorHex = data.AllDoneToday ? PositiveAccentHex : null
		});

		if (data.LongestStreak > 0 && data.LongestStreakHabitName is not null)
		{
			metrics.Add(new CalendarInsightMetric
			{
				LabelKey = "CalendarMetricLongestStreak",
				Value = $"{data.LongestStreak} · {data.LongestStreakHabitName}",
				AccentColorHex = PositiveAccentHex
			});
		}

		metrics.Add(new CalendarInsightMetric
		{
			LabelKey = "CalendarMetricHabitsOnStreak",
			Value = data.HabitsOnStreak.ToString(),
			AccentColorHex = data.HabitsOnStreak > 0 ? PositiveAccentHex : null
		});

		return metrics;
	}

	private static bool IsWeeklyTargetMetToday(Habit habit, IReadOnlyList<HabitLog> logs, DateOnly today)
	{
		var weekStart = WeekBoundaryHelper.GetWeekStart(today);
		var count = logs.Count(l =>
			l.HabitId == habit.Id &&
			l.IsCompleted &&
			l.Date >= weekStart &&
			l.Date <= today);
		return count >= habit.TimesPerWeek;
	}

	private static (int completed, int scheduled) CountMonthDailyProgress(
		Habit habit,
		IReadOnlyList<HabitLog> logs,
		DateOnly monthStart,
		DateOnly monthRangeEnd)
	{
		var completed = 0;
		var scheduled = 0;
		for (var date = monthStart; date <= monthRangeEnd; date = date.AddDays(1))
		{
			if (!HabitScheduleHelper.IsVisibleOnDate(habit, date))
			{
				continue;
			}

			scheduled++;
			var count = GetCountForDate(habit.Id, date, logs);
			if (HabitDailyTargetHelper.IsDailyTargetMet(habit, count))
			{
				completed++;
			}
		}

		return (completed, scheduled);
	}

	private static int CountScheduledDays(Habit habit, DateOnly start, DateOnly end)
	{
		var count = 0;
		for (var date = start; date <= end; date = date.AddDays(1))
		{
			if (HabitScheduleHelper.IsVisibleOnDate(habit, date))
			{
				count++;
			}
		}

		return count;
	}

	private static int CountCompletionsInRange(
		string habitId,
		IReadOnlyList<HabitLog> logs,
		DateOnly start,
		DateOnly end)
	{
		return logs.Count(l =>
			l.HabitId == habitId &&
			l.IsCompleted &&
			l.Date >= start &&
			l.Date <= end);
	}

	private static int GetCountForDate(string habitId, DateOnly date, IReadOnlyList<HabitLog> logs)
	{
		return logs
			.Where(l => l.HabitId == habitId && l.Date == date)
			.Sum(l => l.Count > 0 ? l.Count : l.IsCompleted ? 1 : 0);
	}
}
