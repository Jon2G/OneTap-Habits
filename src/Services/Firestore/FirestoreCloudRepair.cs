using OneTapHabits.Services;
using OneTapHabits.Services.Firestore;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Services.Firestore;

public static class FirestoreCloudRepair
{
	public static async Task<(int HabitsDeleted, int LogsDeleted)> SanitizeCorruptDocumentsAsync(
		ICollectionReference habitsCollection,
		ICollectionReference logsCollection,
		CancellationToken cancellationToken = default)
	{
		var habitsDeleted = await SanitizeHabitsAsync(habitsCollection, cancellationToken);
		var logsDeleted = await SanitizeLogsAsync(logsCollection, cancellationToken);
		return (habitsDeleted, logsDeleted);
	}

	private static async Task<int> SanitizeHabitsAsync(
		ICollectionReference collection,
		CancellationToken cancellationToken)
	{
		var deleted = 0;
		var snapshot = await collection.GetDocumentsAsync<HabitFirestoreDto>();
		foreach (var document in snapshot.Documents)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (document.Data is not null && document.Data.IsValidCloudDocument())
			{
				continue;
			}

			await document.Reference.DeleteDocumentAsync();
			deleted++;
		}

		return deleted;
	}

	private static async Task<int> SanitizeLogsAsync(
		ICollectionReference collection,
		CancellationToken cancellationToken)
	{
		var deleted = 0;
		var snapshot = await collection.GetDocumentsAsync<LogFirestoreDto>();
		foreach (var document in snapshot.Documents)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (document.Data is not null && CloudSnapshotFetcher.IsValidLogDocument(document.Reference.Id, document.Data))
			{
				continue;
			}

			await document.Reference.DeleteDocumentAsync();
			deleted++;
		}

		return deleted;
	}
}
