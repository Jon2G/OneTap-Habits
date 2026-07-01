using OneTapHabits.Firestore;
using OneTapHabits.Models;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Services.Firestore;

public sealed class HabitFirestoreDto : IFirestoreObject
{
	public HabitFirestoreDto()
	{
	}

	[FirestoreProperty("name")]
	public string Name { get; set; } = string.Empty;

	[FirestoreProperty("color_hex")]
	public string ColorHex { get; set; } = "#4CAF50";

	[FirestoreProperty("show_in_widget")]
	public bool ShowInWidget { get; set; } = true;

	[FirestoreProperty("target_days")]
	public List<int> TargetDays { get; set; } = [];

	[FirestoreProperty("schedule_mode")]
	public int ScheduleMode { get; set; }

	[FirestoreProperty("times_per_week")]
	public int TimesPerWeek { get; set; } = 1;

	[FirestoreProperty("times_per_day")]
	public int TimesPerDay { get; set; } = 1;

	[FirestoreProperty("sort_order")]
	public int SortOrder { get; set; }

	[FirestoreProperty("reminder_enabled")]
	public bool ReminderEnabled { get; set; }

	[FirestoreProperty("reminder_time")]
	public string? ReminderTime { get; set; }

	[FirestoreProperty("created_at")]
	public string CreatedAt { get; set; } = string.Empty;

	[FirestoreProperty("is_active")]
	public bool IsActive { get; set; } = true;

	public static HabitFirestoreDto FromModel(Habit habit) => new()
	{
		Name = habit.Name,
		ColorHex = habit.ColorHex,
		ShowInWidget = habit.ShowInWidget,
		TargetDays = habit.TargetDays.ToList(),
		ScheduleMode = (int)habit.ScheduleMode,
		TimesPerWeek = habit.TimesPerWeek,
		TimesPerDay = habit.TimesPerDay,
		SortOrder = habit.SortOrder,
		ReminderEnabled = habit.ReminderEnabled,
		ReminderTime = habit.ReminderTime?.ToString("HH:mm"),
		CreatedAt = habit.CreatedAt.ToString("O"),
		IsActive = habit.IsActive
	};

	public Habit ToModel(string id) => new()
	{
		Id = id,
		Name = Name,
		ColorHex = ColorHex,
		ShowInWidget = ShowInWidget,
		TargetDays = TargetDays,
		ScheduleMode = Enum.IsDefined(typeof(HabitScheduleMode), ScheduleMode)
			? (HabitScheduleMode)ScheduleMode
			: HabitScheduleMode.SpecificDays,
		TimesPerWeek = TimesPerWeek < 1 ? 1 : TimesPerWeek,
		TimesPerDay = TimesPerDay < 1 ? 1 : TimesPerDay,
		SortOrder = SortOrder,
		ReminderEnabled = ReminderEnabled,
		ReminderTime = TimeOnly.TryParse(ReminderTime, out var time) ? time : null,
		CreatedAt = ParseCreatedAt(CreatedAt),
		IsActive = IsActive
	};

	public bool IsValidCloudDocument() => CloudDocumentSanitizer.IsValidHabitName(Name);

	private static DateTimeOffset ParseCreatedAt(string? value) =>
		DateTimeOffset.TryParse(value, out var parsed) ? parsed : DateTimeOffset.UtcNow;
}
