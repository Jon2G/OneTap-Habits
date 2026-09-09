using OneTapHabits.Widget;

namespace OneTapHabits.WidgetHost;

internal static class WidgetAnimationScheduler
{
	public static void ScheduleFinish(Action onFinish)
	{
		_ = Task.Run(async () =>
		{
			await Task.Delay(WidgetTapAnimationTiming.DurationMs);
			onFinish();
		});
	}
}
