namespace OneTapHabits.Platforms.Android.AppWidgets;

public enum WidgetTapAnimationKind
{
	None = 0,
	PlusOne = 1,
	Complete = 2
}

public sealed class WidgetTapAnimation
{
	public int CellIndex { get; init; }

	public WidgetTapAnimationKind Kind { get; init; }
}
