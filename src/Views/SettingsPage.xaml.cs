using OneTapHabits.ViewModels;

namespace OneTapHabits.Views;

public partial class SettingsPage : ContentPage
{
	public SettingsPage(SettingsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is SettingsViewModel viewModel)
		{
			viewModel.RefreshChoices();
		}
	}
}
