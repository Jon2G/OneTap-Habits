namespace OneTapHabits.Services;

public sealed class DiagnosticLogBuffer
{
	private readonly object _lock = new();
	private readonly Queue<DiagnosticLogEntry> _entries = new();

	public int MaxEntries { get; set; } = 500;

	public void Append(DiagnosticLogEntry entry)
	{
		lock (_lock)
		{
			_entries.Enqueue(entry);
			while (_entries.Count > MaxEntries)
			{
				_entries.Dequeue();
			}
		}
	}

	public IReadOnlyList<DiagnosticLogEntry> Snapshot()
	{
		lock (_lock)
		{
			return _entries.ToList();
		}
	}
}

public readonly record struct DiagnosticLogEntry(
	DateTimeOffset Timestamp,
	string Level,
	string Category,
	string Message,
	string? ExceptionDetail);
