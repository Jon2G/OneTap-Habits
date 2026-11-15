using System.Runtime.InteropServices;
using WinRT;

namespace OneTapHabits.WidgetHost;

internal static class Program
{
	private const string RegisterProcessAsComServer = "-RegisterProcessAsComServer";

	[DllImport("ole32.dll")]
	private static extern int CoRegisterClassObject(
		[MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
		[MarshalAs(UnmanagedType.IUnknown)] object pUnk,
		uint dwClsContext,
		uint flags,
		out uint lpdwRegister);

	[DllImport("ole32.dll")]
	private static extern int CoRevokeClassObject(uint dwRegister);

	private const uint ClsctxLocalServer = 0x4;
	private const uint RegclsMultipleuse = 0x1;

	[MTAThread]
	private static void Main(string[] args)
	{
		// IMPORTANT: do not touch WinRT (ApplicationData / Package) before InitializeComWrappers.
		SafeBootLog("WidgetHost Main starting.");

		try
		{
			if (args.Length == 0 ||
			    !string.Equals(args[0], RegisterProcessAsComServer, StringComparison.OrdinalIgnoreCase))
			{
				SafeBootLog($"Not launched as COM server (args=[{string.Join(' ', args)}]); exiting.");
				return;
			}

			ComWrappersSupport.InitializeComWrappers();
			SafeBootLog("ComWrappers initialized.");

			var clsid = Guid.Parse(WidgetConstants.ProviderClsid);
			var hr = CoRegisterClassObject(
				clsid,
				new WidgetProviderFactory<HabitsWidgetProvider>(),
				ClsctxLocalServer,
				RegclsMultipleuse,
				out var cookie);
			if (hr != 0)
			{
				SafeBootLog($"CoRegisterClassObject failed HR=0x{hr:X8}");
				Marshal.ThrowExceptionForHR(hr);
			}

			SafeBootLog($"COM class registered. CLSID={clsid}");

			try
			{
				WidgetRefreshWatcher.Start();
				SafeBootLog("Refresh watcher started.");
			}
			catch (Exception ex)
			{
				// Must not kill the COM server — updates can still happen via CreateWidget/Activate.
				SafeBootLog($"Refresh watcher failed (continuing): {ex}");
			}

			using var emptyWidgetListEvent = HabitsWidgetProvider.GetEmptyWidgetListEvent();
			SafeBootLog("Waiting for empty widget list / shutdown.");
			emptyWidgetListEvent.WaitOne();
			CoRevokeClassObject(cookie);
			SafeBootLog("WidgetHost shutting down.");
		}
		catch (Exception ex)
		{
			SafeBootLog($"WidgetHost fatal: {ex}");
			throw;
		}
	}

	/// <summary>
	/// File-only logging (no WinRT).
	/// Packaged desktop apps redirect LOCALAPPDATA to Packages\&lt;PFN&gt;\LocalCache\Local —
	/// LocalState is the sibling folder Packages\&lt;PFN&gt;\LocalState.
	/// </summary>
	internal static void SafeBootLog(string message)
	{
		var line = $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}";
		foreach (var path in GetBootLogPaths())
		{
			try
			{
				var dir = Path.GetDirectoryName(path);
				if (!string.IsNullOrEmpty(dir))
				{
					Directory.CreateDirectory(dir);
				}

				File.AppendAllText(path, line);
			}
			catch
			{
				// ignore per-path failures
			}
		}
	}

	private static IEnumerable<string> GetBootLogPaths()
	{
		yield return Path.Combine(Path.GetTempPath(), "onetap-widgethost-boot.log");

		var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
		if (string.IsNullOrWhiteSpace(localAppData))
		{
			yield break;
		}

		yield return Path.Combine(localAppData, "onetap-widgethost-boot.log");

		// Packaged: ...\Packages\<PFN>\LocalCache\Local → LocalState sibling
		const string marker = @"\LocalCache\Local";
		var idx = localAppData.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
		if (idx > 0)
		{
			var packageRoot = localAppData[..idx];
			yield return Path.Combine(packageRoot, "LocalState", "widget_host_boot.log");
			yield return Path.Combine(packageRoot, "TempState", "onetap-widgethost-boot.log");
		}
	}
}
