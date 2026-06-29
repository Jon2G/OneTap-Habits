using System.Text.Json;
using OneTapHabits.Models;

namespace OneTapHabits.Services;

public sealed class LocalGuestStore : ILocalGuestStore
{
	public const string FileName = "guest_data.json";

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false
	};

	private readonly string _filePath;

	public LocalGuestStore()
	{
		_filePath = GetFilePath(FileSystem.AppDataDirectory);
	}

	public static string GetFilePath(string appDataDirectory) =>
		Path.Combine(appDataDirectory, FileName);

	public Task<GuestDataSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
		Task.FromResult(LoadFromPath(_filePath));

	public Task SaveAsync(GuestDataSnapshot snapshot, CancellationToken cancellationToken = default)
	{
		SaveToPath(_filePath, snapshot);
		return Task.CompletedTask;
	}

	public Task ClearAsync(CancellationToken cancellationToken = default)
	{
		if (File.Exists(_filePath))
		{
			File.Delete(_filePath);
		}

		return Task.CompletedTask;
	}

	public static GuestDataSnapshot LoadFromPath(string filePath)
	{
		if (!File.Exists(filePath))
		{
			return new GuestDataSnapshot();
		}

		try
		{
			var json = File.ReadAllText(filePath);
			return JsonSerializer.Deserialize<GuestDataSnapshot>(json, JsonOptions) ?? new GuestDataSnapshot();
		}
		catch
		{
			return new GuestDataSnapshot();
		}
	}

	public static void SaveToPath(string filePath, GuestDataSnapshot snapshot)
	{
		var directory = Path.GetDirectoryName(filePath);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}

		var json = JsonSerializer.Serialize(snapshot, JsonOptions);
		File.WriteAllText(filePath, json);
	}

	public static void SetCompleted(string appDataDirectory, string habitId, DateOnly date, bool isCompleted)
	{
		var filePath = GetFilePath(appDataDirectory);
		var snapshot = LoadFromPath(filePath);
		var dateKey = date.ToString("yyyy-MM-dd");

		snapshot.Logs.RemoveAll(l => l.HabitId == habitId && l.Date == dateKey);
		if (isCompleted)
		{
			snapshot.Logs.Add(new GuestLogEntry
			{
				HabitId = habitId,
				Date = dateKey,
				IsCompleted = true,
				Count = 1
			});
		}

		SaveToPath(filePath, snapshot);
	}

	public static int IncrementCount(string appDataDirectory, string habitId, DateOnly date)
	{
		var filePath = GetFilePath(appDataDirectory);
		var snapshot = LoadFromPath(filePath);
		var dateKey = date.ToString("yyyy-MM-dd");
		var existing = snapshot.Logs.FirstOrDefault(l => l.HabitId == habitId && l.Date == dateKey);
		var next = (existing?.Count ?? 0) + 1;

		snapshot.Logs.RemoveAll(l => l.HabitId == habitId && l.Date == dateKey);
		snapshot.Logs.Add(new GuestLogEntry
		{
			HabitId = habitId,
			Date = dateKey,
			IsCompleted = true,
			Count = next
		});

		SaveToPath(filePath, snapshot);
		return next;
	}
}
