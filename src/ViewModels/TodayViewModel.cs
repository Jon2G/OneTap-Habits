using System.Collections.ObjectModel;
using System.Windows.Input;
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
	private readonly IHabitReminderService _reminderService;

	public ObservableCollection<TodayHabitItem> Habits { get; } = [];

	[ObservableProperty]
	private bool isRefreshing;

	public TodayViewModel(
		IHabitService habitService,
		ILogService logService,
		IStreakService streakService,
		IWeeklyProgressService weeklyProgress,
		ILocalizationService localization,
		IWidgetRefreshService widgetRefresh,
		IFirstLaunchSeedService firstLaunchSeed,
		IHabitReminderService reminderService)
	{
		_habitService = habitService;
		_logService = logService;
		_streakService = streakService;
		_weeklyProgress = weeklyProgress;
		_localization = localization;
		_widgetRefresh = widgetRefresh;
		_firstLaunchSeed = firstLaunchSeed;
		_reminderService = reminderService;
	}

	public string Title => _localization.Get("TodayTitle");
	public string AddHabitLabel => _localization.Get("AddHabit");
	public string SettingsLabel => _localization.Get("Settings");

	[RelayCommand]
	public async Task LoadAsync()
	{
		await LoadHabitsAsync();
	}

	[RelayCommand]
	private async Task RefreshAsync()
	{
		try
		{
			await LoadHabitsAsync();
		}
		finally
		{
			IsRefreshing = false;
		}
	}

	private async Task LoadHabitsAsync()
	{
		await _firstLaunchSeed.SeedIfNeededAsync();

		var today = DateOnly.FromDateTime(DateTime.Today);
		var habits = await _habitService.GetTodayHabitsAsync(today);
		var countMap = await _logService.GetCountMapForDateAsync(today);
		var historyStart = today.AddDays(-StreakLookbackDays);
		var historyLogs = await _logService.GetCompletedLogsInRangeAsync(historyStart, today);

		var editLabel = _localization.Get("EditHabit");
		var deleteLabel = _localization.Get("DeleteHabit");
		var swipeHint = _localization.Get("HabitSwipeHint");

		Habits.Clear();
		foreach (var habit in habits)
		{
			var count = countMap.TryGetValue(habit.Id, out var value) ? value : 0;
			if (HabitDailyTargetHelper.IsDailyTargetMet(habit, count))
			{
				continue;
			}

			var completionByDate = CompletionMapBuilder.BuildForHabit(habit.Id, historyLogs);
			var dailyTarget = HabitDailyTargetHelper.GetDailyTarget(habit);

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
				secondaryLabel = dailyTarget > 1
					? string.Format(_localization.Get("DailyProgressFormat"), count, dailyTarget)
					: count > 0
						? _localization.Get("Completed")
						: _localization.Get("TapToComplete");
			}

			Habits.Add(new TodayHabitItem(
				habit,
				count,
				dailyTarget,
				primaryLabel,
				secondaryLabel,
				IncrementHabitCommand,
				EditHabitCommand,
				DeleteHabitCommand,
				editLabel,
				deleteLabel,
				swipeHint));
		}

		await _widgetRefresh.RefreshAsync();
	}

	public async Task PersistHabitOrderAsync()
	{
		var visibleOrder = Habits.Select(h => h.Habit.Id).ToList();
		if (visibleOrder.Count == 0)
		{
			return;
		}

		var allHabits = await _habitService.GetActiveHabitsAsync();
		var rest = allHabits
			.Where(h => !visibleOrder.Contains(h.Id))
			.Select(h => h.Id);
		var fullOrder = visibleOrder.Concat(rest).ToList();

		await _habitService.ReorderHabitsAsync(fullOrder);
		await _widgetRefresh.RefreshAsync();
	}

	[RelayCommand]
	private async Task IncrementHabitAsync(TodayHabitItem item)
	{
		var today = DateOnly.FromDateTime(DateTime.Today);
		await _logService.IncrementCountAsync(item.Habit.Id, today);
		await LoadAsync();
	}

	[RelayCommand]
	private async Task EditHabitAsync(TodayHabitItem item)
	{
		await Shell.Current.GoToAsync($"habitForm?habitId={Uri.EscapeDataString(item.Habit.Id)}");
	}

	[RelayCommand]
	private async Task DeleteHabitAsync(TodayHabitItem item)
	{
		var page = Shell.Current?.CurrentPage;
		if (page is null)
		{
			return;
		}

		var confirmed = await page.DisplayAlert(
			_localization.Get("DeleteHabitTitle"),
			string.Format(_localization.Get("DeleteHabitMessage"), item.Habit.Name),
			_localization.Get("DeleteHabit"),
			_localization.Get("Cancel"));

		if (!confirmed)
		{
			return;
		}

		try
		{
			await _habitService.DeleteHabitAsync(item.Habit.Id);
			await _reminderService.CancelAsync(item.Habit.Id);
			await LoadAsync();
		}
		catch (Exception ex)
		{
			await page.DisplayAlert(
				_localization.Get("AppTitle"),
				UserFriendlyErrorMapper.FromException(ex, _localization),
				"OK");
		}
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

public sealed class TodayHabitItem(
	Habit habit,
	int todayCount,
	int dailyTarget,
	string streakLabel,
	string completedLabel,
	ICommand incrementCommand,
	ICommand editCommand,
	ICommand deleteCommand,
	string editLabel,
	string deleteLabel,
	string swipeHint)
{
	public Habit Habit { get; } = habit;
	public int TodayCount { get; } = todayCount;
	public int DailyTarget { get; } = dailyTarget;
	public string StreakLabel { get; } = streakLabel;
	public string CompletedLabel { get; } = completedLabel;
	public ICommand IncrementCommand { get; } = incrementCommand;
	public ICommand EditCommand { get; } = editCommand;
	public ICommand DeleteCommand { get; } = deleteCommand;
	public string EditLabel { get; } = editLabel;
	public string DeleteLabel { get; } = deleteLabel;
	public string SwipeHint { get; } = swipeHint;
	public Color AccentColor => Color.FromArgb(Habit.ColorHex);

	public bool HasProgress => DailyTarget > 1 && TodayCount > 0;

	public Color CompletedStrokeColor => HasProgress || TodayCount > 0
		? Color.FromArgb("#22C55E")
		: Color.FromArgb("#E5E7EB");

	public Color CardBackgroundColor => HasProgress || TodayCount > 0
		? (IsDarkTheme ? Color.FromArgb("#1A2B22") : Color.FromArgb("#F0FDF4"))
		: (IsDarkTheme ? Color.FromArgb("#1E1E1E") : Color.FromArgb("#FFFFFF"));

	private static bool IsDarkTheme =>
		Application.Current?.RequestedTheme == AppTheme.Dark;

	public Color CompletedTextColor => HasProgress || TodayCount > 0
		? Color.FromArgb("#22C55E")
		: Color.FromArgb("#6B7280");
}
