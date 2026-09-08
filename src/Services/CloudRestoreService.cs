using OneTapHabits.Models;
using OneTapHabits.Services.Firebase;
using OneTapHabits.Services.Firestore;

namespace OneTapHabits.Services;

public sealed class CloudRestoreService : ICloudRestoreService
{
	private readonly IFirebaseAuthGateway _auth;
	private readonly IFirestoreGateway _firestore;
	private readonly ILocalCloudStore _cloudStore;
	private readonly IDiagnosticLogService _diagnosticLog;

	public CloudRestoreService(
		IFirebaseAuthGateway auth,
		IFirestoreGateway firestore,
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
			await _firestore.SetDocumentAsync(
				$"users/{userId}/habits/{habit.Id}",
				HabitFirestoreDto.FromModel(habit),
				cancellationToken);
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
			await _firestore.SetDocumentAsync(
				$"users/{userId}/logs/{logId}",
				LogFirestoreDto.FromEntry(log.HabitId, date, count),
				cancellationToken);
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

	private static string MaskUserId(string userId) =>
		userId.Length <= 8 ? $"{userId}..." : $"{userId[..8]}...";
}
