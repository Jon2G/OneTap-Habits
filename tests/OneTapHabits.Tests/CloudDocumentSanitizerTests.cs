using OneTapHabits.Firestore;
using Xunit;

namespace OneTapHabits.Tests;

public class CloudDocumentSanitizerTests
{
	[Fact]
	public void ShouldDeleteHabitDocument_WhenEmpty()
	{
		Assert.True(CloudDocumentSanitizer.ShouldDeleteHabitDocument(null));
		Assert.True(CloudDocumentSanitizer.ShouldDeleteHabitDocument(new Dictionary<string, object>()));
	}

	[Fact]
	public void ShouldDeleteHabitDocument_WhenNameMissingOrBlank()
	{
		Assert.True(CloudDocumentSanitizer.ShouldDeleteHabitDocument(
			new Dictionary<string, object> { ["is_active"] = true }));
		Assert.True(CloudDocumentSanitizer.ShouldDeleteHabitDocument(
			new Dictionary<string, object> { ["name"] = "   " }));
	}

	[Fact]
	public void ShouldKeepHabitDocument_WhenNamePresent()
	{
		Assert.False(CloudDocumentSanitizer.ShouldDeleteHabitDocument(
			new Dictionary<string, object> { ["name"] = "Gym" }));
	}

	[Fact]
	public void ShouldDeleteLogDocument_WhenEmpty()
	{
		Assert.True(CloudDocumentSanitizer.ShouldDeleteLogDocument("2026-07-01_abc", null));
		Assert.True(CloudDocumentSanitizer.ShouldDeleteLogDocument("2026-07-01_abc", new Dictionary<string, object>()));
	}

	[Fact]
	public void ShouldDeleteLogDocument_WhenNoCompletionFields()
	{
		Assert.True(CloudDocumentSanitizer.ShouldDeleteLogDocument(
			"2026-07-01_abc",
			new Dictionary<string, object> { ["habit_id"] = "abc" }));
	}

	[Fact]
	public void ShouldKeepLogDocument_WhenCountPresent()
	{
		Assert.False(CloudDocumentSanitizer.ShouldDeleteLogDocument(
			"2026-07-01_abc",
			new Dictionary<string, object> { ["count"] = 2 }));
	}

	[Fact]
	public void ShouldKeepLogDocument_WhenHabitIdOnlyInDocumentIdAndCompleted()
	{
		Assert.False(CloudDocumentSanitizer.ShouldDeleteLogDocument(
			"2026-07-01_abc",
			new Dictionary<string, object> { ["is_completed"] = true }));
	}

	[Fact]
	public void ShouldDeleteLogDocument_WhenDocumentIdUnparseableAndNoHabitId()
	{
		Assert.True(CloudDocumentSanitizer.ShouldDeleteLogDocument(
			"invalid",
			new Dictionary<string, object> { ["is_completed"] = true }));
	}
}
