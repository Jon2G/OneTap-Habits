namespace OneTapHabits.Services;

public sealed class NoOpAppStartupService : IAppStartupService
{
	public bool IsSupported => false;

	public Task<bool> GetEnabledAsync() => Task.FromResult(false);

	public Task<bool> SetEnabledAsync(bool enabled) => Task.FromResult(false);
}
