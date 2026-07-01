using OneTapHabits.Calendar;
using OneTapHabits.Models;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace OneTapHabits.Services;

public sealed class LogService : ILogService
{
	private readonly IAuthService _auth;
	private readonly IFirebaseAuth _firebaseAuth;
	private readonly IFirebaseFirestore _firestore;
	private readonly ILocalGuestStore _guestStore;

	public LogService(
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

	public async Task<HabitLog?> GetLogAsync(string habitId, DateOnly date)
	{
		if (_auth.IsGuest)
		{
			var guest = await _guestStore.LoadAsync();
			var dateKey = date.ToString("yyyy-MM-dd");
			var entry = guest.Logs.FirstOrDefault(l => l.HabitId == habitId && l.Date == dateKey);
			if (entry is null)
			{
				return null;
			}

			return ToLog(habitId, date, entry.IsCompleted, entry.Count);
		}

		var id = HabitLog.CreateId(date, habitId);
		var snapshot = await CloudLogsCollection().GetDocument(id).GetDocumentSnapshotAsync<LogDto>();
		if (snapshot.Data is null)
		{
			return null;
		}

		return snapshot.Data.ToModel(id, habitId, date);
	}

	public async Task<int> GetCountAsync(string habitId, DateOnly date)
	{
		var log = await GetLogAsync(habitId, date);
		return log?.Count ?? 0;
	}

	public async Task<int> IncrementCountAsync(string habitId, DateOnly date)
	{
		var current = await GetCountAsync(habitId, date);
		var next = current + 1;
		await UpsertCountAsync(habitId, date, next);
		return next;
	}

	public async Task SetCompletedAsync(string habitId, DateOnly date, bool isCompleted)
	{
		if (isCompleted)
		{
			await UpsertCountAsync(habitId, date, 1);
			return;
		}

		await UpsertCountAsync(habitId, date, 0);
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

		var prefix = date.ToString("yyyy-MM-dd");
		var snapshot = await CloudLogsCollection().GetDocumentsAsync<LogDto>();
		return snapshot.Documents
			.Where(d => d.Data is not null && d.Reference.Id.StartsWith(prefix, StringComparison.Ordinal))
			.Select(d => new
			{
				HabitId = HabitLogDocumentId.ResolveHabitId(d.Reference.Id, d.Data!.HabitId),
				Count = ResolveCount(d.Data!)
			})
			.Where(x => !string.IsNullOrEmpty(x.HabitId) && x.Count > 0)
			.GroupBy(x => x.HabitId)
			.ToDictionary(g => g.Key, g => g.Max(x => x.Count));
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

		var snapshot = await CloudLogsCollection().GetDocumentsAsync<LogDto>();
		return snapshot.Documents
			.Where(d => d.Data is not null && (d.Data!.IsCompleted || d.Data.Count > 0))
			.Select(d => ToLogFromSnapshot(d.Reference.Id, d.Data!))
			.Where(l => l is not null && l.Date >= startInclusive && l.Date <= endInclusive)
			.Cast<HabitLog>()
			.ToList();
	}

	private async Task UpsertCountAsync(string habitId, DateOnly date, int count)
	{
		if (_auth.IsGuest)
		{
			var guest = await _guestStore.LoadAsync();
			var dateKey = date.ToString("yyyy-MM-dd");
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
			return;
		}

		var id = HabitLog.CreateId(date, habitId);
		if (count <= 0)
		{
			await CloudLogsCollection().GetDocument(id).DeleteDocumentAsync();
			return;
		}

		var dto = new LogDto
		{
			HabitId = habitId,
			Date = date.ToString("O"),
			IsCompleted = true,
			Count = count
		};

		await CloudLogsCollection().GetDocument(id).SetDataAsync(dto);
	}

	private static HabitLog ToLog(string habitId, DateOnly date, bool isCompleted, int count) => new()
	{
		Id = HabitLog.CreateId(date, habitId),
		HabitId = habitId,
		Date = date,
		IsCompleted = isCompleted || count > 0,
		Count = count
	};

	private static int ResolveCount(LogDto dto) => dto.Count > 0 ? dto.Count : dto.IsCompleted ? 1 : 0;

	private static HabitLog? ToLogFromSnapshot(string id, LogDto dto)
	{
		var habitId = HabitLogDocumentId.ResolveHabitId(id, dto.HabitId);
		if (string.IsNullOrEmpty(habitId))
		{
			return null;
		}

		if (HabitLogDocumentId.TryParse(id, out var dateFromId, out _))
		{
			return dto.ToModel(id, habitId, dateFromId);
		}

		if (DateOnly.TryParse(dto.Date, out var parsedDate))
		{
			return dto.ToModel(id, habitId, parsedDate);
		}

		return null;
	}

	private ICollectionReference CloudLogsCollection()
	{
		var userId = _firebaseAuth.CurrentUser?.Uid
			?? throw new InvalidOperationException("User must be signed in.");

		return _firestore.GetCollection($"users/{userId}/logs");
	}

	internal sealed class LogDto
	{
		public string HabitId { get; set; } = string.Empty;
		public string Date { get; set; } = string.Empty;
		public bool IsCompleted { get; set; }
		public int Count { get; set; } = 1;

		public HabitLog ToModel(string id, string habitId, DateOnly date)
		{
			var count = Count > 0 ? Count : IsCompleted ? 1 : 0;
			return new HabitLog
			{
				Id = id,
				HabitId = habitId,
				Date = date,
				IsCompleted = count > 0,
				Count = count
			};
		}
	}
}
