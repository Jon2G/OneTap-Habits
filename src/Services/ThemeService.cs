using OneTapHabits.Resources.Strings;

namespace OneTapHabits.Services;

public sealed class ThemeService : IThemeService
{
	private const string PreferenceKey = "app_theme";

	public ThemePreference CurrentPreference { get; private set; } = ThemePreference.System;

	public ThemeService()
	{
		ApplySavedTheme();
	}

	public void SetTheme(ThemePreference preference)
	{
		CurrentPreference = preference;
		Preferences.Default.Set(PreferenceKey, preference.ToString());
		ApplyToApplication(preference);
	}

	public void ApplySavedTheme()
	{
		var saved = Preferences.Default.Get(PreferenceKey, ThemePreference.System.ToString());
		if (!Enum.TryParse<ThemePreference>(saved, out var preference))
		{
			preference = ThemePreference.System;
		}

		CurrentPreference = preference;
		ApplyToApplication(preference);
	}

	public string GetLabel(ThemePreference preference) => preference switch
	{
		ThemePreference.System => AppResources.Get("ThemeSystem"),
		ThemePreference.Light => AppResources.Get("ThemeLight"),
		ThemePreference.Dark => AppResources.Get("ThemeDark"),
		_ => preference.ToString()
	};

	private static void ApplyToApplication(ThemePreference preference)
	{
		if (Application.Current is null)
		{
			return;
		}

		Application.Current.UserAppTheme = preference switch
		{
			ThemePreference.Light => AppTheme.Light,
			ThemePreference.Dark => AppTheme.Dark,
			_ => AppTheme.Unspecified
		};
	}
}
