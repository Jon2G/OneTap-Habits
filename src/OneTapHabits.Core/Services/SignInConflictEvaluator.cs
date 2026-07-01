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

	public static SignInConflictInfo Evaluate(
		bool meaningfulLocalData,
		bool cloudHasData,
		int localHabitCount,
		int cloudHabitCount)
	{
		if (meaningfulLocalData && cloudHasData)
		{
			return new SignInConflictInfo
			{
				NeedsUserChoice = true,
				LocalHabitCount = localHabitCount,
				CloudHabitCount = cloudHabitCount
			};
		}

		var resolution = meaningfulLocalData
			? SignInDataResolution.UseThisDevice
			: SignInDataResolution.KeepCloud;

		return new SignInConflictInfo
		{
			NeedsUserChoice = false,
			AutoResolution = resolution,
			LocalHabitCount = localHabitCount,
			CloudHabitCount = cloudHabitCount
		};
	}
}
