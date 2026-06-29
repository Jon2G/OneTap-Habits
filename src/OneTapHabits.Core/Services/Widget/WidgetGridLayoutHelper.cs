namespace OneTapHabits.Services.Widget;

public static class WidgetGridLayoutHelper
{
	public sealed class GridLayoutConfig
	{
		public bool ShowRow0 { get; init; }
		public bool ShowRow1 { get; init; }
		public bool ShowRow2 { get; init; }
		public bool[] VisibleCells { get; init; } = new bool[6];

		public bool SpanLastCellFullWidth { get; init; }
	}

	public static GridLayoutConfig GetLayout(int habitCount)
	{
		var cells = new bool[6];
		var count = Math.Clamp(habitCount, 0, 6);
		for (var i = 0; i < count; i++)
		{
			cells[i] = true;
		}

		return count switch
		{
			0 => new GridLayoutConfig(),
			1 => new GridLayoutConfig { ShowRow0 = true, VisibleCells = cells, SpanLastCellFullWidth = true },
			2 => new GridLayoutConfig { ShowRow0 = true, VisibleCells = cells },
			3 => new GridLayoutConfig { ShowRow0 = true, ShowRow1 = true, VisibleCells = cells, SpanLastCellFullWidth = true },
			4 => new GridLayoutConfig { ShowRow0 = true, ShowRow1 = true, VisibleCells = cells },
			_ => new GridLayoutConfig { ShowRow0 = true, ShowRow1 = true, ShowRow2 = true, VisibleCells = cells }
		};
	}
}
