namespace OneTapHabits.WidgetHost;

internal static class WidgetRefreshWatcher
{
	public const string SignalFileName = "widget_refresh.signal";

	private static FileSystemWatcher? _signalWatcher;
	private static int _refreshScheduled;

	public static string GetSignalFilePath() =>
		Path.Combine(WidgetSnapshotFileStore.GetAppDataDirectory(), SignalFileName);

	public static void Start()
	{
		var directory = WidgetSnapshotFileStore.GetAppDataDirectory();
		Directory.CreateDirectory(directory);

		_signalWatcher = new FileSystemWatcher(directory, SignalFileName)
		{
			NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
			EnableRaisingEvents = true
		};

		_signalWatcher.Changed += (_, _) => ScheduleRefresh();
		_signalWatcher.Created += (_, _) => ScheduleRefresh();
	}

	private static void ScheduleRefresh()
	{
		if (Interlocked.Exchange(ref _refreshScheduled, 1) == 1)
		{
			return;
		}

		_ = Task.Run(async () =>
		{
			await Task.Delay(50);
			Interlocked.Exchange(ref _refreshScheduled, 0);
			HabitsWidgetProvider.RefreshAllWidgets();
		});
	}
}
