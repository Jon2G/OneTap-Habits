using OneTapHabits.Models;

namespace OneTapHabits.Services;

public static class HabitSortOrderHelper
{
	/// <summary>
	/// One-time legacy migration when every habit still has default SortOrder 0 from older data.
	/// Does not run when habits have explicit order (e.g. first item legitimately at 0 after reorder).
	/// </summary>
	public static bool TryApplyLegacySortOrders(IList<Habit> habits)
	{
		if (habits.Count <= 1 || !habits.All(h => h.SortOrder == 0))
		{
			return false;
		}

		ApplyAlphabeticalSortOrders(habits);
		return true;
	}

	public static void ApplyAlphabeticalSortOrders(IList<Habit> habits)
	{
		var ordered = habits.OrderBy(h => h.Name, StringComparer.OrdinalIgnoreCase).ToList();
		for (var i = 0; i < ordered.Count; i++)
		{
			ordered[i].SortOrder = i;
		}
	}

	public static int GetNextSortOrder(IEnumerable<Habit> habits) =>
		habits.Select(h => h.SortOrder).DefaultIfEmpty(-1).Max() + 1;
}
