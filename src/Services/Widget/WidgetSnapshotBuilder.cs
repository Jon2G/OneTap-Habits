using OneTapHabits.Models;

namespace OneTapHabits.Services.Widget;

public static class WidgetSnapshotBuilder
{
	public const int MaxVisibleHabits = 6;

	public static WidgetSnapshot Build(
		IReadOnlyList<Habit> habits,
		IReadOnlyDictionary<string, int> countMap,
		DateOnly today,
		bool isSignedIn = true,
		string? userId = null)
	{
		var incomplete = habits
			.Where(h => h.ShowInWidget)
			.Where(h =>
			{
				var count = countMap.TryGetValue(h.Id, out var value) ? value : 0;
				return !HabitDailyTargetHelper.IsDailyTargetMet(h, count);
			})
			.Select(h => new WidgetHabitEntry
			{
				Id = h.Id,
				Name = h.Name,
				ColorHex = h.ColorHex,
				Count = countMap.TryGetValue(h.Id, out var count) ? count : 0,
				TimesPerDay = HabitDailyTargetHelper.GetDailyTarget(h)
			})
			.ToList();

		var overflow = Math.Max(0, incomplete.Count - MaxVisibleHabits);
		var visible = incomplete.Take(MaxVisibleHabits).ToList();

		return new WidgetSnapshot
		{
			IsSignedIn = isSignedIn,
			UserId = userId,
			DateIso = today.ToString("O"),
			Habits = visible,
			OverflowCount = overflow
		};
	}
}
