using Plugin.Firebase.Auth;

namespace OneTapHabits.Services;

public sealed class AuthService : IAuthService
{
	private readonly IFirebaseAuth _firebaseAuth;
	private readonly IGoogleSignInService _googleSignInService;
	private readonly IGuestDataSyncService _guestDataSyncService;

	public AuthService(
		IFirebaseAuth firebaseAuth,
		IGoogleSignInService googleSignInService,
		IGuestDataSyncService guestDataSyncService)
	{
		_firebaseAuth = firebaseAuth;
		_googleSignInService = googleSignInService;
		_guestDataSyncService = guestDataSyncService;
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
			await _guestDataSyncService.DownloadCloudDataToGuestAsync();
			await _firebaseAuth.SignOutAsync();
		}
	}
}
