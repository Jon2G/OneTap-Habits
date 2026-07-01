using OneTapHabits.Firestore;
using OneTapHabits.Models;
using OneTapHabits.Services.Firestore;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Services;

public sealed class GuestDataSyncService : IGuestDataSyncService
{
	private readonly IFirebaseAuth _auth;
	private readonly IFirebaseFirestore _firestore;
	private readonly ILocalGuestStore _guestStore;
	private readonly ILocalCloudStore _cloudStore;
	private readonly IDiagnosticLogService _diagnosticLog;
	private IReadOnlyList<Habit> _lastCloudHabits = [];
	private IReadOnlyList<GuestLogEntry> _lastCloudLogs = [];

	public GuestDataSyncService(
		IFirebaseAuth auth,
		IFirebaseFirestore firestore,
		ILocalGuestStore guestStore,
		ILocalCloudStore cloudStore,
		IDiagnosticLogService diagnosticLog)
	{
		_auth = auth;
		_firestore = firestore;
		_guestStore = guestStore;
		_cloudStore = cloudStore;
		_diagnosticLog = diagnosticLog;
	}

	public async Task<SignInConflictInfo> EvaluateSignInConflictAsync(CancellationToken cancellationToken = default)
	{
		var userId = _auth.CurrentUser?.Uid
			?? throw new InvalidOperationException("Must be signed in to evaluate sign-in conflict.");

		var (habitsDeleted, logsDeleted) = await FirestoreCloudRepair.SanitizeCorruptDocumentsAsync(
			HabitsCollection(userId),
			LogsCollection(userId),
			cancellationToken);
		if (habitsDeleted > 0 || logsDeleted > 0)
		{
			_diagnosticLog.LogInfo(
				"SignInSync",
				$"Sanitized corrupt cloud docs user={MaskUserId(userId)} habitsDeleted={habitsDeleted} logsDeleted={logsDeleted}");
		}

		var guest = await _guestStore.LoadAsync(cancellationToken);
		var sampleIds = SignInGuestDataHelper.ParseSampleHabitIds(
			Preferences.Default.Get(SignInGuestDataHelper.SampleHabitIdsPreferenceKey, string.Empty));

		var cloudHabits = await CloudSnapshotFetcher.FetchHabitsAsync(_firestore, userId, cancellationToken);
		var cloudLogs = await CloudSnapshotFetcher.FetchLogsAsync(_firestore, userId, cancellationToken);
		_lastCloudHabits = cloudHabits;
		_lastCloudLogs = cloudLogs;

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
				_diagnosticLog.LogInfo("SignInSync", "KeepCloud: seeding cloud cache and clearing guest store.");
				await _cloudStore.SaveAsync(userId, new GuestDataSnapshot
				{
					Habits = _lastCloudHabits.ToList(),
					Logs = _lastCloudLogs.ToList()
				}, cancellationToken);
				await _guestStore.ClearAsync(cancellationToken);
				break;

			case SignInDataResolution.UseThisDevice:
				var guest = await _guestStore.LoadAsync(cancellationToken);
				_diagnosticLog.LogInfo(
					"SignInSync",
					$"UseThisDevice: uploading guest habits={guest.Habits.Count(h => h.IsActive)} logs={guest.Logs.Count}.");
				try
				{
					await _cloudStore.SaveAsync(userId, new GuestDataSnapshot
					{
						Habits = guest.Habits.ToList(),
						Logs = guest.Logs.ToList()
					}, cancellationToken);
					await ReplaceCloudWithGuestAsync(userId, guest, cancellationToken);
					await _guestStore.ClearAsync(cancellationToken);
					_diagnosticLog.LogInfo("SignInSync", "UseThisDevice: upload complete, guest cleared.");
				}
				catch (Exception ex)
				{
					_diagnosticLog.LogError("SignInSync", ex, "UseThisDevice upload failed; guest data preserved.");
					throw;
				}

				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
		}
	}

	public async Task DownloadCloudDataToGuestAsync(CancellationToken cancellationToken = default)
	{
		var userId = _auth.CurrentUser?.Uid
			?? throw new InvalidOperationException("Must be signed in to download cloud data.");

		var local = await _cloudStore.LoadAsync(userId, cancellationToken);
		if (local.Habits.Count == 0 && local.Logs.Count == 0)
		{
			var habits = await CloudSnapshotFetcher.FetchHabitsAsync(_firestore, userId, cancellationToken);
			var logs = await CloudSnapshotFetcher.FetchLogsAsync(_firestore, userId, cancellationToken);
			local = new GuestDataSnapshot
			{
				Habits = habits.ToList(),
				Logs = logs.ToList()
			};
		}

		await _guestStore.SaveAsync(local, cancellationToken);
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
		var snapshot = await collection.GetDocumentsAsync<Dictionary<string, object>>();
		foreach (var document in snapshot.Documents)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await document.Reference.DeleteDocumentAsync();
		}
	}

	private async Task UploadHabitsAsync(
		string userId,
		IReadOnlyList<Habit> habits,
		CancellationToken cancellationToken)
	{
		foreach (var habit in habits)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await HabitsCollection(userId)
				.GetDocument(habit.Id)
				.SetDataAsync(HabitFirestoreDto.FromModel(habit));
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
			await LogsCollection(userId)
				.GetDocument(logId)
				.SetDataAsync(LogFirestoreDto.FromEntry(log.HabitId, date, count));
		}
	}

	private ICollectionReference HabitsCollection(string userId) =>
		_firestore.GetCollection($"users/{userId}/habits");

	private ICollectionReference LogsCollection(string userId) =>
		_firestore.GetCollection($"users/{userId}/logs");

	private static string MaskUserId(string userId) =>
		userId.Length <= 8 ? $"{userId}..." : $"{userId[..8]}...";
}
