namespace OneTapHabits.Services;

public interface ICloudSyncService
{
	Task<bool> SyncFromCloudAsync(CancellationToken cancellationToken = default);

	void RequestBackgroundSync();
}
