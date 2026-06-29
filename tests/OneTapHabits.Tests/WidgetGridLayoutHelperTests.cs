using OneTapHabits.Services.Widget;
using Xunit;

namespace OneTapHabits.Tests;

public class WidgetGridLayoutHelperTests
{
	[Theory]
	[InlineData(1, 1, true, false, false, true)]
	[InlineData(2, 2, true, false, false, false)]
	[InlineData(3, 3, true, true, false, true)]
	[InlineData(4, 4, true, true, false, false)]
	[InlineData(6, 6, true, true, true, false)]
	public void GetLayout_ShowsExpectedRows(
		int habitCount,
		int visibleCellCount,
		bool showRow0,
		bool showRow1,
		bool showRow2,
		bool spanFull)
	{
		var layout = WidgetGridLayoutHelper.GetLayout(habitCount);

		Assert.Equal(showRow0, layout.ShowRow0);
		Assert.Equal(showRow1, layout.ShowRow1);
		Assert.Equal(showRow2, layout.ShowRow2);
		Assert.Equal(visibleCellCount, layout.VisibleCells.Count(c => c));
		Assert.Equal(spanFull, layout.SpanLastCellFullWidth);
	}

	[Fact]
	public void GetLayout_CapsAtSixCells()
	{
		var layout = WidgetGridLayoutHelper.GetLayout(10);
		Assert.Equal(6, layout.VisibleCells.Count(c => c));
		Assert.True(layout.ShowRow2);
	}
}
