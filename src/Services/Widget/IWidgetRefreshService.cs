namespace OneTapHabits.Services.Widget;

public interface IWidgetRefreshService
{
	Task RefreshAsync();

	Task ClearAsync();
}
