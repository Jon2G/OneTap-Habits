namespace OneTapHabits.Models;

public sealed class Habit
{
	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string ColorHex { get; set; } = HabitColorPalette.Default;
	public bool ShowInWidget { get; set; } = true;
	public IReadOnlyList<int> TargetDays { get; set; } = [1, 2, 3, 4, 5, 6, 7];
	public HabitScheduleMode ScheduleMode { get; set; } = HabitScheduleMode.SpecificDays;
	public int TimesPerWeek { get; set; } = 1;
	public int TimesPerDay { get; set; } = 1;
	public int SortOrder { get; set; }
	public bool ReminderEnabled { get; set; }
	public TimeOnly? ReminderTime { get; set; }
	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
	public bool IsActive { get; set; } = true;
}
