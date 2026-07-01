namespace OneTapHabits.Services;

public interface ILocalCloudStoreMigrationService
{
	Task MigrateLegacyFirestoreCacheIfNeededAsync(CancellationToken cancellationToken = default);
}
