using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneTapHabits.Calendar;
using OneTapHabits.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace OneTapHabits.ViewModels;

public partial class CalendarViewModel : ObservableObject
{
	private readonly IHabitService _habitService;
	private readonly ILogService _logService;
	private readonly ILocalizationService _localization;
	private bool _suppressFilterReload;

	[ObservableProperty]
	private DateOnly displayedMonth;

	[ObservableProperty]
	private bool isBusy;

	[ObservableProperty]
	private HabitFilterOption? selectedFilter;

	[ObservableProperty]
	private bool showEmptyMessage;

	public ObservableCollection<CalendarWeekRow> Weeks { get; } = [];
	public ObservableCollection<HabitFilterOption> HabitFilterOptions { get; } = [];
	public ObservableCollection<string> WeekdayHeaders { get; } = [];

	public CalendarViewModel(
		IHabitService habitService,
		ILogService logService,
		ILocalizationService localization)
	{
		_habitService = habitService;
		_logService = logService;
		_localization = localization;

		var today = DateOnly.FromDateTime(DateTime.Today);
		displayedMonth = new DateOnly(today.Year, today.Month, 1);
	}

	public string Title => _localization.Get("CalendarTitle");
	public string SettingsLabel => _localization.Get("Settings");
	public string AllHabitsFilterLabel => _localization.Get("AllHabitsFilter");
	public string EmptyMessage => _localization.Get("NoCompletionsThisMonth");
	public string PreviousMonthLabel => _localization.Get("PreviousMonth");
	public string NextMonthLabel => _localization.Get("NextMonth");

	public string MonthTitle
	{
		get
		{
			var culture = CultureInfo.CurrentUICulture;
			var format = culture.TwoLetterISOLanguageName == "es" ? "MMMM yyyy" : "MMMM yyyy";
			return DisplayedMonth.ToDateTime(TimeOnly.MinValue).ToString(format, culture);
		}
	}

	public bool CanGoToNextMonth
	{
		get
		{
			var today = DateOnly.FromDateTime(DateTime.Today);
			var currentMonth = new DateOnly(today.Year, today.Month, 1);
			return DisplayedMonth < currentMonth;
		}
	}

	partial void OnDisplayedMonthChanged(DateOnly value)
	{
		OnPropertyChanged(nameof(MonthTitle));
		OnPropertyChanged(nameof(CanGoToNextMonth));
	}

	partial void OnSelectedFilterChanged(HabitFilterOption? value)
	{
		if (!_suppressFilterReload)
		{
			_ = LoadAsync();
		}
	}

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
			RebuildWeekdayHeaders();
			var habits = await _habitService.GetActiveHabitsAsync();
			RebuildFilterOptions(habits);

			var gridStart = CalendarMonthBuilder.StartOfWeek(DisplayedMonth);
			var monthEnd = DisplayedMonth.AddMonths(1).AddDays(-1);
			var gridEnd = CalendarMonthBuilder.EndOfWeek(monthEnd);

			var logs = await _logService.GetCompletedLogsInRangeAsync(gridStart, gridEnd);
			var filterId = SelectedFilter?.HabitId;
			var today = DateOnly.FromDateTime(DateTime.Today);

			var grid = CalendarMonthBuilder.Build(DisplayedMonth, habits, logs, filterId, today);

			Weeks.Clear();
			foreach (var week in grid.Weeks)
			{
				Weeks.Add(CalendarWeekRow.FromWeek(week));
			}

			ShowEmptyMessage = !grid.HasAnyCompletions;
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task PreviousMonthAsync()
	{
		DisplayedMonth = DisplayedMonth.AddMonths(-1);
		await LoadAsync();
	}

	[RelayCommand]
	private async Task NextMonthAsync()
	{
		if (!CanGoToNextMonth)
		{
			return;
		}

		DisplayedMonth = DisplayedMonth.AddMonths(1);
		await LoadAsync();
	}

	[RelayCommand]
	private async Task OpenSettingsAsync()
	{
		await Shell.Current.GoToAsync("settings");
	}

	private void RebuildWeekdayHeaders()
	{
		WeekdayHeaders.Clear();
		WeekdayHeaders.Add(_localization.Get("DayMonShort"));
		WeekdayHeaders.Add(_localization.Get("DayTueShort"));
		WeekdayHeaders.Add(_localization.Get("DayWedShort"));
		WeekdayHeaders.Add(_localization.Get("DayThuShort"));
		WeekdayHeaders.Add(_localization.Get("DayFriShort"));
		WeekdayHeaders.Add(_localization.Get("DaySatShort"));
		WeekdayHeaders.Add(_localization.Get("DaySunShort"));
	}

	private void RebuildFilterOptions(IReadOnlyList<Models.Habit> habits)
	{
		var currentId = SelectedFilter?.HabitId;
		HabitFilterOptions.Clear();
		HabitFilterOptions.Add(HabitFilterOption.All(AllHabitsFilterLabel));

		foreach (var habit in habits.OrderBy(h => h.Name, StringComparer.OrdinalIgnoreCase))
		{
			HabitFilterOptions.Add(new HabitFilterOption(habit.Id, habit.Name, habit.ColorHex));
		}

		_suppressFilterReload = true;
		SelectedFilter = HabitFilterOptions.FirstOrDefault(o => o.HabitId == currentId)
			?? HabitFilterOptions.FirstOrDefault();
		_suppressFilterReload = false;
	}
}

public sealed class HabitFilterOption(string? habitId, string name, string? colorHex)
{
	public string? HabitId { get; } = habitId;
	public string Name { get; } = name;
	public string? ColorHex { get; } = colorHex;

	public static HabitFilterOption All(string label) => new(null, label, null);
}

public sealed class CalendarWeekRow
{
	public CalendarDayDisplay Day0 { get; init; } = CalendarDayDisplay.Empty;
	public CalendarDayDisplay Day1 { get; init; } = CalendarDayDisplay.Empty;
	public CalendarDayDisplay Day2 { get; init; } = CalendarDayDisplay.Empty;
	public CalendarDayDisplay Day3 { get; init; } = CalendarDayDisplay.Empty;
	public CalendarDayDisplay Day4 { get; init; } = CalendarDayDisplay.Empty;
	public CalendarDayDisplay Day5 { get; init; } = CalendarDayDisplay.Empty;
	public CalendarDayDisplay Day6 { get; init; } = CalendarDayDisplay.Empty;

	public static CalendarWeekRow FromWeek(CalendarWeek week)
	{
		var days = week.Days.Select(CalendarDayDisplay.FromCell).ToList();
		while (days.Count < 7)
		{
			days.Add(CalendarDayDisplay.Empty);
		}

		return new CalendarWeekRow
		{
			Day0 = days[0],
			Day1 = days[1],
			Day2 = days[2],
			Day3 = days[3],
			Day4 = days[4],
			Day5 = days[5],
			Day6 = days[6]
		};
	}
}

public sealed class CalendarDayDisplay
{
	public static CalendarDayDisplay Empty { get; } = new();

	public int DayNumber { get; init; }
	public bool IsCurrentMonth { get; init; }
	public bool IsToday { get; init; }
	public bool HasOverflow { get; init; }
	public string OverflowText { get; init; } = string.Empty;
	public IReadOnlyList<CalendarLineDisplay> Lines { get; init; } = [];

	public static CalendarDayDisplay FromCell(CalendarDayCell cell) => new()
	{
		DayNumber = cell.Date.Day,
		IsCurrentMonth = cell.IsCurrentMonth,
		IsToday = cell.IsToday,
		Lines = cell.VisibleLines.Select(l => new CalendarLineDisplay(l.ColorHex)).ToList(),
		HasOverflow = cell.OverflowCount > 0,
		OverflowText = $"+{cell.OverflowCount}"
	};
}

public sealed class CalendarLineDisplay(string colorHex)
{
	public Color LineColor { get; } = Color.FromArgb(colorHex);
}
