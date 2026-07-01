using OneTapHabits.Models;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Services;

public sealed class GuestDataSyncService : IGuestDataSyncService
{
	private readonly IFirebaseAuth _auth;
	private readonly IFirebaseFirestore _firestore;
	private readonly ILocalGuestStore _guestStore;
	private readonly IDiagnosticLogService _diagnosticLog;

	public GuestDataSyncService(
		IFirebaseAuth auth,
		IFirebaseFirestore firestore,
		ILocalGuestStore guestStore,
		IDiagnosticLogService diagnosticLog)
	{
		_auth = auth;
		_firestore = firestore;
		_guestStore = guestStore;
		_diagnosticLog = diagnosticLog;
	}

	public async Task<SignInConflictInfo> EvaluateSignInConflictAsync(CancellationToken cancellationToken = default)
	{
		var userId = _auth.CurrentUser?.Uid
			?? throw new InvalidOperationException("Must be signed in to evaluate sign-in conflict.");

		var guest = await _guestStore.LoadAsync(cancellationToken);
		var sampleIds = SignInGuestDataHelper.ParseSampleHabitIds(
			Preferences.Default.Get(SignInGuestDataHelper.SampleHabitIdsPreferenceKey, string.Empty));

		var cloudHabits = await FetchCloudHabitsAsync(userId, cancellationToken);
		var cloudLogs = await FetchCloudLogsAsync(userId, cancellationToken);

		var meaningfulLocal = SignInGuestDataHelper.HasMeaningfulGuestData(guest, sampleIds);
		var cloudHasData = SignInGuestDataHelper.HasCloudData(cloudHabits, cloudLogs);

		_diagnosticLog.LogInfo(
			"SignInSync",
			$"Evaluate user={MaskUserId(userId)} localHabits={SignInGuestDataHelper.CountLocalHabits(guest)} cloudHabits={cloudHabits.Count} cloudLogs={cloudLogs.Count} meaningfulLocal={meaningfulLocal} cloudHasData={cloudHasData}");

		return SignInConflictInfo.Evaluate(
			meaningfulLocal,
			cloudHasData,
			SignInGuestDataHelper.CountLocalHabits(guest),
			cloudHabits.Count);
	}

	public async Task ApplySignInResolutionAsync(
		SignInDataResolution resolution,
		CancellationToken cancellationToken = default)
	{
		var userId = _auth.CurrentUser?.Uid
			?? throw new InvalidOperationException("Must be signed in to apply sign-in resolution.");

		switch (resolution)
		{
			case SignInDataResolution.KeepCloud:
				_diagnosticLog.LogInfo("SignInSync", "KeepCloud: clearing guest store.");
				await _guestStore.ClearAsync(cancellationToken);
				break;

			case SignInDataResolution.UseThisDevice:
				var guest = await _guestStore.LoadAsync(cancellationToken);
				_diagnosticLog.LogInfo(
					"SignInSync",
					$"UseThisDevice: uploading guest habits={guest.Habits.Count(h => h.IsActive)} logs={guest.Logs.Count}.");
				await ReplaceCloudWithGuestAsync(userId, guest, cancellationToken);
				await _guestStore.ClearAsync(cancellationToken);
				_diagnosticLog.LogInfo("SignInSync", "UseThisDevice: upload complete, guest cleared.");
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
		}
	}

	public async Task DownloadCloudDataToGuestAsync(CancellationToken cancellationToken = default)
	{
		var userId = _auth.CurrentUser?.Uid
			?? throw new InvalidOperationException("Must be signed in to download cloud data.");

		var habits = await FetchCloudHabitsAsync(userId, cancellationToken);
		var logs = await FetchCloudLogsAsync(userId, cancellationToken);

		await _guestStore.SaveAsync(new GuestDataSnapshot
		{
			Habits = habits.ToList(),
			Logs = logs.ToList()
		}, cancellationToken);
	}

	private async Task ReplaceCloudWithGuestAsync(
		string userId,
		GuestDataSnapshot guest,
		CancellationToken cancellationToken)
	{
		await DeleteAllDocumentsAsync(HabitsCollection(userId), cancellationToken);
		await DeleteAllDocumentsAsync(LogsCollection(userId), cancellationToken);

		var habits = guest.Habits.Where(h => h.IsActive).ToList();
		var logs = guest.Logs.Where(l => l.IsCompleted || l.Count > 0).ToList();

		await UploadHabitsAsync(userId, habits, cancellationToken);
		await UploadLogsAsync(userId, logs, cancellationToken);
	}

	private static async Task DeleteAllDocumentsAsync(
		ICollectionReference collection,
		CancellationToken cancellationToken)
	{
		var snapshot = await collection.GetDocumentsAsync<DeleteMarkerDto>();
		foreach (var document in snapshot.Documents)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await document.Reference.DeleteDocumentAsync();
		}
	}

	private async Task<IReadOnlyList<Habit>> FetchCloudHabitsAsync(string userId, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var habitsSnapshot = await HabitsCollection(userId).GetDocumentsAsync<HabitDto>();
		return habitsSnapshot.Documents
			.Where(d => d.Data is not null && d.Data.IsActive)
			.Select(d => d.Data!.ToModel(d.Reference.Id))
			.ToList();
	}

	private async Task<IReadOnlyList<GuestLogEntry>> FetchCloudLogsAsync(string userId, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var logsSnapshot = await LogsCollection(userId).GetDocumentsAsync<LogDto>();
		return logsSnapshot.Documents
			.Where(d => d.Data is not null && (d.Data!.IsCompleted || d.Data.Count > 0))
			.Select(d =>
			{
				var habitId = HabitLogDocumentId.ResolveHabitId(d.Reference.Id, d.Data!.HabitId);
				var date = HabitLogDocumentId.TryParse(d.Reference.Id, out var fromId, out _)
					? fromId
					: DateOnly.TryParse(d.Data.Date, out var parsed)
						? parsed
						: DateOnly.FromDateTime(DateTime.Today);
				var count = d.Data.Count > 0 ? d.Data.Count : 1;
				return new GuestLogEntry
				{
					HabitId = habitId,
					Date = date.ToString("yyyy-MM-dd"),
					IsCompleted = true,
					Count = count
				};
			})
			.Where(e => !string.IsNullOrEmpty(e.HabitId))
			.ToList();
	}

	private async Task UploadHabitsAsync(
		string userId,
		IReadOnlyList<Habit> habits,
		CancellationToken cancellationToken)
	{
		foreach (var habit in habits)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await HabitsCollection(userId).GetDocument(habit.Id).SetDataAsync(HabitDto.FromModel(habit));
		}
	}

	private async Task UploadLogsAsync(
		string userId,
		IReadOnlyList<GuestLogEntry> logs,
		CancellationToken cancellationToken)
	{
		foreach (var log in logs)
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
	}

	private ICollectionReference HabitsCollection(string userId) =>
		_firestore.GetCollection($"users/{userId}/habits");

	private ICollectionReference LogsCollection(string userId) =>
		_firestore.GetCollection($"users/{userId}/logs");

	private static string MaskUserId(string userId) =>
		userId.Length <= 8 ? $"{userId}..." : $"{userId[..8]}...";

	private sealed class DeleteMarkerDto;

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
		public string CreatedAt { get; set; } = string.Empty;
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
			CreatedAt = DateTimeOffset.TryParse(CreatedAt, out var parsed) ? parsed : DateTimeOffset.UtcNow,
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
