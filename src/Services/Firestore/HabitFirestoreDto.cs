using OneTapHabits.Firestore;
using OneTapHabits.Models;
#if ANDROID || IOS
using Plugin.Firebase.Firestore;
#endif

namespace OneTapHabits.Services.Firestore;

public sealed class HabitFirestoreDto
#if ANDROID || IOS
	: IFirestoreObject
#endif
{
	public HabitFirestoreDto()
	{
	}

#if ANDROID || IOS
	[FirestoreProperty("name")]
#endif
	public string Name { get; set; } = string.Empty;

#if ANDROID || IOS
	[FirestoreProperty("color_hex")]
#endif
	public string ColorHex { get; set; } = "#4CAF50";

#if ANDROID || IOS
	[FirestoreProperty("show_in_widget")]
#endif
	public bool ShowInWidget { get; set; } = true;

#if ANDROID || IOS
	[FirestoreProperty("target_days")]
#endif
	public List<int> TargetDays { get; set; } = [];

#if ANDROID || IOS
	[FirestoreProperty("schedule_mode")]
#endif
	public int ScheduleMode { get; set; }

#if ANDROID || IOS
	[FirestoreProperty("times_per_week")]
#endif
	public int TimesPerWeek { get; set; } = 1;

#if ANDROID || IOS
	[FirestoreProperty("times_per_day")]
#endif
	public int TimesPerDay { get; set; } = 1;

#if ANDROID || IOS
	[FirestoreProperty("sort_order")]
#endif
	public int SortOrder { get; set; }

#if ANDROID || IOS
	[FirestoreProperty("reminder_enabled")]
#endif
	public bool ReminderEnabled { get; set; }

#if ANDROID || IOS
	[FirestoreProperty("reminder_time")]
#endif
	public string? ReminderTime { get; set; }

#if ANDROID || IOS
	[FirestoreProperty("created_at")]
#endif
	public string CreatedAt { get; set; } = string.Empty;

#if ANDROID || IOS
	[FirestoreProperty("is_active")]
#endif
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
