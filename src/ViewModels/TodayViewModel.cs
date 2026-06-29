using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneTapHabits.Models;
using OneTapHabits.Services;
using OneTapHabits.Services.Widget;

namespace OneTapHabits.ViewModels;

public partial class TodayViewModel : ObservableObject
{
	private const int StreakLookbackDays = 400;

	private readonly IHabitService _habitService;
	private readonly ILogService _logService;
	private readonly IStreakService _streakService;
	private readonly IWeeklyProgressService _weeklyProgress;
	private readonly ILocalizationService _localization;
	private readonly IWidgetRefreshService _widgetRefresh;
	private readonly IFirstLaunchSeedService _firstLaunchSeed;

	public ObservableCollection<TodayHabitItem> Habits { get; } = [];

	[ObservableProperty]
	private bool isBusy;

	public TodayViewModel(
		IHabitService habitService,
		ILogService logService,
		IStreakService streakService,
		IWeeklyProgressService weeklyProgress,
		ILocalizationService localization,
		IWidgetRefreshService widgetRefresh,
		IFirstLaunchSeedService firstLaunchSeed)
	{
		_habitService = habitService;
		_logService = logService;
		_streakService = streakService;
		_weeklyProgress = weeklyProgress;
		_localization = localization;
		_widgetRefresh = widgetRefresh;
		_firstLaunchSeed = firstLaunchSeed;
	}

	public string Title => _localization.Get("TodayTitle");
	public string AddHabitLabel => _localization.Get("AddHabit");
	public string SettingsLabel => _localization.Get("Settings");

	[RelayCommand]
	public async Task LoadAsync()
	{
		if (IsBusy)
		{
			return;
		}

		IsBusy = true;
		try
		{
			await _firstLaunchSeed.SeedIfNeededAsync();

			var today = DateOnly.FromDateTime(DateTime.Today);
			var habits = await _habitService.GetTodayHabitsAsync(today);
			var completionMap = await _logService.GetCompletionMapForDateAsync(today);
			var historyStart = today.AddDays(-StreakLookbackDays);
			var historyLogs = await _logService.GetCompletedLogsInRangeAsync(historyStart, today);

			Habits.Clear();
			foreach (var habit in habits)
			{
				var isCompleted = completionMap.TryGetValue(habit.Id, out var completed) && completed;
				var completionByDate = CompletionMapBuilder.BuildForHabit(habit.Id, historyLogs);

				string primaryLabel;
				string secondaryLabel;

				if (habit.ScheduleMode == HabitScheduleMode.TimesPerWeek)
				{
					var weekStart = WeekBoundaryHelper.GetWeekStart(today);
					var weekCount = _weeklyProgress.CountCompletionsInWeek(habit.Id, weekStart, historyLogs);
					var weeklyStreak = _weeklyProgress.CalculateWeeklyStreak(habit, historyLogs, today);
					primaryLabel = string.Format(_localization.Get("WeeklyProgressFormat"), weekCount, habit.TimesPerWeek);
					secondaryLabel = string.Format(_localization.Get("WeeklyStreakFormat"), weeklyStreak);
				}
				else
				{
					var streak = _streakService.CalculateCurrentStreak(habit, completionByDate, today);
					primaryLabel = string.Format(_localization.Get("Streak"), streak);
					secondaryLabel = isCompleted ? _localization.Get("Completed") : _localization.Get("TapToComplete");
				}

				Habits.Add(new TodayHabitItem(habit, isCompleted, primaryLabel, secondaryLabel));
			}

			await _widgetRefresh.RefreshAsync();
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task ToggleHabitAsync(TodayHabitItem item)
	{
		var today = DateOnly.FromDateTime(DateTime.Today);
		var next = !item.IsCompleted;
		await _logService.SetCompletedAsync(item.Habit.Id, today, next);
		await LoadAsync();
	}

	[RelayCommand]
	private async Task AddHabitAsync()
	{
		await Shell.Current.GoToAsync("habitForm");
	}

	[RelayCommand]
	private async Task OpenSettingsAsync()
	{
		await Shell.Current.GoToAsync("settings");
	}
}

public sealed class TodayHabitItem(Habit habit, bool isCompleted, string streakLabel, string completedLabel)
{
	public Habit Habit { get; } = habit;
	public bool IsCompleted { get; } = isCompleted;
	public string StreakLabel { get; } = streakLabel;
	public string CompletedLabel { get; } = completedLabel;
	public Color AccentColor => Color.FromArgb(Habit.ColorHex);

	public Color CompletedStrokeColor => IsCompleted
		? Color.FromArgb("#22C55E")
		: Color.FromArgb("#E5E7EB");

	public Color CardBackgroundColor => IsCompleted
		? Color.FromArgb("#2222C55E")
		: Colors.Transparent;

	public Color CompletedTextColor => IsCompleted
		? Color.FromArgb("#22C55E")
		: Color.FromArgb("#6B7280");
}
