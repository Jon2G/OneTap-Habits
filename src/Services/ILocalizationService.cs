namespace OneTapHabits.Services;

public interface ILocalizationService
{
	string CurrentCultureName { get; }
	IReadOnlyList<(string Code, string Label)> SupportedCultures { get; }
	void SetCulture(string cultureName);
	string Get(string key);
}
