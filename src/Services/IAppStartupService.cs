namespace OneTapHabits.Services;

public interface IAppStartupService
{
	bool IsSupported { get; }

	Task<bool> GetEnabledAsync();

	/// <summary>
	/// Enables or disables start-at-login. Returns the resulting enabled state
	/// (may remain false if the user denies consent or policy blocks it).
	/// </summary>
	Task<bool> SetEnabledAsync(bool enabled);
}
