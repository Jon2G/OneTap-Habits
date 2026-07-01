using CommunityToolkit.Mvvm.Messaging;
using OneTapHabits.Messages;
using OneTapHabits.Services;
using OneTapHabits.ViewModels;

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
		HabitsCollectionView.ReorderCompleted += OnReorderCompleted;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		WeakReferenceMessenger.Default.Register<AppResumedMessage>(this, OnAppResumed);
		WeakReferenceMessenger.Default.Register<AuthChangedMessage>(this, OnAuthChanged);
		await _viewModel.LoadCommand.ExecuteAsync(null);
#if ANDROID
		_ = _updateCoordinator.CheckForUpdatesAsync(Navigation, manual: false);
#endif
	}

	protected override void OnDisappearing()
	{
		WeakReferenceMessenger.Default.Unregister<AppResumedMessage>(this);
		WeakReferenceMessenger.Default.Unregister<AuthChangedMessage>(this);
		base.OnDisappearing();
	}

	private async void OnAppResumed(object recipient, AppResumedMessage message)
	{
		await _viewModel.LoadCommand.ExecuteAsync(null);
	}

	private async void OnAuthChanged(object recipient, AuthChangedMessage message)
	{
		await _viewModel.LoadCommand.ExecuteAsync(null);
	}

	private async void OnReorderCompleted(object? sender, EventArgs e)
	{
		await _viewModel.PersistHabitOrderAsync();
	}
}
