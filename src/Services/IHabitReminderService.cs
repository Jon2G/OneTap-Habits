namespace OneTapHabits.Services;

public interface IHabitReminderService
{
	Task ScheduleAsync(Models.Habit habit);
	Task CancelAsync(string habitId);
	Task RescheduleAllAsync();
	Task RequestPermissionIfNeededAsync();
}
