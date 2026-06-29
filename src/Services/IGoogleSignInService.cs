namespace OneTapHabits.Services;

public interface IGoogleSignInService
{
	bool IsSupported { get; }

	Task AuthenticateAsync(CancellationToken cancellationToken = default);
}
