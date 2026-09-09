using System.Runtime.InteropServices;

namespace OneTapHabits.WidgetHost;

internal static class Program
{
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

	[STAThread]
	private static void Main()
	{
		var clsid = Guid.Parse(WidgetConstants.ProviderClsid);
		CoRegisterClassObject(
			clsid,
			new WidgetProviderFactory<HabitsWidgetProvider>(),
			ClsctxLocalServer,
			RegclsMultipleuse,
			out var cookie);

		WidgetRefreshWatcher.Start();

		using var emptyWidgetListEvent = HabitsWidgetProvider.GetEmptyWidgetListEvent();
		emptyWidgetListEvent.WaitOne();
		CoRevokeClassObject(cookie);
	}
}
