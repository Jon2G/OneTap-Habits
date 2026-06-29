using Android.App;
using Android.Runtime;
using OneTapHabits.Platforms.Android.Services;

namespace OneTapHabits;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(nint handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	public override void OnCreate()
	{
		FirebaseAndroidBootstrap.EnsureInitialized(this);
		base.OnCreate();
	}
}
