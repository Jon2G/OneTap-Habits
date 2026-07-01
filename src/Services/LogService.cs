using OneTapHabits.Calendar;
using OneTapHabits.Models;
using OneTapHabits.Services.Firestore;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Services;

public sealed class LogService : ILogService
{
	private readonly IAuthService _auth;
	private readonly IFirebaseAuth _firebaseAuth;
	private readonly IFirebaseFirestore _firestore;
	private readonly ILocalGuestStore _guestStore;
	private readonly ILocalCloudStore _cloudStore;

	public LogService(
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

	public async Task<HabitLog?> GetLogAsync(string habitId, DateOnly date)
	{
		var count = await GetCountAsync(habitId, date);
		if (count <= 0)
		{
			return null;
		}

		return ToLog(habitId, date, true, count);
	}

	public async Task<int> GetCountAsync(string habitId, DateOnly date)
	{
		if (_auth.IsGuest)
		{
			var guest = await _guestStore.LoadAsync();
			var dateKey = date.ToString("yyyy-MM-dd");
			var entry = guest.Logs.FirstOrDefault(l => l.HabitId == habitId && l.Date == dateKey);
			return entry?.Count ?? 0;
		}

		return await _cloudStore.GetCountAsync(RequireUserId(), habitId, date);
	}

	public async Task<int> IncrementCountAsync(string habitId, DateOnly date)
	{
		if (_auth.IsGuest)
		{
			var guest = await _guestStore.LoadAsync();
			var dateKey = date.ToString("yyyy-MM-dd");
			var current = guest.Logs.FirstOrDefault(l => l.HabitId == habitId && l.Date == dateKey)?.Count ?? 0;
			var next = current + 1;
			await UpsertGuestCountAsync(guest, habitId, dateKey, next);
			return next;
		}

		var userId = RequireUserId();
		var nextCount = await _cloudStore.IncrementCountAsync(userId, habitId, date);
		QueueCloudUpsert(habitId, date, nextCount);
		return nextCount;
	}

	public async Task SetCompletedAsync(string habitId, DateOnly date, bool isCompleted)
	{
		if (_auth.IsGuest)
		{
			var guest = await _guestStore.LoadAsync();
			var dateKey = date.ToString("yyyy-MM-dd");
			await UpsertGuestCountAsync(guest, habitId, dateKey, isCompleted ? 1 : 0);
			return;
		}

		var userId = RequireUserId();
		if (isCompleted)
		{
			await _cloudStore.SetCountAsync(userId, habitId, date, 1);
			QueueCloudUpsert(habitId, date, 1);
			return;
		}

		await _cloudStore.SetCountAsync(userId, habitId, date, 0);
		QueueCloudDelete(habitId, date);
	}

	public async Task<IReadOnlyDictionary<string, bool>> GetCompletionMapForDateAsync(DateOnly date)
	{
		var countMap = await GetCountMapForDateAsync(date);
		return countMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value >= 1);
	}

	public async Task<IReadOnlyDictionary<string, int>> GetCountMapForDateAsync(DateOnly date)
	{
		if (_auth.IsGuest)
		{
			var guest = await _guestStore.LoadAsync();
			var dateKey = date.ToString("yyyy-MM-dd");
			return guest.Logs
				.Where(l => l.Date == dateKey && l.Count > 0)
				.GroupBy(l => l.HabitId)
				.ToDictionary(g => g.Key, g => g.Max(l => l.Count));
		}

		return await _cloudStore.GetCountMapForDateAsync(RequireUserId(), date);
	}

	public async Task<IReadOnlyList<HabitLog>> GetCompletedLogsInRangeAsync(
		DateOnly startInclusive,
		DateOnly endInclusive)
	{
		if (endInclusive < startInclusive)
		{
			return [];
		}

		if (_auth.IsGuest)
		{
			var guest = await _guestStore.LoadAsync();
			return GuestLogQuery.FilterCompletedInRange(guest, startInclusive, endInclusive);
		}

		return await _cloudStore.GetLogsInRangeAsync(RequireUserId(), startInclusive, endInclusive);
	}

	private async Task UpsertGuestCountAsync(GuestDataSnapshot guest, string habitId, string dateKey, int count)
	{
		guest.Logs.RemoveAll(l => l.HabitId == habitId && l.Date == dateKey);
		if (count > 0)
		{
			guest.Logs.Add(new GuestLogEntry
			{
				HabitId = habitId,
				Date = dateKey,
				IsCompleted = true,
				Count = count
			});
		}

		await _guestStore.SaveAsync(guest);
	}

	private void QueueCloudUpsert(string habitId, DateOnly date, int count) =>
		_ = Task.Run(async () =>
		{
			try
			{
				await UpsertCloudCountAsync(habitId, date, count);
			}
			catch
			{
				// Local cache remains authoritative until next successful sync.
			}
		});

	private void QueueCloudDelete(string habitId, DateOnly date) =>
		_ = Task.Run(async () =>
		{
			try
			{
				await DeleteCloudLogAsync(habitId, date);
			}
			catch
			{
				// Local cache already cleared.
			}
		});

	private async Task UpsertCloudCountAsync(string habitId, DateOnly date, int count)
	{
		var id = HabitLog.CreateId(date, habitId);
		await CloudLogsCollection()
			.GetDocument(id)
			.SetDataAsync(LogFirestoreDto.FromEntry(habitId, date, count));
	}

	private async Task DeleteCloudLogAsync(string habitId, DateOnly date)
	{
		var id = HabitLog.CreateId(date, habitId);
		await CloudLogsCollection().GetDocument(id).DeleteDocumentAsync();
	}

	private static HabitLog ToLog(string habitId, DateOnly date, bool isCompleted, int count) => new()
	{
		Id = HabitLog.CreateId(date, habitId),
		HabitId = habitId,
		Date = date,
		IsCompleted = isCompleted || count > 0,
		Count = count
	};

	private string RequireUserId() =>
		_firebaseAuth.CurrentUser?.Uid
		?? throw new InvalidOperationException("User must be signed in.");

	private ICollectionReference CloudLogsCollection()
	{
		var userId = RequireUserId();
		return _firestore.GetCollection($"users/{userId}/logs");
	}
}
