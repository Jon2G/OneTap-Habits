namespace OneTapHabits;

/// <summary>
/// Curated habit accent colors aligned with the charcoal + accent design system.
/// </summary>
public static class HabitColorPalette
{
	public static readonly IReadOnlyList<string> All =
	[
		"#22C55E",
		"#10B981",
		"#14B8A6",
		"#06B6D4",
		"#0EA5E9",
		"#3B82F6",
		"#6366F1",
		"#8B5CF6",
		"#A855F7",
		"#EC4899",
		"#F43F5E",
		"#F97316",
		"#F59E0B",
		"#84CC16",
		"#64748B"
	];

	public static string Default => All[0];

	public static string PickRandom() => All[Random.Shared.Next(All.Count)];

	public static string Normalize(string? hex)
	{
		if (string.IsNullOrWhiteSpace(hex))
		{
			return Default;
		}

		var candidate = hex.Trim();
		if (!candidate.StartsWith('#'))
		{
			candidate = $"#{candidate}";
		}

		return All.FirstOrDefault(c => c.Equals(candidate, StringComparison.OrdinalIgnoreCase)) ?? Default;
	}
}
