namespace OneTapHabits.Widget;

public enum WidgetTapAnimationKind
{
	None = 0,
	PlusOne = 1,
	MinusOne = 2,
	Complete = 3
}

public static class WidgetTapAnimationTiming
{
	public const int DurationMs = 450;
}

public sealed class WidgetTapAnimationState
{
	public int CellIndex { get; init; }

	public WidgetTapAnimationKind Kind { get; init; }
}
