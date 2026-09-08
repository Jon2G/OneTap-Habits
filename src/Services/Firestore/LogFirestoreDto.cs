using OneTapHabits.Models;
#if ANDROID || IOS
using Plugin.Firebase.Firestore;
#endif

namespace OneTapHabits.Services.Firestore;

public sealed class LogFirestoreDto
#if ANDROID || IOS
	: IFirestoreObject
#endif
{
	public LogFirestoreDto()
	{
	}

#if ANDROID || IOS
	[FirestoreProperty("habit_id")]
#endif
	public string HabitId { get; set; } = string.Empty;

#if ANDROID || IOS
	[FirestoreProperty("date")]
#endif
	public string Date { get; set; } = string.Empty;

#if ANDROID || IOS
	[FirestoreProperty("is_completed")]
#endif
	public bool IsCompleted { get; set; }

#if ANDROID || IOS
	[FirestoreProperty("count")]
#endif
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
