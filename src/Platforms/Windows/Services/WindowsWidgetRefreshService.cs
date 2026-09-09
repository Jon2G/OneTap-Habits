#if WINDOWS
using OneTapHabits.Models;
using OneTapHabits.Services;
using OneTapHabits.Services.Widget;

namespace OneTapHabits.Platforms.Windows.Services;

public sealed class WindowsWidgetRefreshService : IWidgetRefreshService
{
	private readonly IHabitService _habitService;
	private readonly ILogService _logService;
	private readonly IAuthService _authService;

	public WindowsWidgetRefreshService(
		IHabitService habitService,
		ILogService logService,
		IAuthService authService)
	{
		_habitService = habitService;
		_logService = logService;
		_authService = authService;
	}

	public Task RefreshAsync() => RefreshFromServicesAsync();

	public async Task RefreshAsync(IReadOnlyList<Habit> todayHabits, IReadOnlyDictionary<string, int> countMap)
	{
		ApplySnapshot(todayHabits, countMap);
		await Task.CompletedTask;
	}

	public Task ClearAsync()
	{
		WidgetTapAnimationFileStore.Clear();
		WidgetSnapshotFileStore.Clear();
		WidgetSnapshotFileStore.SignalRefresh();
		return Task.CompletedTask;
	}

	private async Task RefreshFromServicesAsync()
	{
		var today = DateOnly.FromDateTime(DateTime.Today);
		var habits = await _habitService.GetTodayHabitsAsync(today);
		var countMap = await _logService.GetCountMapForDateAsync(today);
		ApplySnapshot(habits, countMap);
	}

	private void ApplySnapshot(
		IReadOnlyList<Habit> habits,
		IReadOnlyDictionary<string, int> countMap)
	{
		WidgetTapAnimationFileStore.Clear();
		var today = DateOnly.FromDateTime(DateTime.Today);
		var userId = _authService.IsSignedIn ? _authService.UserId : null;
		WidgetSnapshotFileStore.Save(WidgetSnapshotBuilder.Build(habits, countMap, today, userId: userId));
		WidgetSnapshotFileStore.SignalRefresh();
	}
}
#endif
