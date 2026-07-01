using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneTapHabits.Models;
using OneTapHabits.Services;
using OneTapHabits.Services.Widget;

namespace OneTapHabits.ViewModels;

public partial class TodayViewModel : ObservableObject, IQueryAttributable
{
	private const int StreakLookbackDays = 400;
	private const string StreakPlaceholder = "—";

	private readonly IAuthService _authService;
	private readonly IHabitService _habitService;
	private readonly ILogService _logService;
	private readonly IStreakService _streakService;
	private readonly IWeeklyProgressService _weeklyProgress;
	private readonly ILocalizationService _localization;
	private readonly IWidgetRefreshService _widgetRefresh;
	private readonly IFirstLaunchSeedService _firstLaunchSeed;
	private readonly IHabitReminderService _reminderService;
	private readonly ICloudSyncService _cloudSync;

	public ObservableCollection<TodayHabitItem> Habits { get; } = [];

	[ObservableProperty]
	private bool isRefreshing;

	[ObservableProperty]
	private DateOnly selectedDate = DateOnly.FromDateTime(DateTime.Today);

	public TodayViewModel(
		IAuthService authService,
		IHabitService habitService,
		ILogService logService,
		IStreakService streakService,
		IWeeklyProgressService weeklyProgress,
		ILocalizationService localization,
		IWidgetRefreshService widgetRefresh,
		IFirstLaunchSeedService firstLaunchSeed,
		IHabitReminderService reminderService,
		ICloudSyncService cloudSync)
	{
		_authService = authService;
		_habitService = habitService;
		_logService = logService;
		_streakService = streakService;
		_weeklyProgress = weeklyProgress;
		_localization = localization;
		_widgetRefresh = widgetRefresh;
		_firstLaunchSeed = firstLaunchSeed;
		_reminderService = reminderService;
		_cloudSync = cloudSync;
	}

	private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

	public bool IsViewingToday => SelectedDate == Today;

	public bool ShowGoToToday => !IsViewingToday;

	public bool CanGoToNextDay => SelectedDate < Today;

	public string Title => ViewDateNavigationHelper.FormatDayTitle(
		SelectedDate,
		Today,
		_localization.Get("TodayTitle"),
		_localization.Get("YesterdayTitle"));

	public string AddHabitLabel => _localization.Get("AddHabit");
	public string SettingsLabel => _localization.Get("Settings");
	public string PreviousDayLabel => _localization.Get("PreviousDay");
	public string NextDayLabel => _localization.Get("NextDay");
	public string GoToTodayLabel => _localization.Get("GoToToday");

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("date", out var value) &&
		    value is string dateText &&
		    ViewDateNavigationHelper.TryParseQueryDate(dateText, out var date))
		{
			SelectedDate = ViewDateNavigationHelper.ClampToTodayOrPast(date, Today);
			OnPropertyChanged(nameof(Title));
			OnPropertyChanged(nameof(IsViewingToday));
			OnPropertyChanged(nameof(ShowGoToToday));
			OnPropertyChanged(nameof(CanGoToNextDay));
			_ = LoadAsync();
		}
	}

	partial void OnSelectedDateChanged(DateOnly value)
	{
		OnPropertyChanged(nameof(Title));
		OnPropertyChanged(nameof(IsViewingToday));
		OnPropertyChanged(nameof(ShowGoToToday));
		OnPropertyChanged(nameof(CanGoToNextDay));
	}

	[RelayCommand]
	public async Task LoadAsync()
	{
		await LoadHabitsAsync(syncFromCloud: false);
		RequestBackgroundCloudSync();
	}

	[RelayCommand]
	private async Task RefreshAsync()
	{
		try
		{
			if (!_authService.IsGuest)
			{
				await _cloudSync.SyncFromCloudAsync();
			}

			await LoadHabitsAsync(syncFromCloud: false);
		}
		finally
		{
			IsRefreshing = false;
		}
	}

	[RelayCommand]
	private async Task PreviousDayAsync()
	{
		SelectedDate = SelectedDate.AddDays(-1);
		await LoadHabitsAsync(syncFromCloud: false);
	}

	[RelayCommand]
	private async Task NextDayAsync()
	{
		if (!CanGoToNextDay)
		{
			return;
		}

		SelectedDate = SelectedDate.AddDays(1);
		await LoadHabitsAsync(syncFromCloud: false);
	}

	[RelayCommand]
	private async Task GoToTodayAsync()
	{
		SelectedDate = Today;
		await LoadHabitsAsync(syncFromCloud: false);
	}

	public async Task ReloadFromCacheAsync()
	{
		await LoadHabitsAsync(syncFromCloud: false);
	}

	private async Task LoadHabitsAsync(bool syncFromCloud)
	{
		if (syncFromCloud && !_authService.IsGuest)
		{
			await _cloudSync.SyncFromCloudAsync();
		}

		await _firstLaunchSeed.SeedIfNeededAsync();

		var viewDate = SelectedDate;
		var habits = await _habitService.GetTodayHabitsAsync(viewDate);
		var countMap = await _logService.GetCountMapForDateAsync(viewDate);

		var editLabel = _localization.Get("EditHabit");
		var deleteLabel = _localization.Get("DeleteHabit");
		var swipeHint = _localization.Get("HabitSwipeHint");

		Habits.Clear();
		foreach (var habit in habits)
		{
			var count = countMap.TryGetValue(habit.Id, out var value) ? value : 0;
			Habits.Add(BuildTodayHabitItemFast(habit, count, editLabel, deleteLabel, swipeHint));
		}

		if (IsViewingToday)
		{
			await _widgetRefresh.RefreshAsync(habits, countMap);
		}

		await EnrichStreakLabelsAsync(viewDate, habits, countMap, editLabel, deleteLabel, swipeHint);
	}

	private async Task EnrichStreakLabelsAsync(
		DateOnly viewDate,
		IReadOnlyList<Habit> habits,
		IReadOnlyDictionary<string, int> countMap,
		string editLabel,
		string deleteLabel,
		string swipeHint)
	{
		var historyStart = viewDate.AddDays(-StreakLookbackDays);
		var historyLogs = await _logService.GetCompletedLogsInRangeAsync(historyStart, viewDate);

		for (var i = 0; i < habits.Count; i++)
		{
			var habit = habits[i];
			var count = countMap.TryGetValue(habit.Id, out var value) ? value : 0;
			var completionByDate = CompletionMapBuilder.BuildForHabit(habit.Id, historyLogs);
			Habits[i] = BuildTodayHabitItem(habit, count, completionByDate, historyLogs, editLabel, deleteLabel, swipeHint);
		}
	}

	private void RequestBackgroundCloudSync()
	{
		if (!_authService.IsGuest)
		{
			_cloudSync.RequestBackgroundSync();
		}
	}

	public async Task PersistHabitOrderAsync()
	{
		if (!IsViewingToday)
		{
			return;
		}

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

		var today = DateOnly.FromDateTime(DateTime.Today);
		var habits = await _habitService.GetTodayHabitsAsync(today);
		var countMap = await _logService.GetCountMapForDateAsync(today);
		await _widgetRefresh.RefreshAsync(habits, countMap);
	}

	[RelayCommand]
	private async Task IncrementHabitAsync(TodayHabitItem item)
	{
		if (HabitDailyTargetHelper.IsDailyTargetMet(item.Habit, item.TodayCount))
		{
			return;
		}

		var next = await _logService.IncrementCountAsync(item.Habit.Id, SelectedDate);
		ReplaceHabitCount(item.Habit.Id, next);
		_ = RefreshHabitsInBackgroundAsync();
	}

	[RelayCommand]
	private async Task UndoHabitAsync(TodayHabitItem item)
	{
		if (item.TodayCount <= 0)
		{
			return;
		}

		await _logService.SetCompletedAsync(item.Habit.Id, SelectedDate, false);
		ReplaceHabitCount(item.Habit.Id, 0);
		_ = RefreshHabitsInBackgroundAsync();
	}

	private async Task RefreshHabitsInBackgroundAsync()
	{
		try
		{
			await LoadHabitsAsync(syncFromCloud: false);
		}
		catch
		{
			// Optimistic UI already updated; background refresh is best-effort.
		}
	}

	private void ReplaceHabitCount(string habitId, int newCount)
	{
		for (var i = 0; i < Habits.Count; i++)
		{
			if (Habits[i].Habit.Id != habitId)
			{
				continue;
			}

			Habits[i] = RebuildTodayHabitItem(Habits[i], newCount);
			return;
		}
	}

	private TodayHabitItem RebuildTodayHabitItem(TodayHabitItem existing, int newCount)
	{
		var habit = existing.Habit;
		var dailyTarget = existing.DailyTarget;
		var secondaryLabel = habit.ScheduleMode == HabitScheduleMode.TimesPerWeek
			? existing.CompletedLabel
			: dailyTarget > 1
				? string.Format(_localization.Get("DailyProgressFormat"), newCount, dailyTarget)
				: newCount > 0
					? _localization.Get("Completed")
					: _localization.Get("TapToComplete");

		return new TodayHabitItem(
			habit,
			newCount,
			dailyTarget,
			existing.StreakLabel,
			secondaryLabel,
			IncrementHabitCommand,
			UndoHabitCommand,
			EditHabitCommand,
			DeleteHabitCommand,
			existing.EditLabel,
			existing.DeleteLabel,
			existing.SwipeHint);
	}

	private TodayHabitItem BuildTodayHabitItemFast(
		Habit habit,
		int count,
		string editLabel,
		string deleteLabel,
		string swipeHint)
	{
		var dailyTarget = HabitDailyTargetHelper.GetDailyTarget(habit);
		var secondaryLabel = habit.ScheduleMode == HabitScheduleMode.TimesPerWeek
			? StreakPlaceholder
			: dailyTarget > 1
				? string.Format(_localization.Get("DailyProgressFormat"), count, dailyTarget)
				: count > 0
					? _localization.Get("Completed")
					: _localization.Get("TapToComplete");

		return new TodayHabitItem(
			habit,
			count,
			dailyTarget,
			StreakPlaceholder,
			secondaryLabel,
			IncrementHabitCommand,
			UndoHabitCommand,
			EditHabitCommand,
			DeleteHabitCommand,
			editLabel,
			deleteLabel,
			swipeHint);
	}

	private TodayHabitItem BuildTodayHabitItem(
		Habit habit,
		int count,
		IReadOnlyDictionary<DateOnly, bool> completionByDate,
		IReadOnlyList<HabitLog> historyLogs,
		string editLabel,
		string deleteLabel,
		string swipeHint)
	{
		var viewDate = SelectedDate;
		var dailyTarget = HabitDailyTargetHelper.GetDailyTarget(habit);

		string primaryLabel;
		string secondaryLabel;

		if (habit.ScheduleMode == HabitScheduleMode.TimesPerWeek)
		{
			var weekStart = WeekBoundaryHelper.GetWeekStart(viewDate);
			var weekCount = _weeklyProgress.CountCompletionsInWeek(habit.Id, weekStart, historyLogs);
			var weeklyStreak = _weeklyProgress.CalculateWeeklyStreak(habit, historyLogs, viewDate);
			primaryLabel = string.Format(_localization.Get("WeeklyProgressFormat"), weekCount, habit.TimesPerWeek);
			secondaryLabel = string.Format(_localization.Get("WeeklyStreakFormat"), weeklyStreak);
		}
		else
		{
			var streak = _streakService.CalculateCurrentStreak(habit, completionByDate, viewDate);
			primaryLabel = string.Format(_localization.Get("Streak"), streak);
			secondaryLabel = dailyTarget > 1
				? string.Format(_localization.Get("DailyProgressFormat"), count, dailyTarget)
				: count > 0
					? _localization.Get("Completed")
					: _localization.Get("TapToComplete");
		}

		return new TodayHabitItem(
			habit,
			count,
			dailyTarget,
			primaryLabel,
			secondaryLabel,
			IncrementHabitCommand,
			UndoHabitCommand,
			EditHabitCommand,
			DeleteHabitCommand,
			editLabel,
			deleteLabel,
			swipeHint);
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
	ICommand undoCommand,
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
	public ICommand UndoCommand { get; } = undoCommand;
	public ICommand EditCommand { get; } = editCommand;
	public ICommand DeleteCommand { get; } = deleteCommand;
	public string EditLabel { get; } = editLabel;
	public string DeleteLabel { get; } = deleteLabel;
	public string SwipeHint { get; } = swipeHint;
	public Color AccentColor => Color.FromArgb(Habit.ColorHex);

	public bool IsDailyTargetMet => TodayCount >= DailyTarget;

	public Color CompletedStrokeColor => TodayCount > 0
		? Color.FromArgb("#22C55E")
		: Color.FromArgb("#E5E7EB");

	public Color CardBackgroundColor => TodayCount > 0
		? (IsDarkTheme ? Color.FromArgb("#1A2B22") : Color.FromArgb("#F0FDF4"))
		: (IsDarkTheme ? Color.FromArgb("#1E1E1E") : Color.FromArgb("#FFFFFF"));

	private static bool IsDarkTheme =>
		Application.Current?.RequestedTheme == AppTheme.Dark;

	public Color CompletedTextColor => TodayCount > 0
		? Color.FromArgb("#22C55E")
		: Color.FromArgb("#6B7280");
}
