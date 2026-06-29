using OneTapHabits.ViewModels;

namespace OneTapHabits.Views;

public partial class HabitFormPage : ContentPage
{
	public HabitFormPage(HabitFormViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
