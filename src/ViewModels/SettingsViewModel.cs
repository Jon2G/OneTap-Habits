using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OneTapHabits.Services;
using OneTapHabits.Services.Widget;

namespace OneTapHabits.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	private const string CreatorName = "Jonathan Eduardo García García";
	private const string RepositoryUrl = "https://github.com/Jon2G/OneTap-Habits";

	private readonly IAuthService _authService;
	private readonly ILocalizationService _localization;
	private readonly IWidgetRefreshService _widgetRefresh;
	private readonly IThemeService _themeService;
	private readonly IWidgetAppearanceService _widgetAppearance;
	private readonly UpdateCoordinator _updateCoordinator;

	[ObservableProperty]
	private string? accountMessage;

	[ObservableProperty]
	private string? accountErrorMessage;

	[ObservableProperty]
	private bool isGoogleSignInBusy;

	public SettingsViewModel(
		IAuthService authService,
		ILocalizationService localization,
		IWidgetRefreshService widgetRefresh,
		IThemeService themeService,
		IWidgetAppearanceService widgetAppearance,
		UpdateCoordinator updateCoordinator)
	{
		_authService = authService;
		_localization = localization;
		_widgetRefresh = widgetRefresh;
		_themeService = themeService;
		_widgetAppearance = widgetAppearance;
		_updateCoordinator = updateCoordinator;
	}

	public INavigation? Navigation { get; set; }

	public string Title => _localization.Get("Settings");
	public string AccountSectionTitle => _localization.Get("AccountSection");
	public string LanguageLabel => _localization.Get("Language");
	public string ThemeLabel => _localization.Get("Theme");
	public string LanguageEnglishLabel => _localization.Get("LanguageEnglish");
	public string LanguageSpanishLabel => _localization.Get("LanguageSpanish");
	public string ThemeSystemLabel => _themeService.GetLabel(ThemePreference.System);
	public string ThemeLightLabel => _themeService.GetLabel(ThemePreference.Light);
	public string ThemeDarkLabel => _themeService.GetLabel(ThemePreference.Dark);
	public string SignOutLabel => _localization.Get("SignOut");
	public string GoogleSignInLabel => _localization.Get("GoogleSignIn");
	public string GuestAccountDescription => _localization.Get("GuestAccountDescription");
	public string SignedInAsLabel => _localization.Get("SignedInAs");
	public string AboutSectionTitle => _localization.Get("AboutSection");
	public string AppVersionLabel => $"{_localization.Get("AppTitle")} v{AppInfo.VersionString}";
	public string CreatedByLabel => string.Format(_localization.Get("CreatedBy"), CreatorName);
	public string ViewSourceLabel => _localization.Get("ViewSourceOnGitHub");
	public string LicenseLabel => _localization.Get("MitLicense");
	public string CheckForUpdatesLabel => _localization.Get("CheckForUpdates");
	public bool IsCheckForUpdatesSupported => DeviceInfo.Platform == DevicePlatform.Android;
	public string WidgetSectionTitle => _localization.Get("WidgetSection");
	public string WidgetTintLabel => _localization.Get("WidgetTintLabel");
	public string WidgetTintSubtleLabel => _widgetAppearance.GetLabel(WidgetTintStrength.Subtle);
	public string WidgetTintMediumLabel => _widgetAppearance.GetLabel(WidgetTintStrength.Medium);
	public string WidgetTintStrongLabel => _widgetAppearance.GetLabel(WidgetTintStrength.Strong);
	public bool IsWidgetSettingsSupported => DeviceInfo.Platform == DevicePlatform.Android;

	public bool IsGuest => _authService.IsGuest;
	public bool IsSignedIn => _authService.IsSignedIn;
	public bool IsGoogleSignInSupported => DeviceInfo.Platform == DevicePlatform.Android;
	public bool CanUseGoogleSignIn => IsGoogleSignInSupported && !IsGoogleSignInBusy;
	public bool HasAccountError => !string.IsNullOrWhiteSpace(AccountErrorMessage);
	public string? SignedInEmail => _authService.UserEmail;

	public bool IsEnglishSelected => _localization.CurrentCultureName == "en";
	public bool IsSpanishSelected => _localization.CurrentCultureName == "es";
	public bool IsThemeSystemSelected => _themeService.CurrentPreference == ThemePreference.System;
	public bool IsThemeLightSelected => _themeService.CurrentPreference == ThemePreference.Light;
	public bool IsThemeDarkSelected => _themeService.CurrentPreference == ThemePreference.Dark;
	public bool IsWidgetTintSubtleSelected => _widgetAppearance.CurrentTint == WidgetTintStrength.Subtle;
	public bool IsWidgetTintMediumSelected => _widgetAppearance.CurrentTint == WidgetTintStrength.Medium;
	public bool IsWidgetTintStrongSelected => _widgetAppearance.CurrentTint == WidgetTintStrength.Strong;

	partial void OnIsGoogleSignInBusyChanged(bool value)
	{
		OnPropertyChanged(nameof(CanUseGoogleSignIn));
	}

	partial void OnAccountErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasAccountError));

	public void RefreshChoices() => NotifyChoicePropertiesChanged();

	[RelayCommand]
	private void SetEnglish()
	{
		_localization.SetCulture("en");
		NotifyLocalizedPropertiesChanged();
		NotifyChoicePropertiesChanged();
	}

	[RelayCommand]
	private void SetSpanish()
	{
		_localization.SetCulture("es");
		NotifyLocalizedPropertiesChanged();
		NotifyChoicePropertiesChanged();
	}

	[RelayCommand]
	private void SetThemeSystem() => ApplyTheme(ThemePreference.System);

	[RelayCommand]
	private void SetThemeLight() => ApplyTheme(ThemePreference.Light);

	[RelayCommand]
	private void SetThemeDark() => ApplyTheme(ThemePreference.Dark);

	[RelayCommand]
	private async Task SetWidgetTintSubtleAsync() => await ApplyWidgetTintAsync(WidgetTintStrength.Subtle);

	[RelayCommand]
	private async Task SetWidgetTintMediumAsync() => await ApplyWidgetTintAsync(WidgetTintStrength.Medium);

	[RelayCommand]
	private async Task SetWidgetTintStrongAsync() => await ApplyWidgetTintAsync(WidgetTintStrength.Strong);

	[RelayCommand]
	private async Task GoogleSignInAsync()
	{
		if (IsGoogleSignInBusy)
		{
			return;
		}

		try
		{
			AccountErrorMessage = null;
			AccountMessage = null;
			IsGoogleSignInBusy = true;
			await _authService.SignInWithGoogleAsync();
			await _widgetRefresh.RefreshAsync();
			AccountMessage = _localization.Get("SyncSuccess");
			NotifyAccountPropertiesChanged();
		}
		catch (OperationCanceledException)
		{
			// User dismissed the Google account picker.
		}
		catch (Exception ex)
		{
			AccountErrorMessage = UserFriendlyErrorMapper.FromException(ex, _localization);
		}
		finally
		{
			IsGoogleSignInBusy = false;
		}
	}

	[RelayCommand]
	private async Task SignOutAsync()
	{
		try
		{
			AccountErrorMessage = null;
			AccountMessage = null;
			await _authService.SignOutToGuestAsync();
			await _widgetRefresh.RefreshAsync();
			AccountMessage = _localization.Get("SignedOutToGuest");
			NotifyAccountPropertiesChanged();
		}
		catch (Exception ex)
		{
			AccountErrorMessage = UserFriendlyErrorMapper.FromException(ex, _localization);
		}
	}

	[RelayCommand]
	private async Task OpenRepositoryAsync()
	{
		await Launcher.Default.OpenAsync(RepositoryUrl);
	}

	[RelayCommand]
	private async Task CheckForUpdatesAsync()
	{
		if (Navigation is null)
		{
			return;
		}

		await _updateCoordinator.CheckForUpdatesAsync(Navigation, manual: true);
	}

	private void ApplyTheme(ThemePreference preference)
	{
		_themeService.SetTheme(preference);
		OnPropertyChanged(nameof(ThemeSystemLabel));
		OnPropertyChanged(nameof(ThemeLightLabel));
		OnPropertyChanged(nameof(ThemeDarkLabel));
		NotifyChoicePropertiesChanged();
	}

	private async Task ApplyWidgetTintAsync(WidgetTintStrength tint)
	{
		_widgetAppearance.SetTint(tint);
		OnPropertyChanged(nameof(WidgetTintSubtleLabel));
		OnPropertyChanged(nameof(WidgetTintMediumLabel));
		OnPropertyChanged(nameof(WidgetTintStrongLabel));
		NotifyChoicePropertiesChanged();
		await _widgetRefresh.RefreshAsync();
	}

	private void NotifyChoicePropertiesChanged()
	{
		OnPropertyChanged(nameof(IsEnglishSelected));
		OnPropertyChanged(nameof(IsSpanishSelected));
		OnPropertyChanged(nameof(IsThemeSystemSelected));
		OnPropertyChanged(nameof(IsThemeLightSelected));
		OnPropertyChanged(nameof(IsThemeDarkSelected));
		OnPropertyChanged(nameof(IsWidgetTintSubtleSelected));
		OnPropertyChanged(nameof(IsWidgetTintMediumSelected));
		OnPropertyChanged(nameof(IsWidgetTintStrongSelected));
	}

	private void NotifyAccountPropertiesChanged()
	{
		OnPropertyChanged(nameof(IsGuest));
		OnPropertyChanged(nameof(IsSignedIn));
		OnPropertyChanged(nameof(SignedInEmail));
	}

	private void NotifyLocalizedPropertiesChanged()
	{
		OnPropertyChanged(nameof(Title));
		OnPropertyChanged(nameof(AccountSectionTitle));
		OnPropertyChanged(nameof(LanguageLabel));
		OnPropertyChanged(nameof(ThemeLabel));
		OnPropertyChanged(nameof(LanguageEnglishLabel));
		OnPropertyChanged(nameof(LanguageSpanishLabel));
		OnPropertyChanged(nameof(ThemeSystemLabel));
		OnPropertyChanged(nameof(ThemeLightLabel));
		OnPropertyChanged(nameof(ThemeDarkLabel));
		OnPropertyChanged(nameof(SignOutLabel));
		OnPropertyChanged(nameof(GoogleSignInLabel));
		OnPropertyChanged(nameof(GuestAccountDescription));
		OnPropertyChanged(nameof(SignedInAsLabel));
		OnPropertyChanged(nameof(AboutSectionTitle));
		OnPropertyChanged(nameof(AppVersionLabel));
		OnPropertyChanged(nameof(CreatedByLabel));
		OnPropertyChanged(nameof(ViewSourceLabel));
		OnPropertyChanged(nameof(LicenseLabel));
		OnPropertyChanged(nameof(CheckForUpdatesLabel));
		OnPropertyChanged(nameof(WidgetSectionTitle));
		OnPropertyChanged(nameof(WidgetTintLabel));
		OnPropertyChanged(nameof(WidgetTintSubtleLabel));
		OnPropertyChanged(nameof(WidgetTintMediumLabel));
		OnPropertyChanged(nameof(WidgetTintStrongLabel));
	}
}
