using OneTapHabits.Models;
using OneTapHabits.Services.Firestore;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Services;

public sealed class CloudRestoreService : ICloudRestoreService
{
	private readonly IFirebaseAuth _auth;
	private readonly IFirebaseFirestore _firestore;
	private readonly ILocalCloudStore _cloudStore;
	private readonly IDiagnosticLogService _diagnosticLog;

	public CloudRestoreService(
		IFirebaseAuth auth,
		IFirebaseFirestore firestore,
		ILocalCloudStore cloudStore,
		IDiagnosticLogService diagnosticLog)
	{
		_auth = auth;
		_firestore = firestore;
		_cloudStore = cloudStore;
		_diagnosticLog = diagnosticLog;
	}

	public async Task<CloudRestoreResult> UploadLocalCacheToCloudAsync(CancellationToken cancellationToken = default)
	{
		var userId = _auth.CurrentUser?.Uid
			?? throw new InvalidOperationException("Must be signed in to restore cloud backup.");

		var local = await _cloudStore.LoadAsync(userId, cancellationToken);
		var habits = local.Habits.Where(h => h.IsActive).ToList();
		var logs = local.Logs.Where(l => l.IsCompleted || l.Count > 0).ToList();
		if (habits.Count == 0 && logs.Count == 0)
		{
			throw new InvalidOperationException("No local habits or logs to upload.");
		}

		_diagnosticLog.LogInfo(
			"CloudRestore",
			$"Uploading local cache user={MaskUserId(userId)} habits={habits.Count} logs={logs.Count}.");

		foreach (var habit in habits)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await HabitsCollection(userId)
				.GetDocument(habit.Id)
				.SetDataAsync(HabitFirestoreDto.FromModel(habit));
		}

		var logsUploaded = 0;
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
			logsUploaded++;
		}

		_diagnosticLog.LogInfo(
			"CloudRestore",
			$"Upload complete user={MaskUserId(userId)} habits={habits.Count} logs={logsUploaded}.");

		return new CloudRestoreResult
		{
			HabitsUploaded = habits.Count,
			LogsUploaded = logsUploaded
		};
	}

	private ICollectionReference HabitsCollection(string userId) =>
		_firestore.GetCollection($"users/{userId}/habits");

	private ICollectionReference LogsCollection(string userId) =>
		_firestore.GetCollection($"users/{userId}/logs");

	private static string MaskUserId(string userId) =>
		userId.Length <= 8 ? $"{userId}..." : $"{userId[..8]}...";
}
