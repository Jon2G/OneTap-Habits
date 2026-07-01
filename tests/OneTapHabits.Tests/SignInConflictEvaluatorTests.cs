using OneTapHabits.Models;
using OneTapHabits.Services;
using Xunit;

namespace OneTapHabits.Tests;

public class SignInGuestDataHelperTests
{
	private static readonly HashSet<string> SampleIds = ["sample1", "sample2"];

	[Fact]
	public void HasMeaningfulGuestData_IsFalse_WhenEmpty()
	{
		var guest = new GuestDataSnapshot();
		Assert.False(SignInGuestDataHelper.HasMeaningfulGuestData(guest, SampleIds));
	}

	[Fact]
	public void HasMeaningfulGuestData_IsFalse_WhenOnlySampleHabitsAndNoLogs()
	{
		var guest = new GuestDataSnapshot
		{
			Habits =
			[
				new Habit { Id = "sample1", Name = "Gym", IsActive = true },
				new Habit { Id = "sample2", Name = "Meds", IsActive = true }
			]
		};
		Assert.False(SignInGuestDataHelper.HasMeaningfulGuestData(guest, SampleIds));
	}

	[Fact]
	public void HasMeaningfulGuestData_IsTrue_WhenExtraHabitBeyondSamples()
	{
		var guest = new GuestDataSnapshot
		{
			Habits =
			[
				new Habit { Id = "sample1", IsActive = true },
				new Habit { Id = "custom", IsActive = true }
			]
		};
		Assert.True(SignInGuestDataHelper.HasMeaningfulGuestData(guest, SampleIds));
	}

	[Fact]
	public void HasMeaningfulGuestData_IsTrue_WhenAnyLogExists()
	{
		var guest = new GuestDataSnapshot
		{
			Habits = [new Habit { Id = "sample1", IsActive = true }],
			Logs = [new GuestLogEntry { HabitId = "sample1", Date = "2026-06-30", Count = 1, IsCompleted = true }]
		};
		Assert.True(SignInGuestDataHelper.HasMeaningfulGuestData(guest, SampleIds));
	}

	[Fact]
	public void ParseSampleHabitIds_RoundTripsIds()
	{
		var formatted = SignInGuestDataHelper.FormatSampleHabitIds(["a", "b"]);
		var parsed = SignInGuestDataHelper.ParseSampleHabitIds(formatted);
		Assert.Equal(["a", "b"], parsed.OrderBy(x => x));
	}
}

public class SignInConflictEvaluatorTests
{
	[Fact]
	public void Evaluate_NeedsUserChoice_WhenMeaningfulLocalAndCloud()
	{
		var info = SignInConflictInfo.Evaluate(meaningfulLocalData: true, cloudHasData: true, 3, 5);
		Assert.True(info.NeedsUserChoice);
		Assert.Null(info.AutoResolution);
		Assert.Equal(3, info.LocalHabitCount);
		Assert.Equal(5, info.CloudHabitCount);
	}

	[Fact]
	public void Evaluate_AutoKeepCloud_WhenOnlySampleLocalAndCloudHasData()
	{
		var info = SignInConflictInfo.Evaluate(meaningfulLocalData: false, cloudHasData: true, 2, 4);
		Assert.False(info.NeedsUserChoice);
		Assert.Equal(SignInDataResolution.KeepCloud, info.AutoResolution);
	}

	[Fact]
	public void Evaluate_AutoUseDevice_WhenMeaningfulLocalAndCloudEmpty()
	{
		var info = SignInConflictInfo.Evaluate(meaningfulLocalData: true, cloudHasData: false, 2, 0);
		Assert.False(info.NeedsUserChoice);
		Assert.Equal(SignInDataResolution.UseThisDevice, info.AutoResolution);
	}

	[Fact]
	public void Evaluate_AutoKeepCloud_WhenBothEmpty()
	{
		var info = SignInConflictInfo.Evaluate(meaningfulLocalData: false, cloudHasData: false, 0, 0);
		Assert.False(info.NeedsUserChoice);
		Assert.Equal(SignInDataResolution.KeepCloud, info.AutoResolution);
	}

	[Fact]
	public void HasCloudData_IsTrue_WhenHabitsOrLogsPresent()
	{
		Assert.True(SignInGuestDataHelper.HasCloudData(
			[new Habit { Id = "x", IsActive = true }],
			[]));
		Assert.True(SignInGuestDataHelper.HasCloudData(
			[],
			[new GuestLogEntry { HabitId = "h", Date = "2026-06-28", Count = 1, IsCompleted = true }]));
		Assert.False(SignInGuestDataHelper.HasCloudData([], []));
	}
}
