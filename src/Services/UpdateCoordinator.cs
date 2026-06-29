using OneTapHabits.Views;

namespace OneTapHabits.Services;

[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public class UpdateCoordinator
{
	public const string LastRemoteVersionSeenKey = "LastRemoteVersionSeen";
	public const string UpdateJustInstalledKey = "UpdateJustInstalled";

	private readonly UpdateService _updateService;
	private readonly ILocalizationService _localization;
	private bool _isChecking;

	public UpdateCoordinator(UpdateService updateService, ILocalizationService localization)
	{
		_updateService = updateService;
		_localization = localization;
	}

	public async Task CheckForUpdatesAsync(INavigation navigation, bool manual)
	{
#if !ANDROID
		_ = manual;
		_ = navigation;
		return;
#else
		if (_isChecking)
		{
			return;
		}

		_isChecking = true;
		try
		{
			if (Preferences.Default.Get(UpdateJustInstalledKey, false))
			{
				Preferences.Default.Remove(UpdateJustInstalledKey);
				var currentVersion = NormalizeVersion(AppInfo.Current.VersionString);
				Preferences.Default.Set(LastRemoteVersionSeenKey, currentVersion);
			}

			if (navigation.ModalStack.Any(page => page is UpdatePopupPage))
			{
				return;
			}

			var updateCheck = await _updateService.CheckForUpdatesAsync();
			if (updateCheck == null)
			{
				if (manual)
				{
					await ShowUpToDateAlertAsync();
				}

				return;
			}

			if (!updateCheck.HasUpdate)
			{
				if (manual)
				{
					await ShowUpToDateAlertAsync();
				}

				return;
			}

			var remoteVersion = NormalizeVersion(updateCheck.LatestVersion);
			var localVersion = NormalizeVersion(AppInfo.Current.VersionString);

			if (!manual)
			{
				var lastRemoteSeen = NormalizeVersion(Preferences.Default.Get(LastRemoteVersionSeenKey, string.Empty));
				if (remoteVersion == lastRemoteSeen || remoteVersion == localVersion)
				{
					return;
				}
			}

			if (navigation.ModalStack.Any(page => page is UpdatePopupPage))
			{
				return;
			}

			await MainThread.InvokeOnMainThreadAsync(async () =>
			{
				await navigation.PushModalAsync(new UpdatePopupPage(
					updateCheck.LatestVersion,
					updateCheck.Changelog,
					updateCheck.ApkDownloadUrl,
					_updateService,
					_localization));
			});
		}
		catch
		{
			// Fail silently on auto-check; manual check can show up-to-date only when API succeeds.
		}
		finally
		{
			_isChecking = false;
		}
#endif
	}

	private async Task ShowUpToDateAlertAsync()
	{
		await MainThread.InvokeOnMainThreadAsync(async () =>
		{
			var page = Shell.Current?.CurrentPage;
			if (page is null)
			{
				return;
			}

			await page.DisplayAlert(_localization.Get("AppTitle"), _localization.Get("UpdateUpToDate"), "OK");
		});
	}

	private static string NormalizeVersion(string raw) =>
		raw.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? raw[1..] : raw;
}
