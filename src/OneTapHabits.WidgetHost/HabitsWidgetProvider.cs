using System.Text.Json;
using Microsoft.Windows.Widgets.Providers;

namespace OneTapHabits.WidgetHost;

internal sealed class HabitsWidgetProvider : IWidgetProvider
{
	private static readonly object Gate = new();
	private static readonly ManualResetEvent EmptyWidgetListEvent = new(false);
	private static readonly Dictionary<string, RunningWidgetInfo> RunningWidgets = new();

	public HabitsWidgetProvider()
	{
		lock (Gate)
		{
			foreach (var widgetInfo in WidgetManager.GetDefault().GetWidgetInfos())
			{
				var widgetId = widgetInfo.WidgetContext.Id;
				if (RunningWidgets.ContainsKey(widgetId))
				{
					continue;
				}

				RunningWidgets[widgetId] = new RunningWidgetInfo
				{
					WidgetId = widgetId,
					DefinitionId = widgetInfo.WidgetContext.DefinitionId
				};
			}
		}
	}

	public static ManualResetEvent GetEmptyWidgetListEvent() => EmptyWidgetListEvent;

	public static void RefreshAllWidgets()
	{
		lock (Gate)
		{
			foreach (var widget in RunningWidgets.Values)
			{
				UpdateWidget(widget);
			}
		}
	}

	public void CreateWidget(WidgetContext widgetContext)
	{
		lock (Gate)
		{
			var widgetId = widgetContext.Id;
			RunningWidgets[widgetId] = new RunningWidgetInfo
			{
				WidgetId = widgetId,
				DefinitionId = widgetContext.DefinitionId
			};

			UpdateWidget(RunningWidgets[widgetId]);
		}
	}

	public void DeleteWidget(string widgetId, string customState)
	{
		lock (Gate)
		{
			RunningWidgets.Remove(widgetId);
			if (RunningWidgets.Count == 0)
			{
				EmptyWidgetListEvent.Set();
			}
		}
	}

	public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
	{
		switch (actionInvokedArgs.Verb)
		{
			case "open_app":
				TryLaunchMainApp();
				break;
			case "increment_habit":
				if (TryParseHabitId(actionInvokedArgs.Data, out var incrementHabitId))
				{
					WidgetCompletionService.IncrementHabit(incrementHabitId);
				}
				break;
			case "decrement_habit":
				if (TryParseHabitId(actionInvokedArgs.Data, out var decrementHabitId))
				{
					WidgetCompletionService.DecrementHabit(decrementHabitId);
				}
				break;
		}
	}

	public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
	{
		lock (Gate)
		{
			var widgetId = contextChangedArgs.WidgetContext.Id;
			if (RunningWidgets.TryGetValue(widgetId, out var widget))
			{
				UpdateWidget(widget);
			}
		}
	}

	public void Activate(WidgetContext widgetContext)
	{
		lock (Gate)
		{
			var widgetId = widgetContext.Id;
			if (RunningWidgets.TryGetValue(widgetId, out var widget))
			{
				widget.IsActive = true;
				UpdateWidget(widget);
			}
		}
	}

	public void Deactivate(string widgetId)
	{
		lock (Gate)
		{
			if (RunningWidgets.TryGetValue(widgetId, out var widget))
			{
				widget.IsActive = false;
			}
		}
	}

	private static void UpdateWidget(RunningWidgetInfo widget)
	{
		var snapshot = WidgetSnapshotFileStore.Load();
		var animation = WidgetTapAnimationFileStore.Load();
		var tintPercent = WidgetPreferencesReader.GetTintPercent();
		var card = WidgetCardBuilder.BuildCard(snapshot, animation, tintPercent);

		var updateOptions = new WidgetUpdateRequestOptions(widget.WidgetId)
		{
			Template = card,
			Data = "{}"
		};

		WidgetManager.GetDefault().UpdateWidget(updateOptions);
	}

	private static bool TryParseHabitId(string? data, out string habitId)
	{
		habitId = string.Empty;
		if (string.IsNullOrWhiteSpace(data))
		{
			return false;
		}

		try
		{
			using var document = JsonDocument.Parse(data);
			if (document.RootElement.TryGetProperty("habitId", out var habitIdElement))
			{
				habitId = habitIdElement.GetString() ?? string.Empty;
				return !string.IsNullOrWhiteSpace(habitId);
			}
		}
		catch
		{
			// Ignore malformed action payloads.
		}

		return false;
	}

	private static void TryLaunchMainApp()
	{
		try
		{
			var entries = Windows.ApplicationModel.Package.Current.GetAppListEntriesAsync()
				.AsTask()
				.GetAwaiter()
				.GetResult();
			if (entries.Count > 0)
			{
				Windows.System.Launcher.LaunchAsync(entries[0])
					.AsTask()
					.GetAwaiter()
					.GetResult();
			}
		}
		catch
		{
			// Best-effort only; widget still works without launching.
		}
	}

	private sealed class RunningWidgetInfo
	{
		public required string WidgetId { get; init; }

		public required string DefinitionId { get; init; }

		public bool IsActive { get; set; }
	}
}
