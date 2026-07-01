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

	public static async Task<IncrementResult> IncrementHabitAsync(Context context, string habitId)
	{
		var today = DateOnly.FromDateTime(DateTime.Today);
		var dailyTarget = await GetDailyTargetAsync(context, habitId);
		var newCount = await WriteIncrementAsync(context, habitId, today);
		return new IncrementResult
		{
			NewCount = newCount,
			DailyTarget = dailyTarget
		};
	}

	private static async Task<int> WriteIncrementAsync(Context context, string habitId, DateOnly today)
	{
		var userId = CrossFirebaseAuth.Current.CurrentUser?.Uid;

		if (string.IsNullOrEmpty(userId))
		{
			var appDataDirectory = context.FilesDir?.AbsolutePath
				?? throw new InvalidOperationException("Android files directory unavailable.");
			return LocalGuestStore.IncrementCount(appDataDirectory, habitId, today);
		}

		FirebaseAndroidBootstrap.EnsureInitialized(context);

		var logId = HabitLog.CreateId(today, habitId);
		var doc = CrossFirebaseFirestore.Current
			.GetCollection($"users/{userId}/logs")
			.GetDocument(logId);

		var snapshot = await doc.GetDocumentSnapshotAsync<LogFirestoreDto>();
		var current = snapshot.Data?.ResolveCount() ?? 0;
		var next = current + 1;

		await doc.SetDataAsync(LogFirestoreDto.FromEntry(habitId, today, next));

		return next;
	}

	private static async Task<int> GetDailyTargetAsync(Context context, string habitId)
	{
		var userId = CrossFirebaseAuth.Current.CurrentUser?.Uid;

		if (string.IsNullOrEmpty(userId))
		{
			var appDataDirectory = context.FilesDir?.AbsolutePath;
			if (string.IsNullOrEmpty(appDataDirectory))
			{
				return 1;
			}

			var guest = LocalGuestStore.LoadFromPath(LocalGuestStore.GetFilePath(appDataDirectory));
			var habit = guest.Habits.FirstOrDefault(h => h.Id == habitId && h.IsActive);
			return habit is null ? 1 : HabitDailyTargetHelper.GetDailyTarget(habit);
		}

		FirebaseAndroidBootstrap.EnsureInitialized(context);
		var habitSnapshot = await CrossFirebaseFirestore.Current
			.GetCollection($"users/{userId}/habits")
			.GetDocument(habitId)
			.GetDocumentSnapshotAsync<HabitFirestoreDto>();

		if (habitSnapshot.Data is null || !habitSnapshot.Data.IsValidCloudDocument())
		{
			return 1;
		}

		return HabitDailyTargetHelper.GetDailyTarget(habitSnapshot.Data.ToModel(habitId));
	}
}
