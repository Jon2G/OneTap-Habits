using Android.Gms.Extensions;
using Firebase.Auth;
using Microsoft.Maui.ApplicationModel;
using OneTapHabits.Services;

namespace OneTapHabits.Platforms.Android.Services;

public sealed class AndroidGoogleSignInService : IGoogleSignInService
{
	private readonly IDiagnosticLogService _diagnosticLog;

	public AndroidGoogleSignInService(IDiagnosticLogService diagnosticLog)
	{
		_diagnosticLog = diagnosticLog;
	}

	public bool IsSupported => true;

	public async Task AuthenticateAsync(CancellationToken cancellationToken = default)
	{
		var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("No active Android activity.");
		var context = activity.ApplicationContext ?? throw new InvalidOperationException("Android context unavailable.");

		if (string.IsNullOrWhiteSpace(GoogleServicesOAuthReader.TryGetWebClientId(context)))
		{
			_diagnosticLog.LogWarning("GoogleSignIn", "Web client id missing from google-services.json.");
			throw new InvalidOperationException(
				"Google Sign-In is not configured. Enable Google in Firebase Console, add SHA-1 fingerprints, and re-download google-services.json.");
		}

		cancellationToken.ThrowIfCancellationRequested();

		var apkSha1 = ApkSigningCertificateHelper.TryGetSha1Fingerprint(context);
		var registeredHashes = ApkSigningCertificateHelper.CountRegisteredAndroidOAuthHashes(context);
		_diagnosticLog.LogInfo(
			"GoogleSignIn",
			$"APK SHA-1={apkSha1 ?? "(unknown)"}; google-services.json android oauth hashes={registeredHashes}");

		if (!string.IsNullOrWhiteSpace(apkSha1)
		    && !ApkSigningCertificateHelper.IsSha1RegisteredInGoogleServices(context, apkSha1))
		{
			_diagnosticLog.LogWarning(
				"GoogleSignIn",
				$"APK SHA-1 {apkSha1} is not in google-services.json — add it in Firebase and re-download config.");
		}

		_diagnosticLog.LogInfo("GoogleSignIn", "Launching Firebase Google provider activity.");

		var provider = OAuthProvider.NewBuilder("google.com").Build();
		var authResult = await FirebaseAuth.Instance
			.StartActivityForSignInWithProvider(activity, provider)
			.AsAsync<IAuthResult>();

		cancellationToken.ThrowIfCancellationRequested();

		if (authResult?.User is null)
		{
			_diagnosticLog.LogWarning("GoogleSignIn", "Firebase returned null user after provider sign-in.");
			throw new InvalidOperationException("Google Sign-In did not return a Firebase user.");
		}

		_diagnosticLog.LogInfo("GoogleSignIn", $"Firebase user authenticated uid={MaskUserId(authResult.User.Uid)}");
	}

	private static string MaskUserId(string userId) =>
		userId.Length <= 8 ? $"{userId}..." : $"{userId[..8]}...";
}
