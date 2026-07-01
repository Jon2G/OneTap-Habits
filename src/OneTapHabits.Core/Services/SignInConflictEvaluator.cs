namespace OneTapHabits.Services;

public enum SignInDataResolution
{
	UseThisDevice,
	KeepCloud
}

public sealed class SignInConflictInfo
{
	public bool NeedsUserChoice { get; init; }

	public SignInDataResolution? AutoResolution { get; init; }

	public int LocalHabitCount { get; init; }

	public int CloudHabitCount { get; init; }

	public bool CloudHasData { get; init; }

	public static SignInConflictInfo Evaluate(
		bool meaningfulLocalData,
		bool cloudHasData,
		int localHabitCount,
		int cloudHabitCount)
	{
		if (meaningfulLocalData)
		{
			return new SignInConflictInfo
			{
				NeedsUserChoice = true,
				LocalHabitCount = localHabitCount,
				CloudHabitCount = cloudHabitCount,
				CloudHasData = cloudHasData
			};
		}

		return new SignInConflictInfo
		{
			NeedsUserChoice = false,
			AutoResolution = SignInDataResolution.KeepCloud,
			LocalHabitCount = localHabitCount,
			CloudHabitCount = cloudHabitCount,
			CloudHasData = cloudHasData
		};
	}
}
