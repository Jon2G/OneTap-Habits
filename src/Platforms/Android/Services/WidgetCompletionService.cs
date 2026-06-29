using Android.Content;
using OneTapHabits.Models;
using OneTapHabits.Platforms.Android.AppWidgets;
using OneTapHabits.Services;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Platforms.Android.Services;

public static class WidgetCompletionService
{
	public static async Task WriteCompletionAsync(Context context, string habitId)
	{
		var today = DateOnly.FromDateTime(DateTime.Today);
		var userId = CrossFirebaseAuth.Current.CurrentUser?.Uid;

		if (string.IsNullOrEmpty(userId))
		{
			var appDataDirectory = context.FilesDir?.AbsolutePath
				?? throw new InvalidOperationException("Android files directory unavailable.");
			LocalGuestStore.SetCompleted(appDataDirectory, habitId, today, true);
			return;
		}

		FirebaseAndroidBootstrap.EnsureInitialized(context);

		var logId = HabitLog.CreateId(today, habitId);
		var dto = new LogDto
		{
			HabitId = habitId,
			Date = today.ToString("O"),
			IsCompleted = true
		};

		await CrossFirebaseFirestore.Current
			.GetCollection($"users/{userId}/logs")
			.GetDocument(logId)
			.SetDataAsync(dto);
	}

	public static async Task CompleteHabitAsync(Context context, string habitId)
	{
		await WriteCompletionAsync(context, habitId);
		WidgetSnapshotStore.RemoveHabit(context, habitId);
		HabitsAppWidgetProvider.UpdateAllWidgets(context);
	}

	private sealed class LogDto
	{
		public string HabitId { get; set; } = string.Empty;
		public string Date { get; set; } = string.Empty;
		public bool IsCompleted { get; set; }
	}
}
