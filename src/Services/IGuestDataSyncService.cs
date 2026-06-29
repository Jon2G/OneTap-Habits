namespace OneTapHabits.Services;

public interface IGuestDataSyncService
{
	Task UploadGuestDataToCloudAsync(CancellationToken cancellationToken = default);

	Task DownloadCloudDataToGuestAsync(CancellationToken cancellationToken = default);
}
