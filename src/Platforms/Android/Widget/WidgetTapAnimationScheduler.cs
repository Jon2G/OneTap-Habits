using Android.Content;

namespace OneTapHabits.Platforms.Android.AppWidgets;

public static class WidgetTapAnimationScheduler
{
	private const int AnimationDurationMs = 450;

	public static void ScheduleFinish(Context context)
	{
		var appContext = context.ApplicationContext ?? context;
		_ = Task.Run(async () =>
		{
			await Task.Delay(AnimationDurationMs);
			WidgetTapAnimationStore.Clear(appContext);
			HabitsAppWidgetProvider.UpdateAllWidgets(appContext);
		});
	}
}
