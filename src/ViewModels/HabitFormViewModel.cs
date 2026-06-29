using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneTapHabits.Models;
using OneTapHabits.Services;
using OneTapHabits.Services.Widget;
using System.Collections.ObjectModel;

namespace OneTapHabits.ViewModels;

public partial class HabitFormViewModel : ObservableObject, IQueryAttributable
{
	private static readonly int[] AllDays = [1, 2, 3, 4, 5, 6, 7];

	private readonly IHabitService _habitService;
	private readonly ILocalizationService _localization;
	private readonly IWidgetRefreshService _widgetRefresh;

	private string? _editingHabitId;

	[ObservableProperty]
	private string name = string.Empty;

	[ObservableProperty]
	private string selectedColorHex = HabitColorPalette.Default;

	[ObservableProperty]
	private string? errorMessage;

	[ObservableProperty]
	private bool showInWidget = true;

	[ObservableProperty]
	private bool isEveryDay = true;

	[ObservableProperty]
	private HabitScheduleMode scheduleMode = HabitScheduleMode.SpecificDays;

	[ObservableProperty]
	private int timesPerWeek = 3;

	public bool IsEditing => !string.IsNullOrWhiteSpace(_editingHabitId);
	public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
	public bool ShowSpecificDaySelection => ScheduleMode == HabitScheduleMode.SpecificDays && !IsEveryDay;
	public bool ShowSpecificDaysSection => ScheduleMode == HabitScheduleMode.SpecificDays;
	public bool ShowTimesPerWeekSection => ScheduleMode == HabitScheduleMode.TimesPerWeek;
	public bool IsSpecificDaysMode => ScheduleMode == HabitScheduleMode.SpecificDays;
	public bool IsTimesPerWeekMode => ScheduleMode == HabitScheduleMode.TimesPerWeek;

	partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

	public ObservableCollection<DayToggleItem> WeekDays { get; } = [];
	public ObservableCollection<ColorSwatchItem> ColorOptions { get; } = [];

	public HabitFormViewModel(
		IHabitService habitService,
		ILocalizationService localization,
		IWidgetRefreshService widgetRefresh)
	{
		_habitService = habitService;
		_localization = localization;
		_widgetRefresh = widgetRefresh;

		foreach (var hex in HabitColorPalette.All)
		{
			ColorOptions.Add(new ColorSwatchItem(hex));
		}

		SelectedColorHex = HabitColorPalette.PickRandom();

		for (var day = 1; day <= 7; day++)
		{
			WeekDays.Add(new DayToggleItem(day, GetDayLabel(day), isSelected: true));
		}
	}

	public string Title => _localization.Get(IsEditing ? "EditHabit" : "AddHabit");
	public string HabitNameLabel => _localization.Get("HabitName");
	public string HabitNamePlaceholder => _localization.Get("HabitNamePlaceholder");
	public string ColorLabel => _localization.Get("ColorLabel");
	public string ColorPreviewLabel => _localization.Get("ColorPreview");
	public string ColorPreviewHint => _localization.Get("ColorPreviewHint");
	public string ShowInWidgetLabel => _localization.Get("ShowInWidget");
	public string ScheduleLabel => _localization.Get("ScheduleLabel");
	public string ScheduleSpecificDaysLabel => _localization.Get("ScheduleSpecificDays");
	public string ScheduleTimesPerWeekLabel => _localization.Get("ScheduleTimesPerWeek");
	public string TargetDaysLabel => _localization.Get("TargetDays");
	public string EveryDayLabel => _localization.Get("EveryDay");
	public string TimesPerWeekHint => _localization.Get("TimesPerWeekHint");
	public string TimesPerWeekDisplay => string.Format(_localization.Get("TimesPerWeekFormat"), TimesPerWeek);

	public double TimesPerWeekStepper
	{
		get => TimesPerWeek;
		set => TimesPerWeek = (int)Math.Clamp(Math.Round(value), 1, 6);
	}

	public string SaveLabel => _localization.Get("Save");

	public string PreviewName => string.IsNullOrWhiteSpace(Name) ? HabitNamePlaceholder : Name.Trim();
	public Color PreviewAccentColor => Color.FromArgb(SelectedColorHex);

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("habitId", out var value) && value is string habitId && !string.IsNullOrWhiteSpace(habitId))
		{
			_ = LoadForEditAsync(habitId);
			return;
		}

		_editingHabitId = null;
		OnPropertyChanged(nameof(IsEditing));
		OnPropertyChanged(nameof(Title));
	}

	partial void OnNameChanged(string value) => OnPropertyChanged(nameof(PreviewName));

	partial void OnTimesPerWeekChanged(int value)
	{
		OnPropertyChanged(nameof(TimesPerWeekDisplay));
		OnPropertyChanged(nameof(TimesPerWeekStepper));
	}

	partial void OnScheduleModeChanged(HabitScheduleMode value)
	{
		OnPropertyChanged(nameof(ShowSpecificDaySelection));
		OnPropertyChanged(nameof(ShowSpecificDaysSection));
		OnPropertyChanged(nameof(ShowTimesPerWeekSection));
		OnPropertyChanged(nameof(IsSpecificDaysMode));
		OnPropertyChanged(nameof(IsTimesPerWeekMode));
	}

	partial void OnIsEveryDayChanged(bool value)
	{
		OnPropertyChanged(nameof(ShowSpecificDaySelection));
		if (value)
		{
			foreach (var day in WeekDays)
			{
				day.IsSelected = true;
			}
		}
	}

	partial void OnSelectedColorHexChanged(string value)
	{
		SyncColorSelection();
		OnPropertyChanged(nameof(PreviewAccentColor));
	}

	[RelayCommand]
	private void SelectColor(ColorSwatchItem swatch)
	{
		SelectedColorHex = swatch.Hex;
	}

	[RelayCommand]
	private void SetScheduleSpecificDays()
	{
		ScheduleMode = HabitScheduleMode.SpecificDays;
	}

	[RelayCommand]
	private void SetScheduleTimesPerWeek()
	{
		ScheduleMode = HabitScheduleMode.TimesPerWeek;
	}

	[RelayCommand]
	private async Task SaveAsync()
	{
		ErrorMessage = null;

		if (string.IsNullOrWhiteSpace(Name))
		{
			ErrorMessage = _localization.Get("ErrorHabitNameRequired");
			return;
		}

		List<int> targetDays;
		var mode = ScheduleMode;
		var weeklyTarget = TimesPerWeek;

		if (mode == HabitScheduleMode.TimesPerWeek)
		{
			if (weeklyTarget is < 1 or > 6)
			{
				ErrorMessage = _localization.Get("ErrorTimesPerWeekRequired");
				return;
			}

			targetDays = [1, 2, 3, 4, 5, 6, 7];
		}
		else
		{
			targetDays = IsEveryDay
				? [1, 2, 3, 4, 5, 6, 7]
				: WeekDays.Where(d => d.IsSelected).Select(d => d.Day).ToList();

			if (targetDays.Count == 0)
			{
				ErrorMessage = _localization.Get("ErrorTargetDayRequired");
				return;
			}
		}

		try
		{
			Habit habit;
			if (IsEditing)
			{
				habit = await _habitService.GetHabitAsync(_editingHabitId!)
					?? throw new InvalidOperationException(_localization.Get("ErrorHabitNotFound"));
			}
			else
			{
				habit = new Habit();
			}

			habit.Name = Name.Trim();
			habit.ColorHex = HabitColorPalette.Normalize(SelectedColorHex);
			habit.ShowInWidget = ShowInWidget;
			habit.TargetDays = targetDays;
			habit.ScheduleMode = mode;
			habit.TimesPerWeek = mode == HabitScheduleMode.TimesPerWeek ? weeklyTarget : 1;

			await _habitService.SaveHabitAsync(habit);
			await _widgetRefresh.RefreshAsync();
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			ErrorMessage = UserFriendlyErrorMapper.FromException(ex, _localization);
		}
	}

	private async Task LoadForEditAsync(string habitId)
	{
		ErrorMessage = null;
		_editingHabitId = habitId;
		OnPropertyChanged(nameof(IsEditing));
		OnPropertyChanged(nameof(Title));

		var habit = await _habitService.GetHabitAsync(habitId);
		if (habit is null)
		{
			ErrorMessage = _localization.Get("ErrorHabitNotFound");
			await Shell.Current.GoToAsync("..");
			return;
		}

		Name = habit.Name;
		SelectedColorHex = HabitColorPalette.Normalize(habit.ColorHex);
		ShowInWidget = habit.ShowInWidget;
		ScheduleMode = habit.ScheduleMode;
		TimesPerWeek = habit.TimesPerWeek;

		if (habit.ScheduleMode == HabitScheduleMode.SpecificDays)
		{
			var selectedDays = habit.TargetDays.ToHashSet();
			IsEveryDay = AllDays.All(selectedDays.Contains);
			foreach (var day in WeekDays)
			{
				day.IsSelected = selectedDays.Contains(day.Day);
			}
		}
		else
		{
			IsEveryDay = true;
			foreach (var day in WeekDays)
			{
				day.IsSelected = true;
			}
		}

		SyncColorSelection();
		OnPropertyChanged(nameof(ShowSpecificDaySelection));
		OnPropertyChanged(nameof(ShowSpecificDaysSection));
		OnPropertyChanged(nameof(ShowTimesPerWeekSection));
		OnPropertyChanged(nameof(IsSpecificDaysMode));
		OnPropertyChanged(nameof(IsTimesPerWeekMode));
		OnPropertyChanged(nameof(TimesPerWeekDisplay));
		OnPropertyChanged(nameof(TimesPerWeekStepper));
		OnPropertyChanged(nameof(PreviewName));
		OnPropertyChanged(nameof(PreviewAccentColor));
	}

	private void SyncColorSelection()
	{
		foreach (var option in ColorOptions)
		{
			option.IsSelected = option.Hex.Equals(SelectedColorHex, StringComparison.OrdinalIgnoreCase);
		}
	}

	private string GetDayLabel(int day) => day switch
	{
		1 => _localization.Get("DayMon"),
		2 => _localization.Get("DayTue"),
		3 => _localization.Get("DayWed"),
		4 => _localization.Get("DayThu"),
		5 => _localization.Get("DayFri"),
		6 => _localization.Get("DaySat"),
		7 => _localization.Get("DaySun"),
		_ => day.ToString()
	};
}

public partial class ColorSwatchItem : ObservableObject
{
	public ColorSwatchItem(string hex) => Hex = hex;

	public string Hex { get; }

	public Color DisplayColor => Color.FromArgb(Hex);

	[ObservableProperty]
	private bool isSelected;
}

public partial class DayToggleItem(int day, string label, bool isSelected) : ObservableObject
{
	public int Day { get; } = day;
	public string Label { get; } = label;

	[ObservableProperty]
	private bool isSelected = isSelected;
}
