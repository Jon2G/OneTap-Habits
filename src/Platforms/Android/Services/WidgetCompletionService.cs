using Android.Content;
using OneTapHabits.Models;
using OneTapHabits.Platforms.Android.AppWidgets;
using OneTapHabits.Services;
using OneTapHabits.Services.Firestore;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Platforms.Android.Services;

public static class WidgetCompletionService
{
	public sealed class IncrementResult
	{
		public int NewCount { get; init; }
		public int DailyTarget { get; init; }
		public bool ShouldRemoveFromWidget => NewCount >= DailyTarget;
	}

	public static IncrementResult IncrementHabitAsync(Context context, string habitId)
	{
		var today = DateOnly.FromDateTime(DateTime.Today);
		var snapshot = WidgetSnapshotStore.Load(context);
		var entry = snapshot.Habits.FirstOrDefault(h => h.Id == habitId);
		var dailyTarget = entry?.TimesPerDay ?? 1;

		var userId = CrossFirebaseAuth.Current.CurrentUser?.Uid;
		var appDataDirectory = context.FilesDir?.AbsolutePath
			?? throw new InvalidOperationException("Android files directory unavailable.");

		int newCount;
		if (string.IsNullOrEmpty(userId))
		{
			newCount = LocalGuestStore.IncrementCount(appDataDirectory, habitId, today);
		}
		else
		{
			newCount = LocalLogOverlayStore.IncrementCount(appDataDirectory, userId, habitId, today);
			QueueFirestoreSync(context, userId, habitId, today, newCount);
		}

		ApplyWidgetSnapshotUpdate(context, habitId, newCount, dailyTarget);
		HabitsAppWidgetProvider.UpdateAllWidgets(context);

		return new IncrementResult
		{
			NewCount = newCount,
			DailyTarget = dailyTarget
		};
	}

	private static void ApplyWidgetSnapshotUpdate(Context context, string habitId, int newCount, int dailyTarget)
	{
		if (newCount >= dailyTarget)
		{
			WidgetSnapshotStore.RemoveHabit(context, habitId);
			return;
		}

		WidgetSnapshotStore.UpdateHabitCount(context, habitId, newCount);
	}

	private static void QueueFirestoreSync(Context context, string userId, string habitId, DateOnly today, int count)
	{
		_ = Task.Run(async () =>
		{
			try
			{
				FirebaseAndroidBootstrap.EnsureInitialized(context);
				var logId = HabitLog.CreateId(today, habitId);
				await CrossFirebaseFirestore.Current
					.GetCollection($"users/{userId}/logs")
					.GetDocument(logId)
					.SetDataAsync(LogFirestoreDto.FromEntry(habitId, today, count));
			}
			catch
			{
				// Local overlay remains authoritative until the app syncs again.
			}
		});
	}
}
