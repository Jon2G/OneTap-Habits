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
			return guest.Habits
				.Where(h => h.IsActive)
				.OrderBy(h => h.Name)
				.ToList();
		}

		var snapshot = await CloudHabitsCollection().GetDocumentsAsync<HabitDto>();
		return snapshot.Documents
			.Where(d => d.Data is not null && d.Data.IsActive)
			.Select(d => d.Data!.ToModel(d.Reference.Id))
			.OrderBy(h => h.Name)
			.ToList();
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
		if (string.IsNullOrWhiteSpace(habit.Id))
		{
			habit.Id = Guid.NewGuid().ToString("N");
			habit.CreatedAt = DateTimeOffset.UtcNow;
		}

		if (_auth.IsGuest)
		{
			var guest = await _guestStore.LoadAsync();
			guest.Habits.RemoveAll(h => h.Id == habit.Id);
			guest.Habits.Add(habit);
			await _guestStore.SaveAsync(guest);
			return;
		}

		await CloudHabitsCollection().GetDocument(habit.Id).SetDataAsync(HabitDto.FromModel(habit));
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

	private ICollectionReference CloudHabitsCollection()
	{
		var userId = _firebaseAuth.CurrentUser?.Uid
			?? throw new InvalidOperationException("User must be signed in.");

		return _firestore.GetCollection($"users/{userId}/habits");
	}

	private sealed class HabitDto
	{
		public string Name { get; set; } = string.Empty;
		public string ColorHex { get; set; } = "#4CAF50";
		public bool ShowInWidget { get; set; } = true;
		public List<int> TargetDays { get; set; } = [];
		public int ScheduleMode { get; set; }
		public int TimesPerWeek { get; set; } = 1;
		public DateTimeOffset CreatedAt { get; set; }
		public bool IsActive { get; set; } = true;

		public static HabitDto FromModel(Habit habit) => new()
		{
			Name = habit.Name,
			ColorHex = habit.ColorHex,
			ShowInWidget = habit.ShowInWidget,
			TargetDays = habit.TargetDays.ToList(),
			ScheduleMode = (int)habit.ScheduleMode,
			TimesPerWeek = habit.TimesPerWeek,
			CreatedAt = habit.CreatedAt,
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
			CreatedAt = CreatedAt,
			IsActive = IsActive
		};
	}
}
