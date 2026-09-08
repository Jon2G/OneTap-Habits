using Android.Content;
using OneTapHabits.Models;
using OneTapHabits.Platforms.Android.AppWidgets;
using OneTapHabits.Services;
using OneTapHabits.Services.Firestore;
using OneTapHabits.Services.Widget;
using OneTapHabits.Storage;
using OneTapHabits.Widget;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Platforms.Android.Services;

public static class WidgetCompletionService
{
	public sealed class AdjustResult
	{
		public int NewCount { get; init; }
		public int DailyTarget { get; init; }
		public bool ShouldRemoveFromWidget => NewCount >= DailyTarget;
	}

	public static AdjustResult IncrementHabitAsync(Context context, string habitId) =>
		AdjustHabitAsync(context, habitId, +1);

	public static AdjustResult DecrementHabitAsync(Context context, string habitId) =>
		AdjustHabitAsync(context, habitId, -1);

	private static AdjustResult AdjustHabitAsync(Context context, string habitId, int delta)
	{
		var today = DateOnly.FromDateTime(DateTime.Today);
		var snapshot = WidgetSnapshotStore.Load(context);
		var cellIndex = snapshot.Habits.FindIndex(h => h.Id == habitId);
		var entry = cellIndex >= 0 ? snapshot.Habits[cellIndex] : null;
		var dailyTarget = entry?.TimesPerDay ?? 1;

		var userId = CrossFirebaseAuth.Current.CurrentUser?.Uid;
		var appDataDirectory = context.FilesDir?.AbsolutePath
			?? throw new InvalidOperationException("Android files directory unavailable.");

		int newCount;
		if (string.IsNullOrEmpty(userId))
		{
			newCount = delta > 0
				? LocalGuestStore.IncrementCount(appDataDirectory, habitId, today)
				: LocalGuestStore.DecrementCount(appDataDirectory, habitId, today);
		}
		else
		{
			newCount = delta > 0
				? LocalCloudStore.IncrementCount(appDataDirectory, userId, habitId, today)
				: LocalCloudStore.DecrementCount(appDataDirectory, userId, habitId, today);
			QueueFirestoreSync(context, userId, habitId, today, newCount);
		}

		var animationKind = ResolveAnimationKind(delta, newCount, dailyTarget);
		if (cellIndex >= 0 && animationKind != WidgetTapAnimationKind.None)
		{
			WidgetTapAnimationStore.Set(context, cellIndex, animationKind);
			HabitsAppWidgetProvider.UpdateAllWidgets(context);
		}

		ApplyWidgetSnapshotUpdate(context, habitId, newCount, dailyTarget, snapshot);

		if (cellIndex >= 0 && animationKind != WidgetTapAnimationKind.None)
		{
			WidgetTapAnimationScheduler.ScheduleFinish(context);
		}
		else
		{
			HabitsAppWidgetProvider.UpdateAllWidgets(context);
		}

		return new AdjustResult
		{
			NewCount = newCount,
			DailyTarget = dailyTarget
		};
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
		Context context,
		string habitId,
		int newCount,
		int dailyTarget,
		WidgetSnapshot previousSnapshot)
	{
		if (newCount >= dailyTarget)
		{
			WidgetSnapshotStore.RemoveHabit(context, habitId);
			return;
		}

		var existing = previousSnapshot.Habits.FirstOrDefault(h => h.Id == habitId);
		if (existing is not null)
		{
			WidgetSnapshotStore.UpdateHabitCount(context, habitId, newCount);
			return;
		}

		RebuildSnapshotFromDisk(context, CrossFirebaseAuth.Current.CurrentUser?.Uid);
	}

	private static void RebuildSnapshotFromDisk(Context context, string? userId)
	{
		var today = DateOnly.FromDateTime(DateTime.Today);
		var appDataDirectory = context.FilesDir?.AbsolutePath;
		if (string.IsNullOrEmpty(appDataDirectory))
		{
			return;
		}

		IReadOnlyList<Habit> habits;
		IReadOnlyDictionary<string, int> countMap;
		if (string.IsNullOrEmpty(userId))
		{
			var guest = LocalGuestStore.LoadFromPath(LocalGuestStore.GetFilePath(appDataDirectory));
			habits = guest.Habits
				.Where(h => h.IsActive && HabitScheduleHelper.IsVisibleOnDate(h, today))
				.ToList();
			var dateKey = today.ToString("yyyy-MM-dd");
			countMap = guest.Logs
				.Where(l => l.Date == dateKey && l.Count > 0)
				.GroupBy(l => l.HabitId)
				.ToDictionary(g => g.Key, g => g.Max(l => l.Count));
		}
		else
		{
			var file = CloudCachePersistence.LoadFromPath(CloudCachePersistence.GetFilePath(appDataDirectory));
			habits = CloudCachePersistence.GetActiveHabits(file, userId)
				.Where(h => HabitScheduleHelper.IsVisibleOnDate(h, today))
				.ToList();
			countMap = CloudCachePersistence.GetCountMapForDate(file, userId, today);
		}

		WidgetSnapshotStore.Save(context, WidgetSnapshotBuilder.Build(habits, countMap, today));
	}

	private static void QueueFirestoreSync(Context context, string userId, string habitId, DateOnly today, int count)
	{
		_ = Task.Run(async () =>
		{
			try
			{
				FirebaseAndroidBootstrap.EnsureInitialized(context);
				var logId = HabitLog.CreateId(today, habitId);
				if (count <= 0)
				{
					await CrossFirebaseFirestore.Current
						.GetCollection($"users/{userId}/logs")
						.GetDocument(logId)
						.DeleteDocumentAsync();
					return;
				}

				await CrossFirebaseFirestore.Current
					.GetCollection($"users/{userId}/logs")
					.GetDocument(logId)
					.SetDataAsync(LogFirestoreDto.FromEntry(habitId, today, count));
			}
			catch
			{
				// Local cache remains authoritative until the app syncs again.
			}
		});
	}
}
