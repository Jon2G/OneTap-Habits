using OneTapHabits.Firestore;
using OneTapHabits.Models;
using OneTapHabits.Services.Firestore;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Services;

public static class CloudSnapshotFetcher
{
	public static async Task<IReadOnlyList<Habit>> FetchHabitsAsync(
		IFirebaseFirestore firestore,
		string userId,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var habitsSnapshot = await HabitsCollection(firestore, userId).GetDocumentsAsync<HabitFirestoreDto>();
		return habitsSnapshot.Documents
			.Where(d => d.Data is not null && d.Data.IsActive && d.Data.IsValidCloudDocument())
			.Select(d => d.Data!.ToModel(d.Reference.Id))
			.ToList();
	}

	public static async Task<IReadOnlyList<GuestLogEntry>> FetchLogsAsync(
		IFirebaseFirestore firestore,
		string userId,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var logsSnapshot = await LogsCollection(firestore, userId).GetDocumentsAsync<LogFirestoreDto>();
		return logsSnapshot.Documents
			.Where(d => d.Data is not null && IsValidLogDocument(d.Reference.Id, d.Data!))
			.Select(d =>
			{
				var dto = d.Data!;
				var habitId = HabitLogDocumentId.ResolveHabitId(d.Reference.Id, dto.HabitId);
				var date = HabitLogDocumentId.TryParse(d.Reference.Id, out var fromId, out _)
					? fromId
					: DateOnly.TryParse(dto.Date, out var parsed)
						? parsed
						: DateOnly.FromDateTime(DateTime.Today);
				var count = dto.ResolveCount();
				return new GuestLogEntry
				{
					HabitId = habitId,
					Date = date.ToString("yyyy-MM-dd"),
					IsCompleted = count > 0,
					Count = count
				};
			})
			.Where(e => !string.IsNullOrEmpty(e.HabitId) && e.Count > 0)
			.ToList();
	}

	internal static bool IsValidLogDocument(string documentId, LogFirestoreDto dto)
	{
		if (dto.ResolveCount() <= 0)
		{
			return false;
		}

		var habitId = HabitLogDocumentId.ResolveHabitId(documentId, dto.HabitId);
		return !string.IsNullOrEmpty(habitId);
	}

	private static ICollectionReference HabitsCollection(IFirebaseFirestore firestore, string userId) =>
		firestore.GetCollection($"users/{userId}/habits");

	private static ICollectionReference LogsCollection(IFirebaseFirestore firestore, string userId) =>
		firestore.GetCollection($"users/{userId}/logs");
}
