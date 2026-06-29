namespace OneTapHabits.Services;

public sealed class NoOpHabitReminderService : IHabitReminderService
{
	public Task ScheduleAsync(Models.Habit habit) => Task.CompletedTask;
	public Task CancelAsync(string habitId) => Task.CompletedTask;
	public Task RescheduleAllAsync() => Task.CompletedTask;
	public Task RequestPermissionIfNeededAsync() => Task.CompletedTask;
}
