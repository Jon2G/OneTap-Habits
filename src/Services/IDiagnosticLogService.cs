namespace OneTapHabits.Services;

public interface IDiagnosticLogService
{
	void LogInfo(string category, string message);

	void LogWarning(string category, string message);

	void LogError(string category, Exception exception, string? message = null);

	string BuildExportText(DiagnosticExportContext context);

	Task ExportAndShareAsync(DiagnosticExportContext context, CancellationToken cancellationToken = default);
}
