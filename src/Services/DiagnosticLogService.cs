namespace OneTapHabits.Services;

public sealed class DiagnosticLogService : IDiagnosticLogService
{
	private readonly DiagnosticLogBuffer _buffer;

	public DiagnosticLogService(DiagnosticLogBuffer buffer)
	{
		_buffer = buffer;
	}

	public void LogInfo(string category, string message) =>
		Append("Info", category, message, null);

	public void LogWarning(string category, string message) =>
		Append("Warning", category, message, null);

	public void LogError(string category, Exception exception, string? message = null)
	{
		var detail = DiagnosticLogExporter.FormatException(exception);
		var text = string.IsNullOrWhiteSpace(message) ? exception.Message : $"{message} | {exception.Message}";
		Append("Error", category, text, detail);
	}

	public string BuildExportText(DiagnosticExportContext context) =>
		DiagnosticLogExporter.BuildExportText(_buffer.Snapshot(), context);

	public async Task ExportAndShareAsync(DiagnosticExportContext context, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var text = BuildExportText(context);
		var fileName = $"onetaphabits-diagnostic-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt";
		var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
		await File.WriteAllTextAsync(filePath, text, cancellationToken);

		await Share.Default.RequestAsync(new ShareFileRequest
		{
			Title = "OneTap Habits diagnostic log",
			File = new ShareFile(filePath)
		});
	}

	private void Append(string level, string category, string message, string? exceptionDetail)
	{
		_buffer.Append(new DiagnosticLogEntry(
			DateTimeOffset.UtcNow,
			level,
			category,
			message,
			exceptionDetail));
	}
}
