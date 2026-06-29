using CommunityToolkit.Mvvm.Messaging;
using OneTapHabits.Messages;
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
		WeakReferenceMessenger.Default.Register<AppResumedMessage>(this, OnAppResumed);
		await _viewModel.LoadCommand.ExecuteAsync(null);
	}

	protected override void OnDisappearing()
	{
		WeakReferenceMessenger.Default.Unregister<AppResumedMessage>(this);
		base.OnDisappearing();
	}

	private async void OnAppResumed(object recipient, AppResumedMessage message)
	{
		await _viewModel.LoadCommand.ExecuteAsync(null);
	}
}
