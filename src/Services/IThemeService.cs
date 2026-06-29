namespace OneTapHabits.Services;

public enum ThemePreference
{
	System,
	Light,
	Dark
}

public interface IThemeService
{
	ThemePreference CurrentPreference { get; }

	void SetTheme(ThemePreference preference);

	void ApplySavedTheme();

	string GetLabel(ThemePreference preference);
}
