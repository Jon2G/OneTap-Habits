using Android.Content;

namespace OneTapHabits.Platforms.Android.AppWidgets;

[BroadcastReceiver(
	Name = WidgetConstants.PackageName + ".AppWidgets.WidgetTapReceiver",
	Exported = false)]
[global::Android.App.IntentFilter(new[] { WidgetConstants.ActionCompleteHabit })]
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public class WidgetTapReceiver : BroadcastReceiver
{
	public override void OnReceive(Context? context, Intent? intent)
	{
		if (context is null || intent?.Action != WidgetConstants.ActionCompleteHabit)
		{
			return;
		}

		var habitId = intent.GetStringExtra(WidgetConstants.ExtraHabitId);
		if (string.IsNullOrEmpty(habitId))
		{
			return;
		}

		var isDecrement = intent.Data?.Path?.EndsWith("/decrement", StringComparison.Ordinal) == true;

		try
		{
			if (isDecrement)
			{
				Services.WidgetCompletionService.DecrementHabitAsync(context, habitId);
			}
			else
			{
				Services.WidgetCompletionService.IncrementHabitAsync(context, habitId);
			}
		}
		catch
		{
			HabitsAppWidgetProvider.UpdateAllWidgets(context);
		}
	}
}
