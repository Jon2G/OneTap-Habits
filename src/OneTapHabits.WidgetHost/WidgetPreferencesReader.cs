using OneTapHabits.Services.Widget;

namespace OneTapHabits.WidgetHost;

internal static class WidgetPreferencesReader
{
	public static int GetTintPercent()
	{
		try
		{
			var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
			if (localSettings.Values.TryGetValue(WidgetAppearanceHelper.TintPreferenceKey, out var value))
			{
				return Convert.ToInt32(value);
			}
		}
		catch
		{
			// Fall back to default tint.
		}

		return WidgetAppearanceHelper.DefaultTintPercent;
	}
}
