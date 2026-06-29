namespace OneTapHabits.Services;

public sealed class NoOpGoogleSignInService : IGoogleSignInService
{
	public bool IsSupported => false;

	public Task AuthenticateAsync(CancellationToken cancellationToken = default) =>
		Task.FromException(new NotSupportedException("Google Sign-In is only available on Android."));
}
