namespace OneTapHabits.Models;

public sealed class GuestDataSnapshot
{
	public List<Habit> Habits { get; set; } = [];

	public List<GuestLogEntry> Logs { get; set; } = [];
}

public sealed class GuestLogEntry
{
	public string HabitId { get; set; } = string.Empty;

	public string Date { get; set; } = string.Empty;

	public bool IsCompleted { get; set; }
}
