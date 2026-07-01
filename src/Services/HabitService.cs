using OneTapHabits.Models;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Services;

public sealed class HabitService : IHabitService
{
	private readonly IAuthService _auth;
	private readonly IFirebaseAuth _firebaseAuth;
	private readonly IFirebaseFirestore _firestore;
	private readonly ILocalGuestStore _guestStore;

	public HabitService(
		IAuthService auth,
		IFirebaseAuth firebaseAuth,
		IFirebaseFirestore firestore,
		ILocalGuestStore guestStore)
	{
		_auth = auth;
		_firebaseAuth = firebaseAuth;
		_firestore = firestore;
		_guestStore = guestStore;
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

		var snapshot = await CloudHabitsCollection().GetDocumentsAsync<HabitDto>();
		var cloudHabits = snapshot.Documents
			.Where(d => d.Data is not null && d.Data.IsActive)
			.Select(d => d.Data!.ToModel(d.Reference.Id))
			.ToList();

		if (HabitSortOrderHelper.TryApplyLegacySortOrders(cloudHabits))
		{
			foreach (var habit in cloudHabits)
			{
				await CloudHabitsCollection().GetDocument(habit.Id).SetDataAsync(HabitDto.FromModel(habit));
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

		var snapshot = await CloudHabitsCollection().GetDocument(habitId).GetDocumentSnapshotAsync<HabitDto>();
		if (snapshot.Data is null)
		{
			return null;
		}

		return snapshot.Data.ToModel(habitId);
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
			await CloudHabitsCollection().GetDocument(habit.Id).SetDataAsync(HabitDto.FromModel(habit));
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

		var habits = (await GetActiveHabitsAsync()).ToList();
		ApplyReorder(habits, orderedHabitIds);
		foreach (var habit in habits)
		{
			await CloudHabitsCollection().GetDocument(habit.Id).SetDataAsync(HabitDto.FromModel(habit));
		}
	}

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

	private ICollectionReference CloudHabitsCollection()
	{
		var userId = _firebaseAuth.CurrentUser?.Uid
			?? throw new InvalidOperationException("User must be signed in.");

		return _firestore.GetCollection($"users/{userId}/habits");
	}

	internal sealed class HabitDto
	{
		public string Name { get; set; } = string.Empty;
		public string ColorHex { get; set; } = "#4CAF50";
		public bool ShowInWidget { get; set; } = true;
		public List<int> TargetDays { get; set; } = [];
		public int ScheduleMode { get; set; }
		public int TimesPerWeek { get; set; } = 1;
		public int TimesPerDay { get; set; } = 1;
		public int SortOrder { get; set; }
		public bool ReminderEnabled { get; set; }
		public string? ReminderTime { get; set; }
		public string CreatedAt { get; set; } = string.Empty;
		public bool IsActive { get; set; } = true;

		public static HabitDto FromModel(Habit habit) => new()
		{
			Name = habit.Name,
			ColorHex = habit.ColorHex,
			ShowInWidget = habit.ShowInWidget,
			TargetDays = habit.TargetDays.ToList(),
			ScheduleMode = (int)habit.ScheduleMode,
			TimesPerWeek = habit.TimesPerWeek,
			TimesPerDay = habit.TimesPerDay,
			SortOrder = habit.SortOrder,
			ReminderEnabled = habit.ReminderEnabled,
			ReminderTime = habit.ReminderTime?.ToString("HH:mm"),
			CreatedAt = habit.CreatedAt.ToString("O"),
			IsActive = habit.IsActive
		};

		public Habit ToModel(string id) => new()
		{
			Id = id,
			Name = Name,
			ColorHex = ColorHex,
			ShowInWidget = ShowInWidget,
			TargetDays = TargetDays,
			ScheduleMode = Enum.IsDefined(typeof(HabitScheduleMode), ScheduleMode)
				? (HabitScheduleMode)ScheduleMode
				: HabitScheduleMode.SpecificDays,
			TimesPerWeek = TimesPerWeek < 1 ? 1 : TimesPerWeek,
			TimesPerDay = TimesPerDay < 1 ? 1 : TimesPerDay,
			SortOrder = SortOrder,
			ReminderEnabled = ReminderEnabled,
			ReminderTime = TimeOnly.TryParse(ReminderTime, out var time) ? time : null,
			CreatedAt = ParseCreatedAt(CreatedAt),
			IsActive = IsActive
		};

		private static DateTimeOffset ParseCreatedAt(string? value) =>
			DateTimeOffset.TryParse(value, out var parsed) ? parsed : DateTimeOffset.UtcNow;
	}
}
