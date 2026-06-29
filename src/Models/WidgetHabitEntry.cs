namespace OneTapHabits.Models;

public sealed class WidgetHabitEntry
{
	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string ColorHex { get; set; } = HabitColorPalette.Default;
	public int Count { get; set; }
	public int TimesPerDay { get; set; } = 1;
}
