using OneTapHabits.ViewModels;

namespace OneTapHabits.Views;

public partial class TodayPage : ContentPage
{
	private readonly TodayViewModel _viewModel;

	public TodayPage(TodayViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.LoadCommand.ExecuteAsync(null);
	}
}
