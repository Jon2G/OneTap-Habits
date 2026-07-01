using System.Text;

namespace OneTapHabits.Services;

public sealed class DiagnosticExportContext
{
	public required string AppVersion { get; init; }

	public required string BuildString { get; init; }

	public required string Platform { get; init; }

	public required string PlatformVersion { get; init; }

	public required string Culture { get; init; }

	public required string AuthState { get; init; }

	public string? UserIdHint { get; init; }
}

public static class DiagnosticLogExporter
{
	public static string BuildExportText(
		IReadOnlyList<DiagnosticLogEntry> entries,
		DiagnosticExportContext context)
	{
		var builder = new StringBuilder();
		builder.AppendLine("OneTap Habits — diagnostic log");
		builder.AppendLine($"Generated: {DateTimeOffset.UtcNow:O}");
		builder.AppendLine($"App: {context.AppVersion} ({context.BuildString})");
		builder.AppendLine($"Platform: {context.Platform} {context.PlatformVersion}");
		builder.AppendLine($"Culture: {context.Culture}");
		builder.AppendLine($"Auth: {context.AuthState}");
		if (!string.IsNullOrWhiteSpace(context.UserIdHint))
		{
			builder.AppendLine($"UserId: {context.UserIdHint}");
		}

		builder.AppendLine();
		builder.AppendLine("--- Entries ---");

		foreach (var entry in entries)
		{
			builder.AppendLine(
				$"{entry.Timestamp:O} [{entry.Level}] {entry.Category}: {entry.Message}");
			if (!string.IsNullOrWhiteSpace(entry.ExceptionDetail))
			{
				builder.AppendLine(entry.ExceptionDetail);
			}
		}

		return builder.ToString();
	}

	public static string FormatException(Exception exception)
	{
		var builder = new StringBuilder();
		for (var current = exception; current is not null; current = current.InnerException)
		{
			builder.AppendLine($"{current.GetType().FullName}: {current.Message}");
			if (!string.IsNullOrWhiteSpace(current.StackTrace))
			{
				builder.AppendLine(current.StackTrace);
			}

			if (current.InnerException is not null)
			{
				builder.AppendLine("--- Inner ---");
			}
		}

		return builder.ToString().TrimEnd();
	}
}
