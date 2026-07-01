using OneTapHabits.Models;
using Xunit;

namespace OneTapHabits.Tests;

public class HabitFirestoreMappingTests
{
	[Fact]
	public void Habit_round_trip_preserves_key_fields()
	{
		var original = new Habit
		{
			Id = "habit123",
			Name = "Read",
			ColorHex = "#FF5722",
			ShowInWidget = false,
			TargetDays = [1, 3, 5],
			ScheduleMode = HabitScheduleMode.TimesPerWeek,
			TimesPerWeek = 4,
			TimesPerDay = 2,
			SortOrder = 3,
			ReminderEnabled = true,
			ReminderTime = new TimeOnly(8, 30),
			CreatedAt = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero),
			IsActive = true
		};

		var fields = HabitFirestoreTestMapping.FromModel(original);
		var restored = HabitFirestoreTestMapping.ToModel(original.Id, fields);

		Assert.Equal(original.Id, restored.Id);
		Assert.Equal(original.Name, restored.Name);
		Assert.Equal(original.ColorHex, restored.ColorHex);
		Assert.Equal(original.ShowInWidget, restored.ShowInWidget);
		Assert.Equal(original.TargetDays, restored.TargetDays);
		Assert.Equal(original.ScheduleMode, restored.ScheduleMode);
		Assert.Equal(original.TimesPerWeek, restored.TimesPerWeek);
		Assert.Equal(original.TimesPerDay, restored.TimesPerDay);
		Assert.Equal(original.SortOrder, restored.SortOrder);
		Assert.Equal(original.ReminderEnabled, restored.ReminderEnabled);
		Assert.Equal(original.ReminderTime, restored.ReminderTime);
		Assert.Equal(original.CreatedAt, restored.CreatedAt);
		Assert.Equal(original.IsActive, restored.IsActive);
	}

	[Fact]
	public void Log_round_trip_preserves_count_and_completion()
	{
		var habitId = "habit123";
		var date = new DateOnly(2026, 7, 1);
		var fields = LogFirestoreTestMapping.FromEntry(habitId, date, 3);
		var log = LogFirestoreTestMapping.ToModel(HabitLog.CreateId(date, habitId), habitId, date, fields);

		Assert.Equal(habitId, log.HabitId);
		Assert.Equal(date, log.Date);
		Assert.Equal(3, log.Count);
		Assert.True(log.IsCompleted);
	}
}

internal static class HabitFirestoreTestMapping
{
	internal sealed class Fields
	{
		public string Name { get; init; } = string.Empty;
		public string ColorHex { get; init; } = "#4CAF50";
		public bool ShowInWidget { get; init; } = true;
		public List<int> TargetDays { get; init; } = [];
		public int ScheduleMode { get; init; }
		public int TimesPerWeek { get; init; } = 1;
		public int TimesPerDay { get; init; } = 1;
		public int SortOrder { get; init; }
		public bool ReminderEnabled { get; init; }
		public string? ReminderTime { get; init; }
		public string CreatedAt { get; init; } = string.Empty;
		public bool IsActive { get; init; } = true;
	}

	public static Fields FromModel(Habit habit) => new()
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

	public static Habit ToModel(string id, Fields fields) => new()
	{
		Id = id,
		Name = fields.Name,
		ColorHex = fields.ColorHex,
		ShowInWidget = fields.ShowInWidget,
		TargetDays = fields.TargetDays,
		ScheduleMode = Enum.IsDefined(typeof(HabitScheduleMode), fields.ScheduleMode)
			? (HabitScheduleMode)fields.ScheduleMode
			: HabitScheduleMode.SpecificDays,
		TimesPerWeek = fields.TimesPerWeek < 1 ? 1 : fields.TimesPerWeek,
		TimesPerDay = fields.TimesPerDay < 1 ? 1 : fields.TimesPerDay,
		SortOrder = fields.SortOrder,
		ReminderEnabled = fields.ReminderEnabled,
		ReminderTime = TimeOnly.TryParse(fields.ReminderTime, out var time) ? time : null,
		CreatedAt = DateTimeOffset.TryParse(fields.CreatedAt, out var parsed) ? parsed : DateTimeOffset.UtcNow,
		IsActive = fields.IsActive
	};
}

internal static class LogFirestoreTestMapping
{
	internal sealed class Fields
	{
		public string HabitId { get; init; } = string.Empty;
		public string Date { get; init; } = string.Empty;
		public bool IsCompleted { get; init; }
		public int Count { get; init; }
	}

	public static Fields FromEntry(string habitId, DateOnly date, int count) => new()
	{
		HabitId = habitId,
		Date = date.ToString("O"),
		IsCompleted = count > 0,
		Count = count
	};

	public static HabitLog ToModel(string id, string habitId, DateOnly date, Fields fields)
	{
		var count = fields.Count > 0 ? fields.Count : fields.IsCompleted ? 1 : 0;
		return new HabitLog
		{
			Id = id,
			HabitId = habitId,
			Date = date,
			IsCompleted = count > 0,
			Count = count
		};
	}
}
