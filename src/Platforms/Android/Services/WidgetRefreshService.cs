using OneTapHabits.Models;
using OneTapHabits.Platforms.Android.AppWidgets;
using OneTapHabits.Services;
using OneTapHabits.Services.Widget;

namespace OneTapHabits.Platforms.Android.Services;

public sealed class WidgetRefreshService : IWidgetRefreshService
{
	private readonly IHabitService _habitService;
	private readonly ILogService _logService;

	public WidgetRefreshService(IHabitService habitService, ILogService logService)
	{
		_habitService = habitService;
		_logService = logService;
	}

	public Task RefreshAsync() =>
		RefreshFromServicesAsync();

	public Task RefreshAsync(IReadOnlyList<Habit> todayHabits, IReadOnlyDictionary<string, int> countMap)
	{
		var context = global::Android.App.Application.Context;
		if (context is null)
		{
			return Task.CompletedTask;
		}

		ApplySnapshot(context, todayHabits, countMap);
		return Task.CompletedTask;
	}

	public Task ClearAsync()
	{
		var context = global::Android.App.Application.Context;
		if (context is null)
		{
			return Task.CompletedTask;
		}

		WidgetSnapshotStore.Clear(context);
		AppWidgets.HabitsAppWidgetProvider.UpdateAllWidgets(context);
		return Task.CompletedTask;
	}

	private async Task RefreshFromServicesAsync()
	{
		var context = global::Android.App.Application.Context;
		if (context is null)
		{
			return;
		}

		var today = DateOnly.FromDateTime(DateTime.Today);
		var habits = await _habitService.GetTodayHabitsAsync(today);
		var countMap = await _logService.GetCountMapForDateAsync(today);
		ApplySnapshot(context, habits, countMap);
	}

	private static void ApplySnapshot(
		global::Android.Content.Context context,
		IReadOnlyList<Habit> habits,
		IReadOnlyDictionary<string, int> countMap)
	{
		WidgetTapAnimationStore.Clear(context);
		var today = DateOnly.FromDateTime(DateTime.Today);
		WidgetSnapshotStore.Save(context, WidgetSnapshotBuilder.Build(habits, countMap, today));
		AppWidgets.HabitsAppWidgetProvider.UpdateAllWidgets(context);
	}
}
