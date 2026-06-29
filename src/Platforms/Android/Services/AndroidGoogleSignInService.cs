using Android.Gms.Extensions;
using Firebase.Auth;
using Microsoft.Maui.ApplicationModel;
using OneTapHabits.Services;

namespace OneTapHabits.Platforms.Android.Services;

public sealed class AndroidGoogleSignInService : IGoogleSignInService
{
	public bool IsSupported => true;

	public async Task AuthenticateAsync(CancellationToken cancellationToken = default)
	{
		var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("No active Android activity.");
		var context = activity.ApplicationContext ?? throw new InvalidOperationException("Android context unavailable.");

		if (string.IsNullOrWhiteSpace(GoogleServicesOAuthReader.TryGetWebClientId(context)))
		{
			throw new InvalidOperationException(
				"Google Sign-In is not configured. Enable Google in Firebase Console, add SHA-1 fingerprints, and re-download google-services.json.");
		}

		cancellationToken.ThrowIfCancellationRequested();

		var provider = OAuthProvider.NewBuilder("google.com").Build();
		var authResult = await FirebaseAuth.Instance
			.StartActivityForSignInWithProvider(activity, provider)
			.AsAsync<IAuthResult>();

		cancellationToken.ThrowIfCancellationRequested();

		if (authResult?.User is null)
		{
			throw new InvalidOperationException("Google Sign-In did not return a Firebase user.");
		}
	}
}
