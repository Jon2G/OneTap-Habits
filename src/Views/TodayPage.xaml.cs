using OneTapHabits.ViewModels;
using OneTapHabits.Services;

namespace OneTapHabits.Views;

public partial class TodayPage : ContentPage
{
	private readonly TodayViewModel _viewModel;
	private readonly UpdateCoordinator _updateCoordinator;

	public TodayPage(TodayViewModel viewModel, UpdateCoordinator updateCoordinator)
	{
		InitializeComponent();
		_viewModel = viewModel;
		_updateCoordinator = updateCoordinator;
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.LoadCommand.ExecuteAsync(null);
#if ANDROID
		_ = _updateCoordinator.CheckForUpdatesAsync(Navigation, manual: false);
#endif
	}
}
