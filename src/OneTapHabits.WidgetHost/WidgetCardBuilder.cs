using System.Text.Json;
using System.Text.Json.Nodes;
using OneTapHabits.Models;
using OneTapHabits.Services.Widget;
using OneTapHabits.Widget;

namespace OneTapHabits.WidgetHost;

internal static class WidgetCardBuilder
{
	private const string FallbackTemplate = """
		{
		  "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
		  "type": "AdaptiveCard",
		  "version": "1.5",
		  "body": [
		    {
		      "type": "TextBlock",
		      "text": "${title}",
		      "weight": "Bolder",
		      "wrap": true
		    },
		    {
		      "type": "TextBlock",
		      "text": "${subtitle}",
		      "isSubtle": true,
		      "wrap": true,
		      "spacing": "Small"
		    }
		  ],
		  "actions": [
		    {
		      "type": "Action.Execute",
		      "title": "Open app",
		      "verb": "open_app"
		    }
		  ]
		}
		""";

	public static string LoadTemplate()
	{
		var path = Path.Combine(AppContext.BaseDirectory, "Templates", "HabitsTemplate.json");
		if (File.Exists(path))
		{
			return File.ReadAllText(path);
		}

		return FallbackTemplate;
	}

	public static string BuildData(WidgetSnapshot snapshot, WidgetTapAnimationState? animation)
	{
		if (!snapshot.IsSignedIn)
		{
			return SerializeData(new Dictionary<string, object?>
			{
				["mode"] = "signin",
				["title"] = "OneTap Habits",
				["subtitle"] = "Open the app to get started."
			});
		}

		if (snapshot.Habits.Count == 0)
		{
			var subtitle = snapshot.OverflowCount > 0
				? $"{snapshot.OverflowCount} more habits done today."
				: "All habits done for today.";
			return SerializeData(new Dictionary<string, object?>
			{
				["mode"] = "empty",
				["title"] = "OneTap Habits",
				["subtitle"] = subtitle
			});
		}

		var layout = WidgetGridLayoutHelper.GetLayout(snapshot.Habits.Count);
		var data = new Dictionary<string, object?>
		{
			["mode"] = "grid",
			["title"] = "Today's habits",
			["subtitle"] = string.Empty,
			["show_row0"] = layout.ShowRow0,
			["show_row1"] = layout.ShowRow1,
			["show_row2"] = layout.ShowRow2,
			["show_overflow"] = snapshot.OverflowCount > 0,
			["overflow"] = $"+{snapshot.OverflowCount} more in app"
		};

		for (var i = 0; i < 6; i++)
		{
			var prefix = $"cell{i}";
			var visible = layout.VisibleCells[i] && i < snapshot.Habits.Count;
			data[$"{prefix}_visible"] = visible;
			if (!visible)
			{
				data[$"{prefix}_id"] = string.Empty;
				data[$"{prefix}_name"] = string.Empty;
				data[$"{prefix}_progress"] = string.Empty;
				data[$"{prefix}_show_progress"] = false;
				data[$"{prefix}_show_decrement"] = false;
				continue;
			}

			var habit = snapshot.Habits[i];
			var isAnimating = animation is not null && animation.CellIndex == i;
			var progress = WidgetProgressFormatter.FormatProgress(habit.Count, habit.TimesPerDay);

			data[$"{prefix}_id"] = habit.Id;
			data[$"{prefix}_name"] = isAnimating
				? GetAnimationText(animation!.Kind)
				: habit.Name;
			data[$"{prefix}_progress"] = progress ?? string.Empty;
			data[$"{prefix}_show_progress"] = !isAnimating && progress is not null;
			data[$"{prefix}_show_decrement"] = !isAnimating && progress is not null && habit.Count > 0;
		}

		return SerializeData(data);
	}

	public static string BuildFallbackData(string subtitle) =>
		SerializeData(new Dictionary<string, object?>
		{
			["mode"] = "signin",
			["title"] = "OneTap Habits",
			["subtitle"] = subtitle
		});

	private static string SerializeData(Dictionary<string, object?> data) =>
		JsonSerializer.Serialize(data, new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = false
		});

	private static string GetAnimationText(WidgetTapAnimationKind kind) => kind switch
	{
		WidgetTapAnimationKind.Complete => "✓",
		WidgetTapAnimationKind.MinusOne => "-1",
		_ => "+1"
	};
}
