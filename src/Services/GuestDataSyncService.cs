using OneTapHabits.Models;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Services;

public sealed class GuestDataSyncService : IGuestDataSyncService
{
	private readonly IFirebaseAuth _auth;
	private readonly IFirebaseFirestore _firestore;
	private readonly ILocalGuestStore _guestStore;

	public GuestDataSyncService(
		IFirebaseAuth auth,
		IFirebaseFirestore firestore,
		ILocalGuestStore guestStore)
	{
		_auth = auth;
		_firestore = firestore;
		_guestStore = guestStore;
	}

	public async Task UploadGuestDataToCloudAsync(CancellationToken cancellationToken = default)
	{
		var userId = _auth.CurrentUser?.Uid
			?? throw new InvalidOperationException("Must be signed in to upload guest data.");

		var guest = await _guestStore.LoadAsync(cancellationToken);
		if (guest.Habits.Count == 0 && guest.Logs.Count == 0)
		{
			return;
		}

		foreach (var habit in guest.Habits.Where(h => h.IsActive))
		{
			cancellationToken.ThrowIfCancellationRequested();
			await HabitsCollection(userId).GetDocument(habit.Id).SetDataAsync(HabitDto.FromModel(habit));
		}

		foreach (var log in guest.Logs.Where(l => l.IsCompleted || l.Count > 0))
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (!DateOnly.TryParse(log.Date, out var date))
			{
				continue;
			}

			var logId = HabitLog.CreateId(date, log.HabitId);
			var count = log.Count > 0 ? log.Count : 1;
			await LogsCollection(userId).GetDocument(logId).SetDataAsync(new LogDto
			{
				HabitId = log.HabitId,
				Date = date.ToString("O"),
				IsCompleted = true,
				Count = count
			});
		}

		await _guestStore.ClearAsync(cancellationToken);
	}

	public async Task DownloadCloudDataToGuestAsync(CancellationToken cancellationToken = default)
	{
		var userId = _auth.CurrentUser?.Uid
			?? throw new InvalidOperationException("Must be signed in to download cloud data.");

		var habitsSnapshot = await HabitsCollection(userId).GetDocumentsAsync<HabitDto>();
		var logsSnapshot = await LogsCollection(userId).GetDocumentsAsync<LogDto>();

		var guest = new GuestDataSnapshot
		{
			Habits = habitsSnapshot.Documents
				.Where(d => d.Data is not null && d.Data.IsActive)
				.Select(d => d.Data!.ToModel(d.Reference.Id))
				.ToList(),
			Logs = logsSnapshot.Documents
				.Where(d => d.Data is not null && (d.Data!.IsCompleted || d.Data.Count > 0))
				.Select(d =>
				{
					var habitId = d.Data!.HabitId;
					var date = DateOnly.TryParse(d.Data.Date, out var parsed)
						? parsed
						: ParseLogDateFromId(d.Reference.Id, habitId);
					var count = d.Data.Count > 0 ? d.Data.Count : 1;
					return new GuestLogEntry
					{
						HabitId = habitId,
						Date = date.ToString("yyyy-MM-dd"),
						IsCompleted = true,
						Count = count
					};
				})
				.ToList()
		};

		await _guestStore.SaveAsync(guest, cancellationToken);
	}

	private static DateOnly ParseLogDateFromId(string logId, string habitId)
	{
		var suffix = $"_{habitId}";
		if (logId.EndsWith(suffix, StringComparison.Ordinal) &&
		    DateOnly.TryParse(logId.AsSpan(0, logId.Length - suffix.Length), out var date))
		{
			return date;
		}

		return DateOnly.FromDateTime(DateTime.Today);
	}

	private ICollectionReference HabitsCollection(string userId) =>
		_firestore.GetCollection($"users/{userId}/habits");

	private ICollectionReference LogsCollection(string userId) =>
		_firestore.GetCollection($"users/{userId}/logs");

	private sealed class HabitDto
	{
		public string Name { get; set; } = string.Empty;
		public string ColorHex { get; set; } = "#4CAF50";
		public bool ShowInWidget { get; set; } = true;
		public List<int> TargetDays { get; set; } = [];
		public int ScheduleMode { get; set; }
		public int TimesPerWeek { get; set; } = 1;
		public int TimesPerDay { get; set; } = 1;
		public int SortOrder { get; set; }
		public bool ReminderEnabled { get; set; }
		public string? ReminderTime { get; set; }
		public DateTimeOffset CreatedAt { get; set; }
		public bool IsActive { get; set; } = true;

		public static HabitDto FromModel(Habit habit) => new()
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
			CreatedAt = habit.CreatedAt,
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
			CreatedAt = CreatedAt,
			IsActive = IsActive
		};
	}

	private sealed class LogDto
	{
		public string HabitId { get; set; } = string.Empty;
		public string Date { get; set; } = string.Empty;
		public bool IsCompleted { get; set; }
		public int Count { get; set; } = 1;
	}
}
