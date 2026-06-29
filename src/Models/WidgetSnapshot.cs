namespace OneTapHabits.Models;

public sealed class WidgetSnapshot
{
	public bool IsSignedIn { get; set; }

	public string DateIso { get; set; } = string.Empty;

	public List<WidgetHabitEntry> Habits { get; set; } = [];

	public int OverflowCount { get; set; }

	public static WidgetSnapshot NotSignedIn() => new() { IsSignedIn = false };

	public static WidgetSnapshot EmptySignedIn(DateOnly date) => new()
	{
		IsSignedIn = true,
		DateIso = date.ToString("O")
	};
}
