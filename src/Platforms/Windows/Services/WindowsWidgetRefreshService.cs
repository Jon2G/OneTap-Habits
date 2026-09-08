#if WINDOWS
using OneTapHabits.Models;
using OneTapHabits.Services;
using OneTapHabits.Services.Widget;

namespace OneTapHabits.Platforms.Windows.Services;

public sealed class WindowsWidgetRefreshService : IWidgetRefreshService
{
	private readonly IHabitService _habitService;
	private readonly ILogService _logService;

	public WindowsWidgetRefreshService(IHabitService habitService, ILogService logService)
	{
		_habitService = habitService;
		_logService = logService;
	}

	public Task RefreshAsync() => RefreshFromServicesAsync();

	public async Task RefreshAsync(IReadOnlyList<Habit> todayHabits, IReadOnlyDictionary<string, int> countMap)
	{
		ApplySnapshot(todayHabits, countMap);
		await Task.CompletedTask;
	}

	public Task ClearAsync()
	{
		WidgetSnapshotFileStore.Clear();
		return Task.CompletedTask;
	}

	private async Task RefreshFromServicesAsync()
	{
		var today = DateOnly.FromDateTime(DateTime.Today);
		var habits = await _habitService.GetTodayHabitsAsync(today);
		var countMap = await _logService.GetCountMapForDateAsync(today);
		ApplySnapshot(habits, countMap);
	}

	private static void ApplySnapshot(
		IReadOnlyList<Habit> habits,
		IReadOnlyDictionary<string, int> countMap)
	{
		WidgetTapAnimationFileStore.Clear();
		var today = DateOnly.FromDateTime(DateTime.Today);
		WidgetSnapshotFileStore.Save(WidgetSnapshotBuilder.Build(habits, countMap, today));
	}
}
#endif
