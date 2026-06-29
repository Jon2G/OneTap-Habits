namespace OneTapHabits.Services.Widget;

public enum WidgetTintStrength
{
	Subtle = 15,
	Medium = 25,
	Strong = 40
}

public static class WidgetAppearanceHelper
{
	public const string TintPreferenceKey = "widget_tint_percent";
	public const int DefaultTintPercent = (int)WidgetTintStrength.Medium;
	public const string BaseCellColorHex = "#2A2A2A";

	public static string BlendCellBackground(string habitColorHex, int tintPercent)
	{
		var weight = Math.Clamp(tintPercent, 0, 100) / 100d;
		var habitHex = HabitColorPalette.Normalize(habitColorHex);
		var baseRgb = ParseRgb(BaseCellColorHex);
		var habitRgb = ParseRgb(habitHex);

		var r = (int)Math.Round(baseRgb.R + (habitRgb.R - baseRgb.R) * weight);
		var g = (int)Math.Round(baseRgb.G + (habitRgb.G - baseRgb.G) * weight);
		var b = (int)Math.Round(baseRgb.B + (habitRgb.B - baseRgb.B) * weight);

		return $"#{r:X2}{g:X2}{b:X2}";
	}

	public static WidgetTintStrength ParseTintStrength(int percent) => percent switch
	{
		(int)WidgetTintStrength.Subtle => WidgetTintStrength.Subtle,
		(int)WidgetTintStrength.Strong => WidgetTintStrength.Strong,
		_ => WidgetTintStrength.Medium
	};

	private static (int R, int G, int B) ParseRgb(string hex)
	{
		var candidate = hex.Trim();
		if (!candidate.StartsWith('#'))
		{
			candidate = $"#{candidate}";
		}

		if (candidate.Length != 7)
		{
			return (0x2A, 0x2A, 0x2A);
		}

		var r = Convert.ToInt32(candidate[1..3], 16);
		var g = Convert.ToInt32(candidate[3..5], 16);
		var b = Convert.ToInt32(candidate[5..7], 16);
		return (r, g, b);
	}
}
