namespace OneTapHabits.Services.Firebase;

public sealed class FirestoreDocument<T>
{
	public required string Id { get; init; }

	public required T Data { get; init; }
}

public interface IFirestoreGateway
{
	Task<IReadOnlyList<FirestoreDocument<T>>> GetDocumentsAsync<T>(
		string collectionPath,
		CancellationToken cancellationToken = default)
		where T : class, new();

	Task SetDocumentAsync<T>(
		string documentPath,
		T data,
		CancellationToken cancellationToken = default)
		where T : class;

	Task DeleteDocumentAsync(
		string documentPath,
		CancellationToken cancellationToken = default);
}
