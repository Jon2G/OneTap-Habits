using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.ApplicationModel;
using OneTapHabits.Services;
using OneTapHabits.Services.Widget;
using OneTapHabits.ViewModels;
using OneTapHabits.Views;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Crashlytics;
using Plugin.Firebase.Firestore;

#if ANDROID
using Plugin.Firebase.Core.Platforms.Android;
#elif IOS
using Plugin.Firebase.Core.Platforms.iOS;
#endif

namespace OneTapHabits;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
			})
			.RegisterFirebaseServices();

		builder.Services.AddSingleton<IAuthService, AuthService>();
		builder.Services.AddSingleton<ILocalGuestStore, LocalGuestStore>();
		builder.Services.AddSingleton<IGuestDataSyncService, GuestDataSyncService>();
		builder.Services.AddSingleton<IFirstLaunchSeedService, FirstLaunchSeedService>();
		builder.Services.AddSingleton<IHabitService, HabitService>();
		builder.Services.AddSingleton<ILogService, LogService>();
		builder.Services.AddSingleton<IStreakService, StreakService>();
		builder.Services.AddSingleton<IWeeklyProgressService, WeeklyProgressService>();
		builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
		builder.Services.AddSingleton<IThemeService, ThemeService>();

#if ANDROID
		builder.Services.AddSingleton<IWidgetRefreshService, Platforms.Android.Services.WidgetRefreshService>();
		builder.Services.AddSingleton<IGoogleSignInService, Platforms.Android.Services.AndroidGoogleSignInService>();
#else
		builder.Services.AddSingleton<IWidgetRefreshService, NoOpWidgetRefreshService>();
		builder.Services.AddSingleton<IGoogleSignInService, NoOpGoogleSignInService>();
#endif

		builder.Services.AddTransient<TodayViewModel>();
		builder.Services.AddTransient<CalendarViewModel>();
		builder.Services.AddTransient<HabitFormViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();

		builder.Services.AddTransient<TodayPage>();
		builder.Services.AddTransient<CalendarPage>();
		builder.Services.AddTransient<HabitFormPage>();
		builder.Services.AddTransient<SettingsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
	{
		builder.ConfigureLifecycleEvents(events =>
		{
#if ANDROID
			events.AddAndroid(android => android.OnCreate((activity, _) =>
			{
				CrossFirebase.Initialize(activity, () => Platform.CurrentActivity);
				CrossFirebaseCrashlytics.Current.SetCrashlyticsCollectionEnabled(true);
			}));
#elif IOS
			events.AddiOS(iOS => iOS.WillFinishLaunching((_, _) =>
			{
				CrossFirebase.Initialize();
				return false;
			}));
#endif
		});

		builder.Services.AddSingleton(_ => CrossFirebaseAuth.Current);
		builder.Services.AddSingleton(_ => CrossFirebaseFirestore.Current);
		return builder;
	}
}
