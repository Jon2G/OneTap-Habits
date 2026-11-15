#if WINDOWS
using Windows.ApplicationModel;

namespace OneTapHabits.Platforms.Windows.Services;

public sealed class WindowsAppStartupService : OneTapHabits.Services.IAppStartupService
{
	public const string StartupTaskId = "OneTapHabitsStartup";

	public bool IsSupported => true;

	public async Task<bool> GetEnabledAsync()
	{
		var task = await StartupTask.GetAsync(StartupTaskId);
		return task.State == StartupTaskState.Enabled;
	}

	public async Task<bool> SetEnabledAsync(bool enabled)
	{
		var task = await StartupTask.GetAsync(StartupTaskId);

		if (enabled)
		{
			if (task.State == StartupTaskState.Enabled)
			{
				return true;
			}

			if (task.State is StartupTaskState.Disabled or StartupTaskState.DisabledByUser)
			{
				var result = await task.RequestEnableAsync();
				return result == StartupTaskState.Enabled;
			}

			// DisabledByPolicy or unknown — cannot enable.
			return false;
		}

		if (task.State == StartupTaskState.Enabled)
		{
			task.Disable();
		}

		return false;
	}
}
#endif
