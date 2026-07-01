using OneTapHabits.Models;

namespace OneTapHabits.Services.Widget;

public interface IWidgetRefreshService
{
	Task RefreshAsync();

	Task RefreshAsync(IReadOnlyList<Habit> todayHabits, IReadOnlyDictionary<string, int> countMap);

	Task ClearAsync();
}
