using OneTapHabits.Services.Widget;
using Xunit;

namespace OneTapHabits.Tests;

public class WidgetAppearanceHelperTests
{
	[Fact]
	public void BlendCellBackground_ZeroPercent_ReturnsBaseColor()
	{
		var result = WidgetAppearanceHelper.BlendCellBackground("#22C55E", 0);
		Assert.Equal(WidgetAppearanceHelper.BaseCellColorHex, result, ignoreCase: true);
	}

	[Fact]
	public void BlendCellBackground_HundredPercent_ReturnsHabitColor()
	{
		var result = WidgetAppearanceHelper.BlendCellBackground("#22C55E", 100);
		Assert.Equal("#22C55E", result, ignoreCase: true);
	}

	[Fact]
	public void BlendCellBackground_TwentyFivePercent_IsBetweenBaseAndHabit()
	{
		var result = WidgetAppearanceHelper.BlendCellBackground("#22C55E", 25);
		var baseRgb = ParseHex(WidgetAppearanceHelper.BaseCellColorHex);
		var habitRgb = ParseHex("#22C55E");
		var blendedRgb = ParseHex(result);

		AssertChannelBetween(baseRgb.R, habitRgb.R, blendedRgb.R);
		AssertChannelBetween(baseRgb.G, habitRgb.G, blendedRgb.G);
		AssertChannelBetween(baseRgb.B, habitRgb.B, blendedRgb.B);
	}

	private static void AssertChannelBetween(int baseChannel, int habitChannel, int blendedChannel)
	{
		var min = Math.Min(baseChannel, habitChannel);
		var max = Math.Max(baseChannel, habitChannel);
		Assert.InRange(blendedChannel, min, max);
		Assert.NotEqual(baseChannel, blendedChannel);
		Assert.NotEqual(habitChannel, blendedChannel);
	}

	[Fact]
	public void BlendCellBackground_InvalidHex_UsesPaletteDefault()
	{
		var expected = WidgetAppearanceHelper.BlendCellBackground(HabitColorPalette.Default, 25);
		var result = WidgetAppearanceHelper.BlendCellBackground("#ABCDEF", 25);
		Assert.Equal(expected, result, ignoreCase: true);
	}

	[Theory]
	[InlineData(15, WidgetTintStrength.Subtle)]
	[InlineData(25, WidgetTintStrength.Medium)]
	[InlineData(40, WidgetTintStrength.Strong)]
	[InlineData(99, WidgetTintStrength.Medium)]
	public void ParseTintStrength_MapsExpectedValues(int percent, WidgetTintStrength expected)
	{
		Assert.Equal(expected, WidgetAppearanceHelper.ParseTintStrength(percent));
	}

	private static (int R, int G, int B) ParseHex(string hex)
	{
		var r = Convert.ToInt32(hex[1..3], 16);
		var g = Convert.ToInt32(hex[3..5], 16);
		var b = Convert.ToInt32(hex[5..7], 16);
		return (r, g, b);
	}
}
