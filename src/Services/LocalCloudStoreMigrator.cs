using OneTapHabits.Storage;

namespace OneTapHabits.Services;

public static class LocalCloudStoreMigrator
{
	public static void MigrateLegacyOverlayIfNeeded()
	{
		try
		{
			var appData = FileSystem.AppDataDirectory;
			var overlayPath = LogOverlayPersistence.GetFilePath(appData);
			if (!File.Exists(overlayPath))
			{
				return;
			}

			var overlay = LogOverlayPersistence.LoadFromPath(overlayPath);
			if (overlay.Entries.Count == 0)
			{
				File.Delete(overlayPath);
				return;
			}

			var cachePath = CloudCachePersistence.GetFilePath(appData);
			var cache = CloudCachePersistence.LoadFromPath(cachePath);
			foreach (var entry in overlay.Entries)
			{
				if (entry.Count <= 0 ||
				    string.IsNullOrEmpty(entry.UserId) ||
				    string.IsNullOrEmpty(entry.HabitId) ||
				    !DateOnly.TryParse(entry.Date, out var date))
				{
					continue;
				}

				var existing = CloudCachePersistence.GetCount(cache, entry.UserId, entry.HabitId, date);
				if (entry.Count > existing)
				{
					CloudCachePersistence.SetCount(cache, entry.UserId, entry.HabitId, date, entry.Count);
				}
			}

			CloudCachePersistence.SaveToPath(cachePath, cache);
			File.Delete(overlayPath);
		}
		catch
		{
			// Migration is best-effort.
		}
	}
}
