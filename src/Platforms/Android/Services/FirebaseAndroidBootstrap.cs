using Android.App;
using Android.Content;
using Firebase;
using Microsoft.Maui.ApplicationModel;
using Plugin.Firebase.Core.Platforms.Android;

namespace OneTapHabits.Platforms.Android.Services;

public static class FirebaseAndroidBootstrap
{
	private static bool _initialized;

	public static void EnsureInitialized(Context context)
	{
		if (_initialized)
		{
			return;
		}

		var appContext = context.ApplicationContext ?? context;

		if (FirebaseApp.GetApps(appContext)?.Count == 0)
		{
			FirebaseApp.InitializeApp(appContext);
		}

		if (Platform.CurrentActivity is Activity activity)
		{
			CrossFirebase.Initialize(activity, () => Platform.CurrentActivity ?? activity);
		}

		_initialized = true;
	}
}
