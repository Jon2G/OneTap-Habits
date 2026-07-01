using OneTapHabits.Models;
using OneTapHabits.Services.Firestore;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Services;

public sealed class HabitService : IHabitService
{
	private readonly IAuthService _auth;
	private readonly IFirebaseAuth _firebaseAuth;
	private readonly IFirebaseFirestore _firestore;
	private readonly ILocalGuestStore _guestStore;
	private readonly ILocalCloudStore _cloudStore;

	public HabitService(
		IAuthService auth,
		IFirebaseAuth firebaseAuth,
		IFirebaseFirestore firestore,
		ILocalGuestStore guestStore,
		ILocalCloudStore cloudStore)
	{
		_auth = auth;
		_firebaseAuth = firebaseAuth;
		_firestore = firestore;
		_guestStore = guestStore;
		_cloudStore = cloudStore;
	}

	public async Task<IReadOnlyList<Habit>> GetActiveHabitsAsync()
	{
		if (_auth.IsGuest)
		{
			var guest = await _guestStore.LoadAsync();
			var habits = guest.Habits.Where(h => h.IsActive).ToList();
			if (HabitSortOrderHelper.TryApplyLegacySortOrders(habits))
			{
				foreach (var habit in habits)
				{
					var stored = guest.Habits.First(h => h.Id == habit.Id);
					stored.SortOrder = habit.SortOrder;
				}

				await _guestStore.SaveAsync(guest);
			}

			return OrderHabits(habits);
		}

		var userId = RequireUserId();
		var cloudHabits = (await _cloudStore.GetActiveHabitsAsync(userId)).ToList();
		if (HabitSortOrderHelper.TryApplyLegacySortOrders(cloudHabits))
		{
			foreach (var habit in cloudHabits)
			{
				await _cloudStore.UpsertHabitAsync(userId, habit);
				QueueCloudHabitUpsert(habit);
			}
		}

		return OrderHabits(cloudHabits);
	}

	public async Task<Habit?> GetHabitAsync(string habitId)
	{
		if (_auth.IsGuest)
		{
			var guest = await _guestStore.LoadAsync();
			return guest.Habits.FirstOrDefault(h => h.Id == habitId);
		}

		return await _cloudStore.GetHabitAsync(RequireUserId(), habitId);
	}

	public async Task SaveHabitAsync(Habit habit)
	{
		var isNew = string.IsNullOrWhiteSpace(habit.Id);
		if (isNew)
		{
			habit.Id = Guid.NewGuid().ToString("N");
			habit.CreatedAt = DateTimeOffset.UtcNow;
			var existing = await GetActiveHabitsAsync();
			habit.SortOrder = HabitSortOrderHelper.GetNextSortOrder(existing);
		}

		habit.TimesPerDay = habit.ScheduleMode == HabitScheduleMode.SpecificDays
			? Math.Clamp(habit.TimesPerDay, 1, 10)
			: 1;

		if (_auth.IsGuest)
		{
			var guest = await _guestStore.LoadAsync();
			guest.Habits.RemoveAll(h => h.Id == habit.Id);
			guest.Habits.Add(habit);
			await _guestStore.SaveAsync(guest);
		}
		else
		{
			await _cloudStore.UpsertHabitAsync(RequireUserId(), habit);
			QueueCloudHabitUpsert(habit);
		}
	}

	public async Task DeleteHabitAsync(string habitId)
	{
		var habit = await GetHabitAsync(habitId)
			?? throw new InvalidOperationException($"Habit not found: {habitId}");

		habit.IsActive = false;
		await SaveHabitAsync(habit);
	}

	public async Task<IReadOnlyList<Habit>> GetTodayHabitsAsync(DateOnly today)
	{
		var habits = await GetActiveHabitsAsync();
		return habits.Where(h => HabitScheduleHelper.IsVisibleOnDate(h, today)).ToList();
	}

	public async Task ReorderHabitsAsync(IReadOnlyList<string> orderedHabitIds)
	{
		if (orderedHabitIds.Count == 0)
		{
			return;
		}

		if (_auth.IsGuest)
		{
			var guest = await _guestStore.LoadAsync();
			var active = guest.Habits.Where(h => h.IsActive).ToList();
			ApplyReorder(active, orderedHabitIds);
			await _guestStore.SaveAsync(guest);
			return;
		}

		var userId = RequireUserId();
		var habits = (await GetActiveHabitsAsync()).ToList();
		ApplyReorder(habits, orderedHabitIds);
		foreach (var habit in habits)
		{
			await _cloudStore.UpsertHabitAsync(userId, habit);
			QueueCloudHabitUpsert(habit);
		}
	}

	private void QueueCloudHabitUpsert(Habit habit) =>
		_ = Task.Run(async () =>
		{
			try
			{
				await CloudHabitsCollection()
					.GetDocument(habit.Id)
					.SetDataAsync(HabitFirestoreDto.FromModel(habit));
			}
			catch
			{
				// Local cache remains authoritative until next successful sync.
			}
		});

	private static void ApplyReorder(IList<Habit> habits, IReadOnlyList<string> orderedHabitIds)
	{
		for (var i = 0; i < orderedHabitIds.Count; i++)
		{
			var habit = habits.FirstOrDefault(h => h.Id == orderedHabitIds[i]);
			if (habit is not null)
			{
				habit.SortOrder = i;
			}
		}
	}

	private static IReadOnlyList<Habit> OrderHabits(IEnumerable<Habit> habits) =>
		habits.OrderBy(h => h.SortOrder).ThenBy(h => h.Name, StringComparer.OrdinalIgnoreCase).ToList();

	private string RequireUserId() =>
		_firebaseAuth.CurrentUser?.Uid
		?? throw new InvalidOperationException("User must be signed in.");

	private ICollectionReference CloudHabitsCollection()
	{
		var userId = RequireUserId();
		return _firestore.GetCollection($"users/{userId}/habits");
	}
}
