using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Util;
using OneTapHabits.Models;
using OneTapHabits.Platforms.Android.Services;

namespace OneTapHabits.Platforms.Android.AppWidgets;

[BroadcastReceiver(
	Name = WidgetConstants.PackageName + ".AppWidgets.HabitsAppWidgetProvider",
	Exported = true)]
[IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
[MetaData("android.appwidget.provider", Resource = "@xml/habits_widget_info")]
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public class HabitsAppWidgetProvider : AppWidgetProvider
{
	private const string Tag = "OneTapWidget";

	public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
	{
		if (context is null || appWidgetManager is null || appWidgetIds is null)
		{
			return;
		}

		Log.Info(Tag, "OnUpdate for {0} widget(s)", appWidgetIds.Length);

		WidgetSnapshot snapshot;
		try
		{
			snapshot = WidgetSnapshotStore.Load(context);
		}
		catch (Exception ex)
		{
			Log.Warn(Tag, $"Failed to load widget snapshot: {ex}");
			snapshot = WidgetSnapshot.NotSignedIn();
		}

		foreach (var widgetId in appWidgetIds)
		{
			try
			{
				var animation = WidgetTapAnimationStore.Load(context);
				var views = WidgetRemoteViewsBuilder.Build(context, snapshot, animation);
				appWidgetManager.UpdateAppWidget(widgetId, views);
				Log.Info(Tag, "Updated widget {0} (signedIn={1}, habits={2})", widgetId, snapshot.IsSignedIn, snapshot.Habits.Count);
			}
			catch (Exception ex)
			{
				Log.Error(Tag, $"Widget build failed for {widgetId}: {ex}");
				WidgetRemoteViewsBuilder.UpdateSignInFallback(context, appWidgetManager, widgetId);
			}
		}
	}

	public override void OnEnabled(Context? context)
	{
		base.OnEnabled(context);
		if (context is not null)
		{
			Log.Info(Tag, "First widget enabled");
			UpdateAllWidgets(context);
		}
	}

	public override void OnAppWidgetOptionsChanged(Context? context, AppWidgetManager? appWidgetManager, int appWidgetId, Bundle? newOptions)
	{
		base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);
		if (context is not null && appWidgetManager is not null)
		{
			OnUpdate(context, appWidgetManager, [appWidgetId]);
		}
	}

	public static void UpdateAllWidgets(Context context)
	{
		var manager = AppWidgetManager.GetInstance(context);
		if (manager is null)
		{
			return;
		}

		var component = new ComponentName(context, WidgetConstants.PackageName + ".AppWidgets.HabitsAppWidgetProvider");
		var ids = manager.GetAppWidgetIds(component);
		if (ids is null || ids.Length == 0)
		{
			return;
		}

		var provider = new HabitsAppWidgetProvider();
		provider.OnUpdate(context, manager, ids);
	}
}
