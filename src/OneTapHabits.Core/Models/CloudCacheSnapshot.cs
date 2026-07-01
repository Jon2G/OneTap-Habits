namespace OneTapHabits.Models;

public sealed class CloudCacheFile
{
	public List<CloudCacheUserData> Users { get; set; } = [];
}

public sealed class CloudCacheUserData
{
	public string UserId { get; set; } = string.Empty;

	public List<Habit> Habits { get; set; } = [];

	public List<GuestLogEntry> Logs { get; set; } = [];
}
