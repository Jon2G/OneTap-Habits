namespace OneTapHabits.Services.Widget;

public static class WidgetProgressFormatter
{
	public static string? FormatProgress(int count, int timesPerDay) =>
		timesPerDay > 1 ? $"{count}/{timesPerDay}" : null;
}
