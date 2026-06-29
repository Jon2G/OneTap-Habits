using OneTapHabits.Models;

namespace OneTapHabits.Calendar;

public static class GuestLogQuery
{
	public static IReadOnlyList<HabitLog> FilterCompletedInRange(
		GuestDataSnapshot guest,
		DateOnly startInclusive,
		DateOnly endInclusive)
	{
		if (endInclusive < startInclusive)
		{
			return [];
		}

		return guest.Logs
			.Where(l => l.IsCompleted || l.Count > 0)
			.Select(ParseEntry)
			.Where(l => l is not null && l.Date >= startInclusive && l.Date <= endInclusive)
			.Cast<HabitLog>()
			.ToList();
	}

	private static HabitLog? ParseEntry(GuestLogEntry entry)
	{
		if (!DateOnly.TryParseExact(entry.Date, "yyyy-MM-dd", out var date))
		{
			return null;
		}

		var count = entry.Count > 0 ? entry.Count : entry.IsCompleted ? 1 : 0;
		return new HabitLog
		{
			Id = HabitLog.CreateId(date, entry.HabitId),
			HabitId = entry.HabitId,
			Date = date,
			IsCompleted = count > 0,
			Count = count
		};
	}
}
