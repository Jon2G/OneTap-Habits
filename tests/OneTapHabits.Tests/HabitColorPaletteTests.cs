using Xunit;

namespace OneTapHabits.Tests;

public class HabitColorPaletteTests
{
	[Fact]
	public void PickRandom_returns_preset_color()
	{
		for (var i = 0; i < 20; i++)
		{
			Assert.Contains(HabitColorPalette.PickRandom(), HabitColorPalette.All);
		}
	}

	[Theory]
	[InlineData("#22C55E")]
	[InlineData("#3b82f6")]
	[InlineData("F97316")]
	public void Normalize_accepts_valid_preset(string hex)
	{
		var normalized = HabitColorPalette.Normalize(hex);
		Assert.Contains(normalized, HabitColorPalette.All);
	}

	[Fact]
	public void Normalize_falls_back_to_default_for_unknown()
	{
		Assert.Equal(HabitColorPalette.Default, HabitColorPalette.Normalize("#ABCDEF"));
	}
}
