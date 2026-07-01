using OneTapHabits.Models;

namespace OneTapHabits.Firestore;

public static class CloudDocumentSanitizer
{
	public const string HabitNameField = "name";
	public const string LogHabitIdField = "habit_id";
	public const string LogCountField = "count";
	public const string LogIsCompletedField = "is_completed";

	public static bool ShouldDeleteHabitDocument(IReadOnlyDictionary<string, object>? data) =>
		data is null
		|| data.Count == 0
		|| !TryGetNonEmptyString(data, HabitNameField, out _);

	public static bool ShouldDeleteLogDocument(string documentId, IReadOnlyDictionary<string, object>? data)
	{
		if (data is null || data.Count == 0)
		{
			return true;
		}

		var habitIdFromData = TryGetNonEmptyString(data, LogHabitIdField, out var habitId)
			? habitId
			: string.Empty;
		var habitIdFromDoc = HabitLogDocumentId.ResolveHabitId(documentId, habitIdFromData);
		if (string.IsNullOrEmpty(habitIdFromDoc))
		{
			return true;
		}

		return !HasPositiveCompletionSignal(data);
	}

	public static bool IsValidHabitName(string? name) => !string.IsNullOrWhiteSpace(name);

	public static bool HasPositiveCompletionSignal(IReadOnlyDictionary<string, object> data)
	{
		if (data.ContainsKey(LogCountField) && TryGetInt(data, LogCountField, out var count) && count > 0)
		{
			return true;
		}

		if (data.ContainsKey(LogIsCompletedField) && TryGetBool(data, LogIsCompletedField, out var completed) && completed)
		{
			return true;
		}

		return false;
	}

	private static bool TryGetNonEmptyString(IReadOnlyDictionary<string, object> data, string key, out string value)
	{
		value = string.Empty;
		if (!data.TryGetValue(key, out var raw) || raw is null)
		{
			return false;
		}

		value = raw.ToString() ?? string.Empty;
		return !string.IsNullOrWhiteSpace(value);
	}

	private static bool TryGetInt(IReadOnlyDictionary<string, object> data, string key, out int value)
	{
		value = 0;
		if (!data.TryGetValue(key, out var raw) || raw is null)
		{
			return false;
		}

		return raw switch
		{
			int i => (value = i) == i,
			long l => (value = (int)l) == l,
			double d => (value = (int)d) == d,
			_ => int.TryParse(raw.ToString(), out value)
		};
	}

	private static bool TryGetBool(IReadOnlyDictionary<string, object> data, string key, out bool value)
	{
		value = false;
		if (!data.TryGetValue(key, out var raw) || raw is null)
		{
			return false;
		}

		return raw switch
		{
			bool b => (value = b) == b,
			_ => bool.TryParse(raw.ToString(), out value)
		};
	}
}
