namespace OneTapHabits.Models;

public static class HabitLogDocumentId
{
	public static bool TryParse(string documentId, out DateOnly date, out string habitId)
	{
		date = default;
		habitId = string.Empty;

		if (string.IsNullOrEmpty(documentId) || documentId.Length <= 11 || documentId[10] != '_')
		{
			return false;
		}

		if (!DateOnly.TryParseExact(documentId.AsSpan(0, 10), "yyyy-MM-dd", out date))
		{
			return false;
		}

		habitId = documentId[11..];
		return !string.IsNullOrEmpty(habitId);
	}

	public static string ResolveHabitId(string documentId, string? dtoHabitId) =>
		!string.IsNullOrWhiteSpace(dtoHabitId)
			? dtoHabitId
			: TryParse(documentId, out _, out var habitId)
				? habitId
				: string.Empty;
}
