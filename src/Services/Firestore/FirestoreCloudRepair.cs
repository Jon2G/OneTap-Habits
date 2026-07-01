using OneTapHabits.Firestore;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Services.Firestore;

public static class FirestoreCloudRepair
{
	public static async Task<(int HabitsDeleted, int LogsDeleted)> SanitizeCorruptDocumentsAsync(
		ICollectionReference habitsCollection,
		ICollectionReference logsCollection,
		CancellationToken cancellationToken = default)
	{
		var habitsDeleted = await SanitizeCollectionAsync(
			habitsCollection,
			CloudDocumentSanitizer.ShouldDeleteHabitDocument,
			cancellationToken);
		var logsDeleted = await SanitizeCollectionAsync(
			logsCollection,
			(id, data) => CloudDocumentSanitizer.ShouldDeleteLogDocument(id, data),
			cancellationToken);
		return (habitsDeleted, logsDeleted);
	}

	private static async Task<int> SanitizeCollectionAsync(
		ICollectionReference collection,
		Func<IReadOnlyDictionary<string, object>?, bool> shouldDelete,
		CancellationToken cancellationToken)
	{
		var deleted = 0;
		var snapshot = await collection.GetDocumentsAsync<Dictionary<string, object>>();
		foreach (var document in snapshot.Documents)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (!shouldDelete(document.Data))
			{
				continue;
			}

			await document.Reference.DeleteDocumentAsync();
			deleted++;
		}

		return deleted;
	}

	private static async Task<int> SanitizeCollectionAsync(
		ICollectionReference collection,
		Func<string, IReadOnlyDictionary<string, object>?, bool> shouldDelete,
		CancellationToken cancellationToken)
	{
		var deleted = 0;
		var snapshot = await collection.GetDocumentsAsync<Dictionary<string, object>>();
		foreach (var document in snapshot.Documents)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (!shouldDelete(document.Reference.Id, document.Data))
			{
				continue;
			}

			await document.Reference.DeleteDocumentAsync();
			deleted++;
		}

		return deleted;
	}
}
