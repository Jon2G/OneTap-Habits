using OneTapHabits.Services;
using Xunit;

namespace OneTapHabits.Tests;

public class DiagnosticLogBufferTests
{
	[Fact]
	public void Append_trims_to_max_entries()
	{
		var buffer = new DiagnosticLogBuffer { MaxEntries = 2 };
		buffer.Append(Entry("a"));
		buffer.Append(Entry("b"));
		buffer.Append(Entry("c"));

		var snapshot = buffer.Snapshot();
		Assert.Equal(2, snapshot.Count);
		Assert.Equal("b", snapshot[0].Message);
		Assert.Equal("c", snapshot[1].Message);
	}

	[Fact]
	public void BuildExportText_includes_header_and_entries()
	{
		var entries = new[]
		{
			new DiagnosticLogEntry(
				new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero),
				"Error",
				"GoogleSignIn",
				"failed",
				"System.InvalidOperationException: test")
		};

		var text = DiagnosticLogExporter.BuildExportText(entries, new DiagnosticExportContext
		{
			AppVersion = "1.2.2",
			BuildString = "10022",
			Platform = "Android",
			PlatformVersion = "14",
			Culture = "en",
			AuthState = "Guest"
		});

		Assert.Contains("OneTap Habits — diagnostic log", text);
		Assert.Contains("App: 1.2.2", text);
		Assert.Contains("[Error] GoogleSignIn: failed", text);
		Assert.Contains("InvalidOperationException", text);
	}

	private static DiagnosticLogEntry Entry(string message) =>
		new(DateTimeOffset.UtcNow, "Info", "Test", message, null);
}
