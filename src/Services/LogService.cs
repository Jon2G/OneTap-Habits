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
	private readonly ILocalLogOverlayStore _logOverlay;

	public LogService(
		IAuthService auth,
		IFirebaseAuth firebaseAuth,
		IFirebaseFirestore firestore,
		ILocalGuestStore guestStore,
		ILocalLogOverlayStore logOverlay)
	{
		_auth = auth;
		_firebaseAuth = firebaseAuth;
		_firestore = firestore;
		_guestStore = guestStore;
		_logOverlay = logOverlay;
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

		var count = await GetMergedCountAsync(habitId, date);
		if (count <= 0)
		{
			return null;
		}

		var id = HabitLog.CreateId(date, habitId);
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

		return await GetMergedCountAsync(habitId, date);
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
		var nextCount = await _logOverlay.IncrementCountAsync(userId, habitId, date);
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
			await _logOverlay.SetCountAsync(userId, habitId, date, 1);
			QueueCloudUpsert(habitId, date, 1);
			return;
		}

		await _logOverlay.SetCountAsync(userId, habitId, date, 0);
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

		var userId = RequireUserId();
		var cloudMap = await FetchCloudCountMapForDateAsync(date);
		var overlayMap = await _logOverlay.GetCountMapForDateAsync(userId, date);
		return MergeCountMaps(cloudMap, overlayMap);
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

		var userId = RequireUserId();
		var cloudLogs = await FetchCloudLogsInRangeAsync(startInclusive, endInclusive);
		var overlayEntries = await _logOverlay.GetLogEntriesInRangeAsync(userId, startInclusive, endInclusive);
		return MergeLogsWithOverlay(cloudLogs, overlayEntries);
	}

	private async Task<int> GetMergedCountAsync(string habitId, DateOnly date)
	{
		var userId = RequireUserId();
		var overlayCount = await _logOverlay.GetCountAsync(userId, habitId, date);
		var cloudCount = await FetchCloudCountAsync(habitId, date);
		return Math.Max(overlayCount, cloudCount);
	}

	private async Task<int> FetchCloudCountAsync(string habitId, DateOnly date)
	{
		var id = HabitLog.CreateId(date, habitId);
		var snapshot = await CloudLogsCollection().GetDocument(id).GetDocumentSnapshotAsync<LogFirestoreDto>();
		return snapshot.Data?.ResolveCount() ?? 0;
	}

	private async Task<IReadOnlyDictionary<string, int>> FetchCloudCountMapForDateAsync(DateOnly date)
	{
		var prefix = date.ToString("yyyy-MM-dd");
		var snapshot = await CloudLogsCollection().GetDocumentsAsync<LogFirestoreDto>();
		return snapshot.Documents
			.Where(d => d.Data is not null && d.Reference.Id.StartsWith(prefix, StringComparison.Ordinal))
			.Select(d => new
			{
				HabitId = HabitLogDocumentId.ResolveHabitId(d.Reference.Id, d.Data!.HabitId),
				Count = d.Data.ResolveCount()
			})
			.Where(x => !string.IsNullOrEmpty(x.HabitId) && x.Count > 0)
			.GroupBy(x => x.HabitId)
			.ToDictionary(g => g.Key, g => g.Max(x => x.Count));
	}

	private async Task<IReadOnlyList<HabitLog>> FetchCloudLogsInRangeAsync(
		DateOnly startInclusive,
		DateOnly endInclusive)
	{
		var snapshot = await CloudLogsCollection().GetDocumentsAsync<LogFirestoreDto>();
		return snapshot.Documents
			.Where(d => d.Data is not null && d.Data.ResolveCount() > 0)
			.Select(d => ToLogFromSnapshot(d.Reference.Id, d.Data!))
			.Where(l => l is not null && l.Date >= startInclusive && l.Date <= endInclusive)
			.Cast<HabitLog>()
			.ToList();
	}

	private static IReadOnlyDictionary<string, int> MergeCountMaps(
		IReadOnlyDictionary<string, int> cloud,
		IReadOnlyDictionary<string, int> overlay)
	{
		var merged = new Dictionary<string, int>(cloud);
		foreach (var (habitId, count) in overlay)
		{
			merged[habitId] = merged.TryGetValue(habitId, out var existing)
				? Math.Max(existing, count)
				: count;
		}

		return merged;
	}

	private static List<HabitLog> MergeLogsWithOverlay(
		IReadOnlyList<HabitLog> cloudLogs,
		IReadOnlyList<GuestLogEntry> overlayEntries)
	{
		var byKey = cloudLogs.ToDictionary(l => (l.HabitId, l.Date), l => l);
		foreach (var entry in overlayEntries)
		{
			if (!DateOnly.TryParse(entry.Date, out var date))
			{
				continue;
			}

			var key = (entry.HabitId, date);
			if (byKey.TryGetValue(key, out var existing))
			{
				if (entry.Count > existing.Count)
				{
					byKey[key] = ToLog(entry.HabitId, date, true, entry.Count);
				}
			}
			else if (entry.Count > 0)
			{
				byKey[key] = ToLog(entry.HabitId, date, true, entry.Count);
			}
		}

		return byKey.Values.OrderBy(l => l.Date).ThenBy(l => l.HabitId, StringComparer.Ordinal).ToList();
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
				// Overlay remains source of truth until next successful sync.
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
				// Overlay already cleared locally.
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

	private static HabitLog? ToLogFromSnapshot(string id, LogFirestoreDto dto)
	{
		var habitId = HabitLogDocumentId.ResolveHabitId(id, dto.HabitId);
		if (string.IsNullOrEmpty(habitId))
		{
			return null;
		}

		var count = dto.ResolveCount();
		if (count <= 0)
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

	private string RequireUserId() =>
		_firebaseAuth.CurrentUser?.Uid
		?? throw new InvalidOperationException("User must be signed in.");

	private ICollectionReference CloudLogsCollection()
	{
		var userId = RequireUserId();
		return _firestore.GetCollection($"users/{userId}/logs");
	}
}
