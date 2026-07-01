namespace OneTapHabits.Services.Widget;

public sealed class NoOpWidgetRefreshService : IWidgetRefreshService
{
	public Task RefreshAsync() => Task.CompletedTask;

	public Task RefreshAsync(IReadOnlyList<Models.Habit> todayHabits, IReadOnlyDictionary<string, int> countMap) =>
		Task.CompletedTask;

	public Task ClearAsync() => Task.CompletedTask;
}
