namespace OneTapHabits.Services;

public interface IGuestDataSyncService
{
	Task<SignInConflictInfo> EvaluateSignInConflictAsync(CancellationToken cancellationToken = default);

	Task ApplySignInResolutionAsync(SignInDataResolution resolution, CancellationToken cancellationToken = default);

	Task DownloadCloudDataToGuestAsync(CancellationToken cancellationToken = default);
}
