using OneTapHabits.Widget;

namespace OneTapHabits.Platforms.Android.AppWidgets;

public sealed class WidgetTapAnimation
{
	public int CellIndex { get; init; }

	public WidgetTapAnimationKind Kind { get; init; }
}
