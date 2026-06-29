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

		Services.WidgetSnapshotStore.RemoveHabit(context, habitId);
		HabitsAppWidgetProvider.UpdateAllWidgets(context);

		Task.Run(async () =>
		{
			try
			{
				await Services.WidgetCompletionService.WriteCompletionAsync(context, habitId);
			}
			catch
			{
				// Snapshot already updated; app refresh will reconcile on next open.
			}
		});
	}
}
