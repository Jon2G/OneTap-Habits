using OneTapHabits.Models;
using OneTapHabits.Storage;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Services;

public sealed class LocalCloudStoreMigrationService : ILocalCloudStoreMigrationService
{
	private const string MigrationPrefPrefix = "legacy_firestore_cache_migrated_v1_";

	private readonly IFirebaseAuth _auth;
	private readonly IFirebaseFirestore _firestore;
	private readonly ILocalCloudStore _cloudStore;
	private readonly IDiagnosticLogService _diagnosticLog;

	public LocalCloudStoreMigrationService(
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

	public async Task MigrateLegacyFirestoreCacheIfNeededAsync(CancellationToken cancellationToken = default)
	{
		var userId = _auth.CurrentUser?.Uid;
		if (string.IsNullOrEmpty(userId))
		{
			return;
		}

		var prefKey = MigrationPrefPrefix + userId;
		if (Preferences.Default.Get(prefKey, false))
		{
			return;
		}

		var cachePath = CloudCachePersistence.GetFilePath(FileSystem.AppDataDirectory);
		var cacheFile = CloudCachePersistence.LoadFromPath(cachePath);
		if (CloudCachePersistence.HasUserCacheData(cacheFile, userId))
		{
			Preferences.Default.Set(prefKey, true);
			return;
		}

		try
		{
			var habits = await CloudSnapshotFetcher.FetchHabitsAsync(_firestore, userId, cancellationToken);
			var logs = await CloudSnapshotFetcher.FetchLogsAsync(_firestore, userId, cancellationToken);
			if (habits.Count == 0 && logs.Count == 0)
			{
				_diagnosticLog.LogInfo(
					"Migration",
					$"Legacy Firestore cache migration skipped: no cached documents for user={MaskUserId(userId)}.");
				return;
			}

			await _cloudStore.SaveAsync(userId, new GuestDataSnapshot
			{
				Habits = habits.ToList(),
				Logs = logs.ToList()
			}, cancellationToken);

			Preferences.Default.Set(prefKey, true);
			_diagnosticLog.LogInfo(
				"Migration",
				$"Seeded cloud_cache from Firestore SDK cache user={MaskUserId(userId)} habits={habits.Count} logs={logs.Count}.");
		}
		catch (Exception ex)
		{
			_diagnosticLog.LogError("Migration", ex, "Legacy Firestore cache migration failed.");
		}
	}

	private static string MaskUserId(string userId) =>
		userId.Length <= 8 ? $"{userId}..." : $"{userId[..8]}...";
}
