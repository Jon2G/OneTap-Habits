namespace OneTapHabits;

using CommunityToolkit.Mvvm.Messaging;
using OneTapHabits.Messages;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new MainShell());
	}

	protected override void OnResume()
	{
		base.OnResume();
		WeakReferenceMessenger.Default.Send(new AppResumedMessage());
	}
}