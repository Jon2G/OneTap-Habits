using OneTapHabits;
using Xunit;

namespace OneTapHabits.Tests;

public class SampleHabitSeedTests
{
	[Fact]
	public void Create_returns_two_everyday_widget_habits()
	{
		var habits = SampleHabitSeed.Create("Go to gym", "Take medication");

		Assert.Equal(2, habits.Count);
		Assert.Equal("Go to gym", habits[0].Name);
		Assert.Equal("Take medication", habits[1].Name);
		Assert.All(habits, h =>
		{
			Assert.True(h.ShowInWidget);
			Assert.Equal(7, h.TargetDays.Count);
			Assert.True(h.IsActive);
			Assert.False(string.IsNullOrWhiteSpace(h.Id));
		});
	}
}
