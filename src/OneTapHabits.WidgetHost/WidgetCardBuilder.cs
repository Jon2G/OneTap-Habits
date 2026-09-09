using System.Text.Json;
using System.Text.Json.Nodes;
using OneTapHabits.Models;
using OneTapHabits.Services.Widget;
using OneTapHabits.Widget;

namespace OneTapHabits.WidgetHost;

internal static class WidgetCardBuilder
{
	private const string CompleteCellBackgroundHex = "#1A3D2E";

	public static string BuildCard(WidgetSnapshot snapshot, WidgetTapAnimationState? animation, int tintPercent)
	{
		var card = new JsonObject
		{
			["type"] = "AdaptiveCard",
			["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
			["version"] = "1.6",
			["body"] = BuildBody(snapshot, animation, tintPercent)
		};

		return card.ToJsonString();
	}

	private static JsonArray BuildBody(WidgetSnapshot snapshot, WidgetTapAnimationState? animation, int tintPercent)
	{
		var body = new JsonArray();

		if (!snapshot.IsSignedIn)
		{
			body.Add(CreateMessageContainer(
				"OneTap Habits",
				"Open the app to get started.",
				withOpenAppAction: true));
			return body;
		}

		if (snapshot.Habits.Count == 0)
		{
			var subtitle = snapshot.OverflowCount > 0
				? $"{snapshot.OverflowCount} more habits done today."
				: "All habits done for today.";
			body.Add(CreateMessageContainer("OneTap Habits", subtitle, withOpenAppAction: true));
			return body;
		}

		var layout = WidgetGridLayoutHelper.GetLayout(snapshot.Habits.Count);
		AddRow(body, snapshot, animation, tintPercent, layout, 0, 1, layout.ShowRow0);
		AddRow(body, snapshot, animation, tintPercent, layout, 2, 3, layout.ShowRow1);
		AddRow(body, snapshot, animation, tintPercent, layout, 4, 5, layout.ShowRow2);

		if (snapshot.OverflowCount > 0)
		{
			body.Add(new JsonObject
			{
				["type"] = "TextBlock",
				["text"] = $"+{snapshot.OverflowCount} more in app",
				["isSubtle"] = true,
				["size"] = "Small",
				["spacing"] = "Small",
				["wrap"] = true,
				["selectAction"] = CreateOpenAppAction()
			});
		}

		return body;
	}

	private static void AddRow(
		JsonArray body,
		WidgetSnapshot snapshot,
		WidgetTapAnimationState? animation,
		int tintPercent,
		WidgetGridLayoutHelper.GridLayoutConfig layout,
		int cellA,
		int cellB,
		bool showRow)
	{
		if (!showRow)
		{
			return;
		}

		var columns = new JsonArray();
		if (layout.VisibleCells[cellA])
		{
			columns.Add(BuildHabitColumn(snapshot.Habits[cellA], cellA, animation, tintPercent));
		}

		if (layout.VisibleCells[cellB])
		{
			columns.Add(BuildHabitColumn(snapshot.Habits[cellB], cellB, animation, tintPercent));
		}

		if (columns.Count == 0)
		{
			return;
		}

		body.Add(new JsonObject
		{
			["type"] = "ColumnSet",
			["spacing"] = "Small",
			["columns"] = columns
		});
	}

	private static JsonObject BuildHabitColumn(
		WidgetHabitEntry habit,
		int cellIndex,
		WidgetTapAnimationState? animation,
		int tintPercent)
	{
		var isAnimating = animation is not null && animation.CellIndex == cellIndex;
		var items = new JsonArray();

		if (isAnimating)
		{
			items.Add(new JsonObject
			{
				["type"] = "TextBlock",
				["text"] = GetAnimationText(animation!.Kind),
				["weight"] = "Bolder",
				["size"] = animation.Kind == WidgetTapAnimationKind.Complete ? "Large" : "Medium",
				["color"] = "Good",
				["horizontalAlignment"] = "Center",
				["wrap"] = false,
				["maxLines"] = 1
			});
		}
		else
		{
			var nameSize = HabitNameDisplayHelper.IsSingleEmoji(habit.Name) ? "ExtraLarge" : "Small";
			items.Add(new JsonObject
			{
				["type"] = "TextBlock",
				["text"] = habit.Name,
				["weight"] = "Bolder",
				["size"] = nameSize,
				["wrap"] = true,
				["maxLines"] = 2
			});

			var progress = WidgetProgressFormatter.FormatProgress(habit.Count, habit.TimesPerDay);
			if (progress is not null && habit.Count > 0)
			{
				items.Add(new JsonObject
				{
					["type"] = "TextBlock",
					["text"] = progress,
					["size"] = "Small",
					["isSubtle"] = true,
					["spacing"] = "None",
					["selectAction"] = CreateHabitAction("decrement_habit", habit.Id)
				});
			}
			else if (progress is not null)
			{
				items.Add(new JsonObject
				{
					["type"] = "TextBlock",
					["text"] = progress,
					["size"] = "Small",
					["isSubtle"] = true,
					["spacing"] = "None"
				});
			}
		}

		var backgroundHex = isAnimating
			? CompleteCellBackgroundHex
			: WidgetAppearanceHelper.BlendCellBackground(habit.ColorHex, tintPercent);

		var container = new JsonObject
		{
			["type"] = "Container",
			["items"] = items,
			["minHeight"] = "52px",
			["style"] = isAnimating ? "good" : "emphasis",
			["backgroundImage"] = CreateSolidBackground(backgroundHex)
		};

		if (!isAnimating)
		{
			container["selectAction"] = CreateHabitAction("increment_habit", habit.Id);
		}

		return new JsonObject
		{
			["type"] = "Column",
			["width"] = "stretch",
			["items"] = new JsonArray { container }
		};
	}

	private static JsonObject CreateMessageContainer(string title, string subtitle, bool withOpenAppAction)
	{
		var container = new JsonObject
		{
			["type"] = "Container",
			["items"] = new JsonArray
			{
				new JsonObject
				{
					["type"] = "TextBlock",
					["text"] = title,
					["weight"] = "Bolder",
					["wrap"] = true
				},
				new JsonObject
				{
					["type"] = "TextBlock",
					["text"] = subtitle,
					["isSubtle"] = true,
					["wrap"] = true,
					["spacing"] = "Small"
				}
			}
		};

		if (withOpenAppAction)
		{
			container["selectAction"] = CreateOpenAppAction();
		}

		return container;
	}

	private static JsonObject CreateHabitAction(string verb, string habitId) => new()
	{
		["type"] = "Action.Execute",
		["verb"] = verb,
		["data"] = new JsonObject { ["habitId"] = habitId }
	};

	private static JsonObject CreateOpenAppAction() => new()
	{
		["type"] = "Action.Execute",
		["verb"] = "open_app"
	};

	private static JsonObject CreateSolidBackground(string colorHex)
	{
		var svg = $"<svg xmlns='http://www.w3.org/2000/svg'><rect fill='{colorHex}' width='100%' height='100%'/></svg>";
		return new JsonObject
		{
			["url"] = $"data:image/svg+xml,{Uri.EscapeDataString(svg)}",
			["fillMode"] = "cover"
		};
	}

	private static string GetAnimationText(WidgetTapAnimationKind kind) => kind switch
	{
		WidgetTapAnimationKind.Complete => "✓",
		WidgetTapAnimationKind.MinusOne => "-1",
		_ => "+1"
	};
}
