using OneTapHabits.Services.Widget;
using Xunit;

namespace OneTapHabits.Tests;

public class HabitNameDisplayHelperTests
{
	[Theory]
	[InlineData("💪")]
	[InlineData("  🏋️  ")]
	[InlineData("👨‍👩‍👧")]
	[InlineData("🇺🇸")]
	[InlineData("1️⃣")]
	public void IsSingleEmoji_ReturnsTrue_ForSingleEmojiNames(string name)
	{
		Assert.True(HabitNameDisplayHelper.IsSingleEmoji(name));
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("💪 gym")]
	[InlineData("💪💪")]
	[InlineData("Run")]
	[InlineData("12")]
	[InlineData("✓")]
	public void IsSingleEmoji_ReturnsFalse_ForNonSingleEmojiNames(string name)
	{
		Assert.False(HabitNameDisplayHelper.IsSingleEmoji(name));
	}

	[Fact]
	public void IsSingleEmoji_ReturnsFalse_ForNull()
	{
		Assert.False(HabitNameDisplayHelper.IsSingleEmoji(null));
	}
}
