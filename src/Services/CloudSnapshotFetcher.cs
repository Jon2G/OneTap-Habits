using OneTapHabits.Firestore;
using OneTapHabits.Models;
using OneTapHabits.Services.Firebase;
using OneTapHabits.Services.Firestore;

namespace OneTapHabits.Services;

public static class CloudSnapshotFetcher
{
	public static async Task<IReadOnlyList<Habit>> FetchHabitsAsync(
		IFirestoreGateway firestore,
		string userId,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var habitsSnapshot = await firestore.GetDocumentsAsync<HabitFirestoreDto>(
			$"users/{userId}/habits",
			cancellationToken);
		return habitsSnapshot
			.Where(d => d.Data.IsActive && d.Data.IsValidCloudDocument())
			.Select(d => d.Data.ToModel(d.Id))
			.ToList();
	}

	public static async Task<IReadOnlyList<GuestLogEntry>> FetchLogsAsync(
		IFirestoreGateway firestore,
		string userId,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var logsSnapshot = await firestore.GetDocumentsAsync<LogFirestoreDto>(
			$"users/{userId}/logs",
			cancellationToken);
		return logsSnapshot
			.Where(d => IsValidLogDocument(d.Id, d.Data))
			.Select(d =>
			{
				var dto = d.Data;
				var habitId = HabitLogDocumentId.ResolveHabitId(d.Id, dto.HabitId);
				var date = HabitLogDocumentId.TryParse(d.Id, out var fromId, out _)
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
}
