using CommunityToolkit.Mvvm.Messaging;
using OneTapHabits.Messages;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Services;

public sealed class CloudSyncService : ICloudSyncService
{
	private readonly IAuthService _auth;
	private readonly IFirebaseAuth _firebaseAuth;
	private readonly IFirebaseFirestore _firestore;
	private readonly ILocalCloudStore _cloudStore;
	private readonly IDiagnosticLogService _diagnosticLog;
	private int _syncRunning;

	public CloudSyncService(
		IAuthService auth,
		IFirebaseAuth firebaseAuth,
		IFirebaseFirestore firestore,
		ILocalCloudStore cloudStore,
		IDiagnosticLogService diagnosticLog)
	{
		_auth = auth;
		_firebaseAuth = firebaseAuth;
		_firestore = firestore;
		_cloudStore = cloudStore;
		_diagnosticLog = diagnosticLog;
	}

	public async Task<bool> SyncFromCloudAsync(CancellationToken cancellationToken = default)
	{
		if (_auth.IsGuest)
		{
			return false;
		}

		var userId = _firebaseAuth.CurrentUser?.Uid;
		if (string.IsNullOrEmpty(userId))
		{
			return false;
		}

		if (Interlocked.CompareExchange(ref _syncRunning, 1, 0) != 0)
		{
			return false;
		}

		try
		{
			var habits = await CloudSnapshotFetcher.FetchHabitsAsync(_firestore, userId, cancellationToken);
			var logs = await CloudSnapshotFetcher.FetchLogsAsync(_firestore, userId, cancellationToken);
			var changed = await _cloudStore.MergeFromCloudAsync(userId, habits, logs, cancellationToken);
			if (changed)
			{
				WeakReferenceMessenger.Default.Send(new CloudCacheUpdatedMessage());
			}

			return changed;
		}
		catch (Exception ex)
		{
			_diagnosticLog.LogError("CloudSync", ex, "Background sync failed.");
			return false;
		}
		finally
		{
			Interlocked.Exchange(ref _syncRunning, 0);
		}
	}

	public void RequestBackgroundSync() =>
		_ = Task.Run(async () =>
		{
			try
			{
				await SyncFromCloudAsync();
			}
			catch
			{
				// Best-effort background sync.
			}
		});
}
