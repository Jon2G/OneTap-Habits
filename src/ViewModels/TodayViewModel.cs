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

	[ObservableProperty]
	private DateOnly selectedDate = DateOnly.FromDateTime(DateTime.Today);

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

	[RelayCommand]
	private async Task PreviousDayAsync()
	{
		SelectedDate = SelectedDate.AddDays(-1);
		await LoadHabitsAsync();
	}

	[RelayCommand]
	private async Task NextDayAsync()
	{
		if (!CanGoToNextDay)
		{
			return;
		}

		SelectedDate = SelectedDate.AddDays(1);
		await LoadHabitsAsync();
	}

	[RelayCommand]
	private async Task GoToTodayAsync()
	{
		SelectedDate = Today;
		await LoadHabitsAsync();
	}

	private async Task LoadHabitsAsync()
	{
		await _firstLaunchSeed.SeedIfNeededAsync();

		var viewDate = SelectedDate;
		var habits = await _habitService.GetTodayHabitsAsync(viewDate);
		var countMap = await _logService.GetCountMapForDateAsync(viewDate);
		var historyStart = viewDate.AddDays(-StreakLookbackDays);
		var historyLogs = await _logService.GetCompletedLogsInRangeAsync(historyStart, viewDate);

		var editLabel = _localization.Get("EditHabit");
		var deleteLabel = _localization.Get("DeleteHabit");
		var swipeHint = _localization.Get("HabitSwipeHint");

		Habits.Clear();
		foreach (var habit in habits)
		{
			var count = countMap.TryGetValue(habit.Id, out var value) ? value : 0;
			var completionByDate = CompletionMapBuilder.BuildForHabit(habit.Id, historyLogs);
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

			Habits.Add(new TodayHabitItem(
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
				swipeHint));
		}

		if (IsViewingToday)
		{
			await _widgetRefresh.RefreshAsync();
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
		await _widgetRefresh.RefreshAsync();
	}

	[RelayCommand]
	private async Task IncrementHabitAsync(TodayHabitItem item)
	{
		if (HabitDailyTargetHelper.IsDailyTargetMet(item.Habit, item.TodayCount))
		{
			return;
		}

		await _logService.IncrementCountAsync(item.Habit.Id, SelectedDate);
		await LoadAsync();
	}

	[RelayCommand]
	private async Task UndoHabitAsync(TodayHabitItem item)
	{
		if (item.TodayCount <= 0)
		{
			return;
		}

		await _logService.SetCompletedAsync(item.Habit.Id, SelectedDate, false);
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
