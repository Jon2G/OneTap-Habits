using OneTapHabits.Models;
using OneTapHabits.Widget;

namespace OneTapHabits.WidgetHost;

internal static class WidgetCompletionService
{
	public static void IncrementHabit(string habitId) => AdjustHabit(habitId, +1);

	public static void DecrementHabit(string habitId) => AdjustHabit(habitId, -1);

	private static void AdjustHabit(string habitId, int delta)
	{
		var today = DateOnly.FromDateTime(DateTime.Today);
		var snapshot = WidgetSnapshotFileStore.Load();
		var cellIndex = snapshot.Habits.FindIndex(h => h.Id == habitId);
		var entry = cellIndex >= 0 ? snapshot.Habits[cellIndex] : null;
		var dailyTarget = entry?.TimesPerDay ?? 1;
		var appDataDirectory = WidgetSnapshotFileStore.GetAppDataDirectory();

		int newCount;
		if (string.IsNullOrEmpty(snapshot.UserId))
		{
			newCount = delta > 0
				? WidgetLocalDataStore.IncrementGuestCount(appDataDirectory, habitId, today)
				: WidgetLocalDataStore.DecrementGuestCount(appDataDirectory, habitId, today);
		}
		else
		{
			newCount = delta > 0
				? WidgetLocalDataStore.IncrementCloudCount(appDataDirectory, snapshot.UserId, habitId, today)
				: WidgetLocalDataStore.DecrementCloudCount(appDataDirectory, snapshot.UserId, habitId, today);
		}

		var animationKind = ResolveAnimationKind(delta, newCount, dailyTarget);
		if (cellIndex >= 0 && animationKind != WidgetTapAnimationKind.None)
		{
			WidgetTapAnimationFileStore.Set(cellIndex, animationKind);
			HabitsWidgetProvider.RefreshAllWidgets();
			WidgetAnimationScheduler.ScheduleFinish(() =>
			{
				ApplyWidgetSnapshotUpdate(habitId, newCount, dailyTarget, snapshot);
				WidgetTapAnimationFileStore.Clear();
				HabitsWidgetProvider.RefreshAllWidgets();
			});
			return;
		}

		ApplyWidgetSnapshotUpdate(habitId, newCount, dailyTarget, snapshot);
		HabitsWidgetProvider.RefreshAllWidgets();
	}

	private static WidgetTapAnimationKind ResolveAnimationKind(int delta, int newCount, int dailyTarget)
	{
		if (delta > 0)
		{
			return newCount >= dailyTarget ? WidgetTapAnimationKind.Complete : WidgetTapAnimationKind.PlusOne;
		}

		return newCount > 0 ? WidgetTapAnimationKind.MinusOne : WidgetTapAnimationKind.None;
	}

	private static void ApplyWidgetSnapshotUpdate(
		string habitId,
		int newCount,
		int dailyTarget,
		WidgetSnapshot previousSnapshot)
	{
		if (newCount >= dailyTarget)
		{
			WidgetSnapshotFileStore.RemoveHabit(habitId);
			return;
		}

		var existing = previousSnapshot.Habits.FirstOrDefault(h => h.Id == habitId);
		if (existing is not null)
		{
			WidgetSnapshotFileStore.UpdateHabitCount(habitId, newCount);
		}
	}
}
