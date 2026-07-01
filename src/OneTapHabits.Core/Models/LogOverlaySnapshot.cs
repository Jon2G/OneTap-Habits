namespace OneTapHabits.Models;

public sealed class LogOverlaySnapshot
{
	public List<LogOverlayEntry> Entries { get; set; } = [];
}

public sealed class LogOverlayEntry
{
	public string UserId { get; set; } = string.Empty;

	public string HabitId { get; set; } = string.Empty;

	public string Date { get; set; } = string.Empty;

	public int Count { get; set; }
}
