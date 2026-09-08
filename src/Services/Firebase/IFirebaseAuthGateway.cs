namespace OneTapHabits.Services.Firebase;

public sealed class FirebaseUserInfo
{
	public required string Uid { get; init; }

	public string? Email { get; init; }
}

public interface IFirebaseAuthGateway
{
	FirebaseUserInfo? CurrentUser { get; }

	Task SignOutAsync(CancellationToken cancellationToken = default);
}
