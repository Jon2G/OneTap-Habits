namespace OneTapHabits.Services.Widget;

public sealed class NoOpWidgetRefreshService : IWidgetRefreshService
{
	public Task RefreshAsync() => Task.CompletedTask;

	public Task ClearAsync() => Task.CompletedTask;
}
