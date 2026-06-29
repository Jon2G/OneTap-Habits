using Android.App;
using Android.Content;

namespace OneTapHabits.Platforms.Android.AppWidgets;

[BroadcastReceiver(
	Name = WidgetConstants.PackageName + ".AppWidgets.WidgetBootReceiver",
	Exported = true,
	DirectBootAware = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionLockedBootCompleted })]
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public class WidgetBootReceiver : BroadcastReceiver
{
	public override void OnReceive(Context? context, Intent? intent)
	{
		if (context is null)
		{
			return;
		}

		HabitsAppWidgetProvider.UpdateAllWidgets(context);
	}
}
