using OneTapHabits.Models;
using Xunit;

namespace OneTapHabits.Tests;

public class HabitLogDocumentIdTests
{
	[Fact]
	public void TryParse_parses_date_and_habit_id()
	{
		Assert.True(HabitLogDocumentId.TryParse("2026-07-01_abc123", out var date, out var habitId));
		Assert.Equal(new DateOnly(2026, 7, 1), date);
		Assert.Equal("abc123", habitId);
	}

	[Fact]
	public void TryParse_supports_habit_ids_with_underscores()
	{
		Assert.True(HabitLogDocumentId.TryParse("2026-07-01_part_one_part_two", out var date, out var habitId));
		Assert.Equal(new DateOnly(2026, 7, 1), date);
		Assert.Equal("part_one_part_two", habitId);
	}

	[Fact]
	public void ResolveHabitId_prefers_dto_value()
	{
		var habitId = HabitLogDocumentId.ResolveHabitId("2026-07-01_fromDoc", "fromDto");
		Assert.Equal("fromDto", habitId);
	}

	[Fact]
	public void ResolveHabitId_falls_back_to_document_id()
	{
		var habitId = HabitLogDocumentId.ResolveHabitId("2026-07-01_fromDoc", string.Empty);
		Assert.Equal("fromDoc", habitId);
	}
}
