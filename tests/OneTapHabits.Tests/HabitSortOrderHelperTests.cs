using OneTapHabits.Models;
using OneTapHabits.Services;
using Xunit;

namespace OneTapHabits.Tests;

public class HabitSortOrderHelperTests
{
	[Fact]
	public void TryApplyLegacySortOrders_AssignsAlphabeticalOrderWhenAllZero()
	{
		var habits = new List<Habit>
		{
			new() { Id = "b", Name = "Brush teeth", SortOrder = 0 },
			new() { Id = "a", Name = "Apple", SortOrder = 0 }
		};

		Assert.True(HabitSortOrderHelper.TryApplyLegacySortOrders(habits));
		Assert.Equal(0, habits.First(h => h.Id == "a").SortOrder);
		Assert.Equal(1, habits.First(h => h.Id == "b").SortOrder);
	}

	[Fact]
	public void TryApplyLegacySortOrders_DoesNotResetExplicitOrderStartingAtZero()
	{
		var habits = new List<Habit>
		{
			new() { Id = "c", Name = "Brush teeth", SortOrder = 0 },
			new() { Id = "a", Name = "Apple", SortOrder = 1 },
			new() { Id = "b", Name = "Banana", SortOrder = 2 }
		};

		Assert.False(HabitSortOrderHelper.TryApplyLegacySortOrders(habits));
		Assert.Equal(0, habits.First(h => h.Id == "c").SortOrder);
		Assert.Equal(1, habits.First(h => h.Id == "a").SortOrder);
		Assert.Equal(2, habits.First(h => h.Id == "b").SortOrder);
	}

	[Fact]
	public void GetNextSortOrder_ReturnsMaxPlusOne()
	{
		var habits = new List<Habit>
		{
			new() { SortOrder = 0 },
			new() { SortOrder = 5 }
		};

		Assert.Equal(6, HabitSortOrderHelper.GetNextSortOrder(habits));
	}
}
