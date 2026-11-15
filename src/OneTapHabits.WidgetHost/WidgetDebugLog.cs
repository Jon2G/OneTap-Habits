namespace OneTapHabits.WidgetHost;

internal static class WidgetDebugLog
{
	private static readonly object Gate = new();
	private static readonly string LogPath = Path.Combine(
		WidgetSnapshotFileStore.GetAppDataDirectory(),
		"widget_debug.log");

	public static bool IsEnabled
	{
		get
		{
			if (string.Equals(Environment.GetEnvironmentVariable("ONETAP_WIDGET_DEBUG"), "1", StringComparison.Ordinal))
			{
				return true;
			}

			try
			{
				// Prefer non-WinRT probe first so early callers never touch ApplicationData.
				var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
				if (!string.IsNullOrWhiteSpace(localAppData))
				{
					var packages = Path.Combine(localAppData, "Packages");
					if (Directory.Exists(packages))
					{
						foreach (var dir in Directory.EnumerateDirectories(packages, "com.jon2g.onetaphabits_*"))
						{
							if (File.Exists(Path.Combine(dir, "LocalState", "widget_debug.enable")))
							{
								return true;
							}
						}
					}
				}

				return File.Exists(Path.Combine(WidgetSnapshotFileStore.GetAppDataDirectory(), "widget_debug.enable"));
			}
			catch
			{
				return false;
			}
		}
	}

	public static void Info(string message) => Write("INFO", message);

	public static void Error(string message, Exception? ex = null)
	{
		var detail = ex is null ? message : $"{message}{Environment.NewLine}{ex}";
		Write("ERROR", detail);
	}

	public static void DumpPayload(string widgetId, string template, string data)
	{
		if (!IsEnabled)
		{
			return;
		}

		try
		{
			var directory = WidgetSnapshotFileStore.GetAppDataDirectory();
			Directory.CreateDirectory(directory);
			File.WriteAllText(Path.Combine(directory, "widget_last_template.json"), template);
			File.WriteAllText(Path.Combine(directory, "widget_last_data.json"), data);
			Info($"Dumped payload for widget {widgetId} ({template.Length} template chars, {data.Length} data chars).");
		}
		catch (Exception ex)
		{
			Error("Failed to dump widget payload.", ex);
		}
	}

	private static void Write(string level, string message)
	{
		if (!IsEnabled)
		{
			return;
		}

		try
		{
			lock (Gate)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
				var line = $"{DateTimeOffset.Now:O} [{level}] {message}{Environment.NewLine}";
				File.AppendAllText(LogPath, line);
			}
		}
		catch
		{
			// Debug logging must never break widget rendering.
		}
	}
}
