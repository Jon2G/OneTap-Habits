using Android.App;
using Android.Content;

namespace OneTapHabits.Platforms.Android.AppWidgets;

[BroadcastReceiver(
	Name = WidgetConstants.PackageName + ".AppWidgets.WidgetTapReceiver",
	Exported = false)]
[IntentFilter(new[] { WidgetConstants.ActionCompleteHabit })]
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

		Task.Run(async () =>
		{
			try
			{
				var result = await Services.WidgetCompletionService.IncrementHabitAsync(context, habitId);
				if (result.ShouldRemoveFromWidget)
				{
					Services.WidgetSnapshotStore.RemoveHabit(context, habitId);
				}
				else
				{
					Services.WidgetSnapshotStore.UpdateHabitCount(context, habitId, result.NewCount);
				}

				HabitsAppWidgetProvider.UpdateAllWidgets(context);
			}
			catch
			{
				HabitsAppWidgetProvider.UpdateAllWidgets(context);
			}
		});
	}
}
