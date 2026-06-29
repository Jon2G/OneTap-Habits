using OneTapHabits.Resources.Strings;

namespace OneTapHabits;

public partial class MainShell : Shell
{
	public MainShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("habitForm", typeof(Views.HabitFormPage));
		Routing.RegisterRoute("settings", typeof(Views.SettingsPage));
		UpdateTabTitles();
	}

	private void UpdateTabTitles()
	{
		if (Items.Count == 0 || Items[0] is not TabBar tabBar)
		{
			return;
		}

		if (tabBar.Items.Count > 0)
		{
			tabBar.Items[0].Title = AppResources.Get("TabToday");
		}

		if (tabBar.Items.Count > 1)
		{
			tabBar.Items[1].Title = AppResources.Get("TabCalendar");
		}
	}
}
