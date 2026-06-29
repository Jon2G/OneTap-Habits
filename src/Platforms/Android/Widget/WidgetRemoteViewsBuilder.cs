using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using OneTapHabits.Models;
using OneTapHabits.Platforms.Android.Services;
using OneTapHabits.Services.Widget;

namespace OneTapHabits.Platforms.Android.AppWidgets;

public static class WidgetRemoteViewsBuilder
{
	private const string Tag = "OneTapWidget";

	private static readonly int[] CellIds =
	[
		Resource.Id.cell0, Resource.Id.cell1, Resource.Id.cell2,
		Resource.Id.cell3, Resource.Id.cell4, Resource.Id.cell5
	];

	private static readonly int[] NameIds =
	[
		Resource.Id.cell0_name, Resource.Id.cell1_name, Resource.Id.cell2_name,
		Resource.Id.cell3_name, Resource.Id.cell4_name, Resource.Id.cell5_name
	];

	private static readonly int[] ColorBarIds =
	[
		Resource.Id.cell0_color, Resource.Id.cell1_color, Resource.Id.cell2_color,
		Resource.Id.cell3_color, Resource.Id.cell4_color, Resource.Id.cell5_color
	];

	public static RemoteViews Build(Context context, WidgetSnapshot snapshot)
	{
		var views = new RemoteViews(context.PackageName!, Resource.Layout.widget_habits);

		if (!snapshot.IsSignedIn)
		{
			ShowOpenAppEmpty(views, context);
			AttachOpenAppClick(views, context);
			return views;
		}

		if (snapshot.Habits.Count == 0)
		{
			var message = snapshot.OverflowCount > 0
				? context.GetString(Resource.String.widget_overflow, snapshot.OverflowCount)!
				: context.GetString(Resource.String.widget_all_done)!;
			ShowEmpty(views, message);
			AttachOpenAppClick(views, context);
			return views;
		}

		views.SetViewVisibility(Resource.Id.empty_container, ViewStates.Gone);
		views.SetViewVisibility(Resource.Id.grid_container, ViewStates.Visible);

		var layout = OneTapHabits.Services.Widget.WidgetGridLayoutHelper.GetLayout(snapshot.Habits.Count);
		views.SetViewVisibility(Resource.Id.row0, layout.ShowRow0 ? ViewStates.Visible : ViewStates.Gone);
		views.SetViewVisibility(Resource.Id.row1, layout.ShowRow1 ? ViewStates.Visible : ViewStates.Gone);
		views.SetViewVisibility(Resource.Id.row2, layout.ShowRow2 ? ViewStates.Visible : ViewStates.Gone);

		for (var i = 0; i < CellIds.Length; i++)
		{
			if (!layout.VisibleCells[i])
			{
				views.SetViewVisibility(CellIds[i], ViewStates.Gone);
				continue;
			}

			var habit = snapshot.Habits[i];
			views.SetViewVisibility(CellIds[i], ViewStates.Visible);
			views.SetTextViewText(NameIds[i], habit.Name);
			if (HabitNameDisplayHelper.IsSingleEmoji(habit.Name))
			{
				views.SetTextViewTextSize(
					NameIds[i],
					(int)ComplexUnitType.Sp,
					HabitNameDisplayHelper.WidgetSingleEmojiTextSizeSp);
				views.SetInt(NameIds[i], "setMaxLines", 1);
			}

			views.SetInt(ColorBarIds[i], "setBackgroundColor", ParseColor(habit.ColorHex));
			views.SetOnClickPendingIntent(CellIds[i], CreateCompleteIntent(context, habit.Id, i));
		}

		if (snapshot.OverflowCount > 0)
		{
			views.SetViewVisibility(Resource.Id.overflow_label, ViewStates.Visible);
			views.SetTextViewText(
				Resource.Id.overflow_label,
				context.GetString(Resource.String.widget_overflow, snapshot.OverflowCount)!);
			AttachOpenAppClick(views, context, Resource.Id.overflow_label);
		}
		else
		{
			views.SetViewVisibility(Resource.Id.overflow_label, ViewStates.Gone);
		}

		return views;
	}

	public static void UpdateSignInFallback(Context context, AppWidgetManager appWidgetManager, int widgetId)
	{
		try
		{
			var views = new RemoteViews(context.PackageName!, Resource.Layout.widget_habits);
			ShowOpenAppEmpty(views, context);
			AttachOpenAppClick(views, context);
			appWidgetManager.UpdateAppWidget(widgetId, views);
		}
		catch (Exception ex)
		{
			Log.Error(Tag, $"Fallback widget update failed for widget {widgetId}: {ex}");
		}
	}

	private static void ShowOpenAppEmpty(RemoteViews views, Context context)
	{
		views.SetViewVisibility(Resource.Id.grid_container, ViewStates.Gone);
		views.SetViewVisibility(Resource.Id.overflow_label, ViewStates.Gone);
		views.SetViewVisibility(Resource.Id.empty_container, ViewStates.Visible);
		views.SetTextViewText(Resource.Id.empty_message, context.GetString(Resource.String.widget_open_app)!);
		views.SetTextViewText(Resource.Id.empty_hint, context.GetString(Resource.String.widget_open_app_hint)!);
		views.SetViewVisibility(Resource.Id.empty_hint, ViewStates.Visible);
	}

	private static void ShowEmpty(RemoteViews views, string message)
	{
		views.SetViewVisibility(Resource.Id.grid_container, ViewStates.Gone);
		views.SetViewVisibility(Resource.Id.overflow_label, ViewStates.Gone);
		views.SetViewVisibility(Resource.Id.empty_container, ViewStates.Visible);
		views.SetTextViewText(Resource.Id.empty_message, message);
		views.SetViewVisibility(Resource.Id.empty_hint, ViewStates.Gone);
	}

	private static void AttachOpenAppClick(RemoteViews views, Context context, int? extraViewId = null)
	{
		var openApp = CreateOpenAppIntent(context);
		if (openApp is null)
		{
			return;
		}

		views.SetOnClickPendingIntent(Resource.Id.empty_container, openApp);
		views.SetOnClickPendingIntent(Resource.Id.widget_root, openApp);
		if (extraViewId is int viewId)
		{
			views.SetOnClickPendingIntent(viewId, openApp);
		}
	}

	private static PendingIntent? CreateCompleteIntent(Context context, string habitId, int requestCode)
	{
		var intent = new Intent(WidgetConstants.ActionCompleteHabit);
		intent.SetComponent(new ComponentName(context, WidgetConstants.PackageName + ".AppWidgets.WidgetTapReceiver"));
		intent.PutExtra(WidgetConstants.ExtraHabitId, habitId);
		intent.SetData(global::Android.Net.Uri.Parse($"onetaphabits://habit/{habitId}"));

		var flags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
		return PendingIntent.GetBroadcast(context, requestCode + 100, intent, flags);
	}

	private static PendingIntent? CreateOpenAppIntent(Context context)
	{
		var intent = new Intent(context, typeof(MainActivity));
		intent.SetAction(WidgetConstants.ActionOpenApp);
		intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);

		var flags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
		return PendingIntent.GetActivity(context, 0, intent, flags);
	}

	private static int ParseColor(string colorHex)
	{
		try
		{
			return global::Android.Graphics.Color.ParseColor(colorHex).ToArgb();
		}
		catch
		{
			return global::Android.Graphics.Color.ParseColor("#4CAF50").ToArgb();
		}
	}
}
