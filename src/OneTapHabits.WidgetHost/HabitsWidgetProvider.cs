using System.Runtime.InteropServices.WindowsRuntime;
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
				RegisterWidget(widgetInfo.WidgetContext.Id, widgetInfo.WidgetContext.DefinitionId);
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
			var widget = RegisterWidget(widgetContext.Id, widgetContext.DefinitionId);
			UpdateWidget(widget);
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
			var widget = RegisterWidget(
				contextChangedArgs.WidgetContext.Id,
				contextChangedArgs.WidgetContext.DefinitionId);
			UpdateWidget(widget);
		}
	}

	public void Activate(WidgetContext widgetContext)
	{
		lock (Gate)
		{
			var widget = RegisterWidget(widgetContext.Id, widgetContext.DefinitionId);
			widget.IsActive = true;
			UpdateWidget(widget);
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

	private static RunningWidgetInfo RegisterWidget(string widgetId, string definitionId)
	{
		if (RunningWidgets.TryGetValue(widgetId, out var existing))
		{
			return existing;
		}

		var widget = new RunningWidgetInfo
		{
			WidgetId = widgetId,
			DefinitionId = definitionId
		};
		RunningWidgets[widgetId] = widget;
		return widget;
	}

	private static void UpdateWidget(RunningWidgetInfo widget)
	{
		try
		{
			var snapshot = WidgetSnapshotFileStore.Load();
			var animation = WidgetTapAnimationFileStore.Load();
			var updateOptions = new WidgetUpdateRequestOptions(widget.WidgetId)
			{
				Template = WidgetCardBuilder.LoadTemplate(),
				Data = WidgetCardBuilder.BuildData(snapshot, animation)
			};

			WidgetManager.GetDefault().UpdateWidget(updateOptions);
		}
		catch
		{
			var updateOptions = new WidgetUpdateRequestOptions(widget.WidgetId)
			{
				Template = WidgetCardBuilder.LoadTemplate(),
				Data = WidgetCardBuilder.BuildFallbackData("Open the app to refresh the widget.")
			};

			WidgetManager.GetDefault().UpdateWidget(updateOptions);
		}
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
