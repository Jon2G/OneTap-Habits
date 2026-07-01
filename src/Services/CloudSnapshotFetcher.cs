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
		var logsSnapshot = await LogsCollection(firestore, userId).GetDocumentsAsync<Dictionary<string, object>>();
		return logsSnapshot.Documents
			.Where(d => d.Data is not null && !CloudDocumentSanitizer.ShouldDeleteLogDocument(d.Reference.Id, d.Data))
			.Select(d =>
			{
				var dto = MapLogDto(d.Data!);
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

	private static LogFirestoreDto MapLogDto(IReadOnlyDictionary<string, object> data) => new()
	{
		HabitId = GetString(data, CloudDocumentSanitizer.LogHabitIdField),
		Date = GetString(data, "date"),
		IsCompleted = GetBool(data, CloudDocumentSanitizer.LogIsCompletedField),
		Count = GetInt(data, CloudDocumentSanitizer.LogCountField)
	};

	private static string GetString(IReadOnlyDictionary<string, object> data, string key) =>
		data.TryGetValue(key, out var value) && value is not null ? value.ToString() ?? string.Empty : string.Empty;

	private static bool GetBool(IReadOnlyDictionary<string, object> data, string key) =>
		data.TryGetValue(key, out var value) && value is bool b && b;

	private static int GetInt(IReadOnlyDictionary<string, object> data, string key)
	{
		if (!data.TryGetValue(key, out var value) || value is null)
		{
			return 0;
		}

		return value switch
		{
			int i => i,
			long l => (int)l,
			double d => (int)d,
			_ => int.TryParse(value.ToString(), out var parsed) ? parsed : 0
		};
	}

	private static ICollectionReference HabitsCollection(IFirebaseFirestore firestore, string userId) =>
		firestore.GetCollection($"users/{userId}/habits");

	private static ICollectionReference LogsCollection(IFirebaseFirestore firestore, string userId) =>
		firestore.GetCollection($"users/{userId}/logs");
}
