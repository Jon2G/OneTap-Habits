using OneTapHabits.Models;
using OneTapHabits.Services;
using Plugin.LocalNotification;

namespace OneTapHabits.Platforms.Android.Services;

public sealed class AndroidHabitReminderService : IHabitReminderService
{
	private readonly IHabitService _habitService;
	private readonly ILocalizationService _localization;

	public AndroidHabitReminderService(IHabitService habitService, ILocalizationService localization)
	{
		_habitService = habitService;
		_localization = localization;
	}

	public async Task RequestPermissionIfNeededAsync()
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
			if (status != PermissionStatus.Granted)
			{
				await Permissions.RequestAsync<Permissions.PostNotifications>();
			}
		}
	}

	public async Task ScheduleAsync(Habit habit)
	{
		var notificationId = HabitReminderScheduleHelper.GetNotificationId(habit.Id);
		LocalNotificationCenter.Current.Cancel(notificationId);

		if (!habit.ReminderEnabled || habit.ReminderTime is null || !habit.IsActive)
		{
			return;
		}

		var next = HabitReminderScheduleHelper.GetNextReminderLocalDateTime(habit, DateTime.Now);
		if (next is null)
		{
			return;
		}

		var request = new NotificationRequest
		{
			NotificationId = notificationId,
			Title = habit.Name,
			Description = _localization.Get("ReminderBody"),
			Schedule = new NotificationRequestSchedule
			{
				NotifyTime = next.Value,
				RepeatType = NotificationRepeat.No
			}
		};

		await LocalNotificationCenter.Current.Show(request);
	}

	public Task CancelAsync(string habitId)
	{
		LocalNotificationCenter.Current.Cancel(HabitReminderScheduleHelper.GetNotificationId(habitId));
		return Task.CompletedTask;
	}

	public async Task RescheduleAllAsync()
	{
		var habits = await _habitService.GetActiveHabitsAsync();
		foreach (var habit in habits)
		{
			await ScheduleAsync(habit);
		}
	}
}
