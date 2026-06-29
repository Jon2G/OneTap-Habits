namespace OneTapHabits.Services;

public interface IAuthService
{
	bool IsSignedIn { get; }

	bool IsGuest => !IsSignedIn;

	string? UserId { get; }

	string? UserEmail { get; }

	Task SignInWithGoogleAsync();

	Task SignOutToGuestAsync();
}
