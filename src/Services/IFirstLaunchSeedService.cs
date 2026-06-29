namespace OneTapHabits.Services;

public interface IFirstLaunchSeedService
{
	Task SeedIfNeededAsync();
}
