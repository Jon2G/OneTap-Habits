using Android.Appwidget;
using OneTapHabits.Models;
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

	public async Task RefreshAsync()
	{
		var context = global::Android.App.Application.Context;
		if (context is null)
		{
			return;
		}

		var today = DateOnly.FromDateTime(DateTime.Today);
		var habits = await _habitService.GetTodayHabitsAsync(today);
		var countMap = await _logService.GetCountMapForDateAsync(today);

		var incomplete = habits
			.Where(h => h.ShowInWidget)
			.Where(h =>
			{
				var count = countMap.TryGetValue(h.Id, out var value) ? value : 0;
				return !HabitDailyTargetHelper.IsDailyTargetMet(h, count);
			})
			.Select(h => new WidgetHabitEntry
			{
				Id = h.Id,
				Name = h.Name,
				ColorHex = h.ColorHex,
				Count = countMap.TryGetValue(h.Id, out var count) ? count : 0,
				TimesPerDay = HabitDailyTargetHelper.GetDailyTarget(h)
			})
			.ToList();

		var overflow = Math.Max(0, incomplete.Count - AppWidgets.WidgetConstants.MaxVisibleHabits);
		var visible = incomplete.Take(AppWidgets.WidgetConstants.MaxVisibleHabits).ToList();

		WidgetSnapshotStore.Save(context, new WidgetSnapshot
		{
			IsSignedIn = true,
			DateIso = today.ToString("O"),
			Habits = visible,
			OverflowCount = overflow
		});

		AppWidgets.HabitsAppWidgetProvider.UpdateAllWidgets(context);
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
}
