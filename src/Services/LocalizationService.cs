using System.Globalization;
using OneTapHabits.Resources.Strings;

namespace OneTapHabits.Services;

public sealed class LocalizationService : ILocalizationService
{
	private const string PreferenceKey = "app_culture";

	public IReadOnlyList<(string Code, string Label)> SupportedCultures { get; } =
	[
		("en", "English"),
		("es", "Español")
	];

	public string CurrentCultureName => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

	public LocalizationService()
	{
		var saved = Preferences.Default.Get(PreferenceKey, string.Empty);
		if (!string.IsNullOrWhiteSpace(saved))
		{
			ApplyCulture(saved);
		}
	}

	public void SetCulture(string cultureName)
	{
		Preferences.Default.Set(PreferenceKey, cultureName);
		ApplyCulture(cultureName);
	}

	public string Get(string key) => AppResources.Get(key);

	private static void ApplyCulture(string cultureName)
	{
		var culture = new CultureInfo(cultureName);
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
	}
}
