namespace OneTapHabits.Services;

public static class UserFriendlyErrorMapper
{
	public static string FromException(Exception ex, ILocalizationService localization) =>
		localization.Get(ErrorMessageKeyResolver.Resolve(ex));
}
