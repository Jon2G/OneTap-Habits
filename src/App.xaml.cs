namespace OneTapHabits;

using CommunityToolkit.Mvvm.Messaging;
using OneTapHabits.Messages;
using OneTapHabits.Services;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new MainShell());
		window.Created += (_, _) =>
		{
			var diagnosticLog = IPlatformApplication.Current?.Services.GetService<IDiagnosticLogService>();
			diagnosticLog?.LogInfo("App", $"Started v{AppInfo.VersionString} ({AppInfo.BuildString}) on {DeviceInfo.Platform} {DeviceInfo.VersionString}");
			LocalCloudStoreMigrator.MigrateLegacyOverlayIfNeeded();
			_ = RunStartupMigrationsAsync();
		};
		return window;
	}

	protected override void OnResume()
	{
		base.OnResume();
		WeakReferenceMessenger.Default.Send(new AppResumedMessage());
		_ = RescheduleRemindersAsync();
		RequestBackgroundCloudSync();
	}

	private static async Task RunStartupMigrationsAsync()
	{
		try
		{
			var migration = IPlatformApplication.Current?.Services.GetService<ILocalCloudStoreMigrationService>();
			if (migration is not null)
			{
				await migration.MigrateLegacyFirestoreCacheIfNeededAsync();
			}
		}
		catch
		{
			// Migration is best-effort.
		}
	}

	private static void RequestBackgroundCloudSync()
	{
		var cloudSync = IPlatformApplication.Current?.Services.GetService<ICloudSyncService>();
		cloudSync?.RequestBackgroundSync();
	}

	private static async Task RescheduleRemindersAsync()
	{
		try
		{
			var reminderService = IPlatformApplication.Current?.Services.GetService<IHabitReminderService>();
			if (reminderService is not null)
			{
				await reminderService.RescheduleAllAsync();
			}
		}
		catch
		{
			// Non-fatal if reminders fail to reschedule on resume.
		}
	}
}
