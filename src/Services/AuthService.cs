using Plugin.Firebase.Auth;

namespace OneTapHabits.Services;

public sealed class AuthService : IAuthService
{
	private readonly IFirebaseAuth _firebaseAuth;
	private readonly IGoogleSignInService _googleSignInService;
	private readonly IGuestDataSyncService _guestDataSyncService;
	private readonly ILocalLogOverlayStore _logOverlay;

	public AuthService(
		IFirebaseAuth firebaseAuth,
		IGoogleSignInService googleSignInService,
		IGuestDataSyncService guestDataSyncService,
		ILocalLogOverlayStore logOverlay)
	{
		_firebaseAuth = firebaseAuth;
		_googleSignInService = googleSignInService;
		_guestDataSyncService = guestDataSyncService;
		_logOverlay = logOverlay;
	}

	public bool IsSignedIn => _firebaseAuth.CurrentUser is not null;

	public string? UserId => _firebaseAuth.CurrentUser?.Uid;

	public string? UserEmail => _firebaseAuth.CurrentUser?.Email;

	public async Task<SignInConflictInfo> SignInWithGoogleAsync()
	{
		await _googleSignInService.AuthenticateAsync();
		return await _guestDataSyncService.EvaluateSignInConflictAsync();
	}

	public Task CompleteSignInAsync(SignInDataResolution resolution) =>
		_guestDataSyncService.ApplySignInResolutionAsync(resolution);

	public Task AbortSignInAsync() => _firebaseAuth.SignOutAsync();

	public async Task SignOutToGuestAsync()
	{
		if (IsSignedIn)
		{
			var userId = UserId;
			await _guestDataSyncService.DownloadCloudDataToGuestAsync();
			if (!string.IsNullOrEmpty(userId))
			{
				await _logOverlay.ClearForUserAsync(userId);
			}

			await _firebaseAuth.SignOutAsync();
		}
	}
}
