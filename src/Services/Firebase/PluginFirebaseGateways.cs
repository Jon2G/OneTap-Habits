#if ANDROID || IOS
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Services.Firebase;

public sealed class PluginFirebaseAuthGateway : IFirebaseAuthGateway
{
	private readonly IFirebaseAuth _auth;

	public PluginFirebaseAuthGateway(IFirebaseAuth auth) => _auth = auth;

	public FirebaseUserInfo? CurrentUser
	{
		get
		{
			var user = _auth.CurrentUser;
			return user is null
				? null
				: new FirebaseUserInfo { Uid = user.Uid, Email = user.Email };
		}
	}

	public Task SignOutAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return _auth.SignOutAsync();
	}
}

public sealed class PluginFirestoreGateway : IFirestoreGateway
{
	private readonly IFirebaseFirestore _firestore;

	public PluginFirestoreGateway(IFirebaseFirestore firestore) => _firestore = firestore;

	public async Task<IReadOnlyList<FirestoreDocument<T>>> GetDocumentsAsync<T>(
		string collectionPath,
		CancellationToken cancellationToken = default)
		where T : class, new()
	{
		cancellationToken.ThrowIfCancellationRequested();
		var snapshot = await _firestore.GetCollection(collectionPath).GetDocumentsAsync<T>();
		return snapshot.Documents
			.Where(d => d.Data is not null)
			.Select(d => new FirestoreDocument<T> { Id = d.Reference.Id, Data = d.Data! })
			.ToList();
	}

	public Task SetDocumentAsync<T>(
		string documentPath,
		T data,
		CancellationToken cancellationToken = default)
		where T : class
	{
		cancellationToken.ThrowIfCancellationRequested();
		return _firestore.GetCollection(GetCollectionPath(documentPath))
			.GetDocument(GetDocumentId(documentPath))
			.SetDataAsync(data);
	}

	public Task DeleteDocumentAsync(string documentPath, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return _firestore.GetCollection(GetCollectionPath(documentPath))
			.GetDocument(GetDocumentId(documentPath))
			.DeleteDocumentAsync();
	}

	private static string GetCollectionPath(string documentPath)
	{
		var lastSlash = documentPath.LastIndexOf('/');
		return lastSlash < 0 ? documentPath : documentPath[..lastSlash];
	}

	private static string GetDocumentId(string documentPath)
	{
		var lastSlash = documentPath.LastIndexOf('/');
		return lastSlash < 0 ? documentPath : documentPath[(lastSlash + 1)..];
	}
}
#endif
