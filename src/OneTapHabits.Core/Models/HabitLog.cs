namespace OneTapHabits.Models;

public sealed class HabitLog
{
	public string Id { get; set; } = string.Empty;
	public string HabitId { get; set; } = string.Empty;
	public DateOnly Date { get; set; }
	public bool IsCompleted { get; set; }

	public static string CreateId(DateOnly date, string habitId) => $"{date:yyyy-MM-dd}_{habitId}";
}
