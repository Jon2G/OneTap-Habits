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
		return new Window(new MainShell());
	}

	protected override void OnResume()
	{
		base.OnResume();
		WeakReferenceMessenger.Default.Send(new AppResumedMessage());
		_ = RescheduleRemindersAsync();
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
