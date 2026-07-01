namespace OneTapHabits.Services;

public interface IAuthService
{
	bool IsSignedIn { get; }

	bool IsGuest => !IsSignedIn;

	string? UserId { get; }

	string? UserEmail { get; }

	Task<SignInConflictInfo> SignInWithGoogleAsync();

	Task CompleteSignInAsync(SignInDataResolution resolution);

	Task AbortSignInAsync();

	Task SignOutToGuestAsync();
}
