using Android.Appwidget;
using Android.Content;
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
		var completionMap = await _logService.GetCompletionMapForDateAsync(today);

		var incomplete = habits
			.Where(h => h.ShowInWidget)
			.Where(h => !completionMap.TryGetValue(h.Id, out var done) || !done)
			.OrderBy(h => h.Name)
			.Select(h => new WidgetHabitEntry
			{
				Id = h.Id,
				Name = h.Name,
				ColorHex = h.ColorHex
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
		if (context is not null)
		{
			WidgetSnapshotStore.Clear(context);
			AppWidgets.HabitsAppWidgetProvider.UpdateAllWidgets(context);
		}

		return Task.CompletedTask;
	}
}
