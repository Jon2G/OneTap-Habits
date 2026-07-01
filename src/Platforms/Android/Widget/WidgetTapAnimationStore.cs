using Android.Content;

namespace OneTapHabits.Platforms.Android.AppWidgets;

public static class WidgetTapAnimationStore
{
	private const string PrefsName = "onetap_widget_animation";
	private const string KeyCellIndex = "cell_index";
	private const string KeyKind = "kind";

	public static void Set(Context context, int cellIndex, WidgetTapAnimationKind kind)
	{
		var editor = context.GetSharedPreferences(PrefsName, FileCreationMode.Private)!.Edit()!;
		editor.PutInt(KeyCellIndex, cellIndex);
		editor.PutInt(KeyKind, (int)kind);
		editor.Apply();
	}

	public static WidgetTapAnimation? Load(Context context)
	{
		var prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
		if (prefs is null)
		{
			return null;
		}

		var kind = (WidgetTapAnimationKind)prefs.GetInt(KeyKind, (int)WidgetTapAnimationKind.None);
		if (kind == WidgetTapAnimationKind.None)
		{
			return null;
		}

		return new WidgetTapAnimation
		{
			CellIndex = prefs.GetInt(KeyCellIndex, -1),
			Kind = kind
		};
	}

	public static void Clear(Context context) =>
		Set(context, -1, WidgetTapAnimationKind.None);
}
