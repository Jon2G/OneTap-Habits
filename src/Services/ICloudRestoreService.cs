namespace OneTapHabits.Services;

public sealed class CloudRestoreResult
{
	public int HabitsUploaded { get; init; }

	public int LogsUploaded { get; init; }
}

public interface ICloudRestoreService
{
	Task<CloudRestoreResult> UploadLocalCacheToCloudAsync(CancellationToken cancellationToken = default);
}
