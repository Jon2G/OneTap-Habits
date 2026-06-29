using System.Globalization;
using System.Text;

namespace OneTapHabits.Services.Widget;

public static class HabitNameDisplayHelper
{
	public const float WidgetDefaultNameTextSizeSp = 12f;
	public const float WidgetSingleEmojiTextSizeSp = 28f;

	public static bool IsSingleEmoji(string? name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return false;
		}

		var trimmed = name.Trim();
		if (new StringInfo(trimmed).LengthInTextElements != 1)
		{
			return false;
		}

		var grapheme = new StringInfo(trimmed).SubstringByTextElements(0, 1);
		return IsEmojiOnlyGrapheme(grapheme);
	}

	private static bool IsEmojiOnlyGrapheme(string grapheme)
	{
		var hasPrimaryEmoji = false;

		foreach (var rune in grapheme.EnumerateRunes())
		{
			if (IsEmojiJoinerOrModifier(rune))
			{
				continue;
			}

			if (IsRegionalIndicator(rune) || IsKeycapBase(rune) || IsEmojiPresentationRune(rune))
			{
				hasPrimaryEmoji = true;
				continue;
			}

			return false;
		}

		return hasPrimaryEmoji;
	}

	private static bool IsEmojiJoinerOrModifier(Rune rune) =>
		rune.Value is 0xFE0F or 0x200D or 0x20E3
		or >= 0x1F3FB and <= 0x1F3FF;

	private static bool IsRegionalIndicator(Rune rune) =>
		rune.Value is >= 0x1F1E6 and <= 0x1F1FF;

	private static bool IsKeycapBase(Rune rune) =>
		rune.Value is >= 0x30 and <= 0x39 or 0x23 or 0x2A;

	private static bool IsEmojiPresentationRune(Rune rune)
	{
		var value = rune.Value;
		if (value is >= 0x1F300 and <= 0x1FAFF)
		{
			return true;
		}

		if (value is >= 0x2600 and <= 0x26FF)
		{
			return true;
		}

		if (value is >= 0x2700 and <= 0x27BF)
		{
			return value is not (0x2713 or 0x2714);
		}

		return value is >= 0x2300 and <= 0x23FF or >= 0x2B50 and <= 0x2B55;
	}
}
