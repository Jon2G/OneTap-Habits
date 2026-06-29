namespace OneTapHabits.Services;

public static class ShellNavigationService
{
	public static async Task GoToTodayAsync()
	{
		if (Shell.Current is not null)
		{
			await Shell.Current.GoToAsync("//today");
		}
	}
}
