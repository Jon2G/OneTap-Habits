using OneTapHabits.Resources.Strings;
using OneTapHabits.Services.Widget;

namespace OneTapHabits.Services;

public sealed class WidgetAppearanceService : IWidgetAppearanceService
{
	public WidgetTintStrength CurrentTint { get; private set; } = WidgetTintStrength.Medium;

	public WidgetAppearanceService()
	{
		ApplySavedTint();
	}

	public void SetTint(WidgetTintStrength tint)
	{
		CurrentTint = tint;
		Preferences.Default.Set(WidgetAppearanceHelper.TintPreferenceKey, (int)tint);
	}

	public void ApplySavedTint()
	{
		var saved = Preferences.Default.Get(WidgetAppearanceHelper.TintPreferenceKey, WidgetAppearanceHelper.DefaultTintPercent);
		CurrentTint = WidgetAppearanceHelper.ParseTintStrength(saved);
	}

	public int GetTintPercent() => (int)CurrentTint;

	public string GetLabel(WidgetTintStrength tint) => tint switch
	{
		WidgetTintStrength.Subtle => AppResources.Get("WidgetTintSubtle"),
		WidgetTintStrength.Strong => AppResources.Get("WidgetTintStrong"),
		_ => AppResources.Get("WidgetTintMedium")
	};
}
