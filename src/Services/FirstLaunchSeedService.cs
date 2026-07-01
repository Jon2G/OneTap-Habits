using OneTapHabits.Models;

namespace OneTapHabits.Services;

public sealed class FirstLaunchSeedService : IFirstLaunchSeedService
{
	private const string PreferenceKey = "sample_habits_seeded_v1";

	private readonly IAuthService _authService;
	private readonly ILocalGuestStore _guestStore;
	private readonly ILocalizationService _localization;

	public FirstLaunchSeedService(
		IAuthService authService,
		ILocalGuestStore guestStore,
		ILocalizationService localization)
	{
		_authService = authService;
		_guestStore = guestStore;
		_localization = localization;
	}

	public async Task SeedIfNeededAsync()
	{
		if (Preferences.Default.Get(PreferenceKey, false) || !_authService.IsGuest)
		{
			return;
		}

		var guest = await _guestStore.LoadAsync();
		if (guest.Habits.Count > 0)
		{
			Preferences.Default.Set(PreferenceKey, true);
			return;
		}

		var samples = SampleHabitSeed.Create(
			_localization.Get("SampleHabitGym"),
			_localization.Get("SampleHabitMedication"));

		guest.Habits.AddRange(samples);
		await _guestStore.SaveAsync(guest);
		Preferences.Default.Set(PreferenceKey, true);
		Preferences.Default.Set(
			SignInGuestDataHelper.SampleHabitIdsPreferenceKey,
			SignInGuestDataHelper.FormatSampleHabitIds(samples.Select(s => s.Id)));
	}
}
