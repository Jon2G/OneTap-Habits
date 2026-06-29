using OneTapHabits.Services.Widget;

namespace OneTapHabits.Services;

public interface IWidgetAppearanceService
{
	WidgetTintStrength CurrentTint { get; }

	void SetTint(WidgetTintStrength tint);

	void ApplySavedTint();

	int GetTintPercent();

	string GetLabel(WidgetTintStrength tint);
}
