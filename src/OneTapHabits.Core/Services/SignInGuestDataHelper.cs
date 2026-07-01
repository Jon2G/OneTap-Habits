using OneTapHabits.Models;

namespace OneTapHabits.Services;

public static class SignInGuestDataHelper
{
	public const string SampleHabitIdsPreferenceKey = "sample_habit_ids";

	public static string FormatSampleHabitIds(IEnumerable<string> habitIds) =>
		string.Join(',', habitIds);

	public static HashSet<string> ParseSampleHabitIds(string? raw) =>
		string.IsNullOrWhiteSpace(raw)
			? []
			: raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.ToHashSet(StringComparer.Ordinal);

	public static bool HasMeaningfulGuestData(GuestDataSnapshot guest, IReadOnlySet<string> sampleHabitIds)
	{
		if (guest.Logs.Any(log => IsMeaningfulLog(log) && !sampleHabitIds.Contains(log.HabitId)))
		{
			return true;
		}

		var activeHabits = guest.Habits.Where(h => h.IsActive).ToList();
		if (activeHabits.Count == 0)
		{
			return false;
		}

		if (sampleHabitIds.Count == 0)
		{
			return true;
		}

		return !activeHabits.All(h => sampleHabitIds.Contains(h.Id));
	}

	public static int CountLocalHabits(GuestDataSnapshot guest) =>
		guest.Habits.Count(h => h.IsActive);

	public static bool HasCloudData(IReadOnlyList<Habit> cloudHabits, IReadOnlyList<GuestLogEntry> cloudLogs) =>
		cloudHabits.Count > 0 || cloudLogs.Any(IsMeaningfulLog);

	private static bool IsMeaningfulLog(GuestLogEntry log) =>
		log.IsCompleted || log.Count > 0;
}
