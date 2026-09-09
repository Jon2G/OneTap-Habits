using System.Text.Json;
using OneTapHabits.Widget;

namespace OneTapHabits.WidgetHost;

internal static class WidgetTapAnimationFileStore
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public static string GetFilePath() =>
		Path.Combine(WidgetSnapshotFileStore.GetAppDataDirectory(), "widget_tap_animation.json");

	public static void Set(int cellIndex, WidgetTapAnimationKind kind)
	{
		var path = GetFilePath();
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, JsonSerializer.Serialize(new WidgetTapAnimationState
		{
			CellIndex = cellIndex,
			Kind = kind
		}, JsonOptions));
	}

	public static WidgetTapAnimationState? Load()
	{
		var path = GetFilePath();
		if (!File.Exists(path))
		{
			return null;
		}

		try
		{
			var state = JsonSerializer.Deserialize<WidgetTapAnimationState>(File.ReadAllText(path), JsonOptions);
			return state?.Kind == WidgetTapAnimationKind.None ? null : state;
		}
		catch
		{
			return null;
		}
	}

	public static void Clear()
	{
		var path = GetFilePath();
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}
}
