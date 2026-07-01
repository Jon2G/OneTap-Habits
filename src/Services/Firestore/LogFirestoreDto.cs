using OneTapHabits.Models;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Services.Firestore;

public sealed class LogFirestoreDto : IFirestoreObject
{
	public LogFirestoreDto()
	{
	}

	[FirestoreProperty("habit_id")]
	public string HabitId { get; set; } = string.Empty;

	[FirestoreProperty("date")]
	public string Date { get; set; } = string.Empty;

	[FirestoreProperty("is_completed")]
	public bool IsCompleted { get; set; }

	[FirestoreProperty("count")]
	public int Count { get; set; }

	public static LogFirestoreDto FromEntry(string habitId, DateOnly date, int count) => new()
	{
		HabitId = habitId,
		Date = date.ToString("O"),
		IsCompleted = count > 0,
		Count = count
	};

	public int ResolveCount() => Count > 0 ? Count : IsCompleted ? 1 : 0;

	public HabitLog ToModel(string id, string habitId, DateOnly date)
	{
		var count = ResolveCount();
		return new HabitLog
		{
			Id = id,
			HabitId = habitId,
			Date = date,
			IsCompleted = count > 0,
			Count = count
		};
	}
}
