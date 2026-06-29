using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui.ApplicationModel;
using OneTapHabits.Services.Widget;

namespace OneTapHabits;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnResume()
	{
		base.OnResume();

		var widgetRefresh = IPlatformApplication.Current?.Services.GetService<IWidgetRefreshService>();
		if (widgetRefresh is not null)
		{
			_ = widgetRefresh.RefreshAsync();
		}
	}
}
