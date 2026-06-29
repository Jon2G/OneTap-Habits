using OneTapHabits.Models;

namespace OneTapHabits;

public static class SampleHabitSeed
{
	public static IReadOnlyList<Habit> Create(string gymName, string medicationName)
	{
		var createdAt = DateTimeOffset.UtcNow;

		return
		[
			new Habit
			{
				Id = Guid.NewGuid().ToString("N"),
				Name = gymName,
				ColorHex = "#F97316",
				ShowInWidget = true,
				TargetDays = [1, 2, 3, 4, 5, 6, 7],
				CreatedAt = createdAt,
				IsActive = true
			},
			new Habit
			{
				Id = Guid.NewGuid().ToString("N"),
				Name = medicationName,
				ColorHex = "#3B82F6",
				ShowInWidget = true,
				TargetDays = [1, 2, 3, 4, 5, 6, 7],
				CreatedAt = createdAt,
				IsActive = true
			}
		];
	}
}
