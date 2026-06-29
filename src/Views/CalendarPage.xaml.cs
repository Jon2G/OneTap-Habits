using OneTapHabits.ViewModels;

namespace OneTapHabits.Views;

public partial class CalendarPage : ContentPage
{
	private readonly CalendarViewModel _viewModel;

	public CalendarPage(CalendarViewModel viewModel)
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
