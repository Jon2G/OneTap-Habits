using System.Text;
using System.Text.RegularExpressions;
using OneTapHabits.Services;

namespace OneTapHabits.Views;

[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public partial class UpdatePopupPage : ContentPage
{
	private const string ReleasesUrl = "https://github.com/Jon2G/OneTap-Habits/releases/latest";

	private readonly string _assignedVersion;
	private readonly string? _apkDownloadUrl;
	private readonly UpdateService _updateService;
	private readonly ILocalizationService _localization;
	private bool _isClosing;
	private bool _isDownloading;
	private bool _isFallbackMode;
	private CancellationTokenSource? _downloadCts;

	public UpdatePopupPage(
		string version,
		string changelog,
		string? apkDownloadUrl,
		UpdateService updateService,
		ILocalizationService localization)
	{
		InitializeComponent();
		_assignedVersion = version;
		_apkDownloadUrl = apkDownloadUrl;
		_updateService = updateService;
		_localization = localization;

		HeaderTitle.Text = _localization.Get("UpdateAvailableTitle");
		VersionLabel.Text = $"v{version}";
		LaterButton.Text = _localization.Get("UpdateLater");
		InstallButton.Text = _localization.Get("UpdateInstall");

		if (string.IsNullOrWhiteSpace(changelog))
		{
			ChangelogBorder.IsVisible = false;
		}
		else
		{
			LoadChangelogHtml(changelog);
		}
	}

	private void LoadChangelogHtml(string markdown)
	{
		var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
		var bg = isDark ? "#121212" : "#FAFAFA";
		var fg = isDark ? "#F5F5F5" : "#1A1A1A";
		var fgMuted = isDark ? "#9CA3AF" : "#6B7280";
		var accent = isDark ? "#4ADE80" : "#22C55E";
		var divider = isDark ? "#374151" : "#E5E7EB";
		var utilBg = isDark ? "#2A2A2A" : "#F0F0F0";
		var bulletCol = isDark ? "#374151" : "#E5E7EB";

		var bodyHtml = ConvertMarkdownToStructuredHtml(markdown);
		var html = $@"<!DOCTYPE html>
<html><head><meta name='viewport' content='width=device-width,initial-scale=1,maximum-scale=1'>
<style>
*{{margin:0;padding:0;box-sizing:border-box;}}
body{{
  background:{bg};color:{fg};
  font-family:-apple-system,'SF Pro Text',Roboto,'Segoe UI',sans-serif;
  font-size:14px;line-height:1.6;
  padding:16px;
  -webkit-text-size-adjust:100%;
}}
.section{{margin-bottom:16px;}}
.section:last-child{{margin-bottom:0;}}
.section-title{{
  font-size:15px;font-weight:700;color:{fg};
  margin:0 0 10px 0;padding-bottom:8px;
  border-bottom:1px solid {divider};letter-spacing:-0.2px;
}}
.section-body p{{margin:6px 0;color:{fgMuted};font-size:13px;line-height:1.55;}}
.section-body ul,.section-body ol{{list-style:none;padding:0;margin:0;}}
.section-body ul li{{
  position:relative;padding:8px 0 8px 18px;
  font-size:13.5px;line-height:1.5;border-bottom:1px solid {divider};
}}
.section-body ul li:last-child{{border-bottom:none;}}
.section-body ul li::before{{
  content:'';position:absolute;left:0;top:15px;
  width:6px;height:6px;border-radius:50%;background:{accent};
}}
.section-body ol{{counter-reset:step;}}
.section-body ol li{{
  position:relative;padding:8px 0 8px 24px;
  font-size:13.5px;line-height:1.5;border-bottom:1px solid {divider};
  counter-increment:step;
}}
.section-body ol li:last-child{{border-bottom:none;}}
.section-body ol li::before{{
  content:counter(step);position:absolute;left:0;top:8px;
  width:18px;height:18px;border-radius:50%;
  background:{accent};color:#fff;font-size:11px;font-weight:700;
  display:flex;align-items:center;justify-content:center;
}}
strong{{font-weight:600;color:{fg};}}
em{{font-style:italic;}}
code{{
  background:{divider};padding:1px 5px;border-radius:4px;
  font-size:12px;font-family:'SF Mono',Menlo,monospace;
}}
.util-section{{background:{utilBg};border-radius:10px;padding:12px 14px;margin-bottom:10px;}}
.util-title{{
  font-size:13px;font-weight:700;color:{fgMuted};
  text-transform:uppercase;letter-spacing:0.5px;margin:0 0 8px 0;
}}
.util-section ul li,.util-section ol li{{font-size:13px;color:{fgMuted};border-bottom-color:{divider};}}
.util-section ul li::before,.util-section ol li::before{{background:{bulletCol};}}
.section-divider{{border:none;border-top:1px solid {divider};margin:4px 0;}}
blockquote{{
  border-left:3px solid {accent};padding:6px 12px;margin:8px 0;
  color:{fgMuted};font-size:13px;background:{utilBg};border-radius:0 6px 6px 0;
}}
</style></head><body>{bodyHtml}</body></html>";

		ChangelogWebView.Source = new HtmlWebViewSource { Html = html };
		ChangelogWebView.Navigated += (_, _) =>
		{
			ChangelogWebView.EvaluateJavaScriptAsync("document.body.scrollHeight")
				.ContinueWith(t =>
				{
					if (t.Result is string result && double.TryParse(result, out var height) && height > 0)
					{
						MainThread.BeginInvokeOnMainThread(() => ChangelogWebView.HeightRequest = height + 16);
					}
				});
		};
	}

	private static readonly HashSet<string> UtilitySections = new(StringComparer.OrdinalIgnoreCase)
	{
		"Installation", "Requirements", "About"
	};

	private static string ConvertMarkdownToStructuredHtml(string markdown)
	{
		var lines = markdown.Split('\n');
		var sb = new StringBuilder();
		var inSection = false;
		var inSectionBody = false;
		var inUl = false;
		var inOl = false;
		var firstH2Stripped = false;

		foreach (var rawLine in lines)
		{
			var line = rawLine.TrimEnd('\r');

			if (Regex.IsMatch(line.Trim(), @"^-{3,}$|^\*{3,}$|^_{3,}$"))
			{
				CloseList(sb, ref inUl, ref inOl);
				CloseSectionBody(sb, ref inSectionBody);
				CloseSection(sb, ref inSection);
				sb.AppendLine("<hr class='section-divider'/>");
				continue;
			}

			if (line.StartsWith("## "))
			{
				var title = line[3..].Trim();
				if (!firstH2Stripped && title.StartsWith("What", StringComparison.OrdinalIgnoreCase))
				{
					firstH2Stripped = true;
					continue;
				}

				CloseList(sb, ref inUl, ref inOl);
				CloseSectionBody(sb, ref inSectionBody);
				CloseSection(sb, ref inSection);
				OpenSection(sb, title, ref inSection, ref inSectionBody);
				continue;
			}

			if (line.StartsWith("### "))
			{
				var title = line[4..].Trim();
				CloseList(sb, ref inUl, ref inOl);
				CloseSectionBody(sb, ref inSectionBody);
				CloseSection(sb, ref inSection);
				OpenSection(sb, title, ref inSection, ref inSectionBody);
				continue;
			}

			if (line.StartsWith("# "))
			{
				continue;
			}

			if (!inSection)
			{
				sb.AppendLine("<div class='section'>");
				inSection = true;
				sb.AppendLine("<div class='section-body'>");
				inSectionBody = true;
			}

			if (line.StartsWith("> "))
			{
				CloseList(sb, ref inUl, ref inOl);
				sb.AppendLine($"<blockquote>{FormatInline(line[2..])}</blockquote>");
				continue;
			}

			var ulMatch = Regex.Match(line, @"^\s*[-*]\s+(.+)");
			if (ulMatch.Success)
			{
				if (inOl)
				{
					sb.AppendLine("</ol>");
					inOl = false;
				}

				if (!inUl)
				{
					sb.AppendLine("<ul>");
					inUl = true;
				}

				sb.AppendLine($"<li>{FormatInline(ulMatch.Groups[1].Value)}</li>");
				continue;
			}

			var olMatch = Regex.Match(line, @"^\s*\d+\.\s+(.+)");
			if (olMatch.Success)
			{
				if (inUl)
				{
					sb.AppendLine("</ul>");
					inUl = false;
				}

				if (!inOl)
				{
					sb.AppendLine("<ol>");
					inOl = true;
				}

				sb.AppendLine($"<li>{FormatInline(olMatch.Groups[1].Value)}</li>");
				continue;
			}

			if (string.IsNullOrWhiteSpace(line))
			{
				CloseList(sb, ref inUl, ref inOl);
				continue;
			}

			CloseList(sb, ref inUl, ref inOl);
			sb.AppendLine($"<p>{FormatInline(line)}</p>");
		}

		CloseList(sb, ref inUl, ref inOl);
		CloseSectionBody(sb, ref inSectionBody);
		CloseSection(sb, ref inSection);
		return sb.ToString();
	}

	private static void OpenSection(StringBuilder sb, string title, ref bool inSection, ref bool inSectionBody)
	{
		var isUtil = UtilitySections.Contains(title.Trim());
		sb.AppendLine(isUtil ? "<div class='section util-section'>" : "<div class='section'>");
		sb.AppendLine(isUtil
			? $"<div class='util-title'>{FormatInline(title)}</div>"
			: $"<div class='section-title'>{FormatInline(title)}</div>");
		inSection = true;
		sb.AppendLine("<div class='section-body'>");
		inSectionBody = true;
	}

	private static void CloseList(StringBuilder sb, ref bool inUl, ref bool inOl)
	{
		if (inUl)
		{
			sb.AppendLine("</ul>");
			inUl = false;
		}

		if (inOl)
		{
			sb.AppendLine("</ol>");
			inOl = false;
		}
	}

	private static void CloseSectionBody(StringBuilder sb, ref bool inSectionBody)
	{
		if (inSectionBody)
		{
			sb.AppendLine("</div>");
			inSectionBody = false;
		}
	}

	private static void CloseSection(StringBuilder sb, ref bool inSection)
	{
		if (inSection)
		{
			sb.AppendLine("</div>");
			inSection = false;
		}
	}

	private static string FormatInline(string text)
	{
		text = Regex.Replace(text, @"`([^`]+)`", "<code>$1</code>");
		text = Regex.Replace(text, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
		text = Regex.Replace(text, @"__(.+?)__", "<strong>$1</strong>");
		text = Regex.Replace(text, @"\*(.+?)\*", "<em>$1</em>");
		text = Regex.Replace(text, @"(?<!\w)_(.+?)_(?!\w)", "<em>$1</em>");
		return text;
	}

	private async void OnLaterClicked(object sender, EventArgs e)
	{
		if (_isClosing || _isDownloading)
		{
			return;
		}

		_isClosing = true;
		Preferences.Default.Set(UpdateCoordinator.LastRemoteVersionSeenKey, _assignedVersion);
		try
		{
			await Navigation.PopModalAsync();
		}
		catch
		{
			// Modal may already be dismissed.
		}
	}

	private async void OnInstallClicked(object? sender, EventArgs e)
	{
		if (_isDownloading)
		{
			return;
		}

		if (string.IsNullOrEmpty(_apkDownloadUrl))
		{
			await FallbackToGitHubAsync();
			return;
		}

		_isDownloading = true;
		InstallButton.IsEnabled = false;
		LaterButton.IsEnabled = false;
		InstallButton.Text = _localization.Get("UpdateInstall");
		_downloadCts = new CancellationTokenSource();

		var progress = new Progress<double>(percent =>
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				var pct = (int)Math.Round(percent * 100);
				ProgressLabel.IsVisible = true;
				ProgressLabel.Text = string.Format(_localization.Get("UpdateDownloading"), pct);
			});
		});

		var apkPath = await _updateService.DownloadApkAsync(
			_apkDownloadUrl,
			_assignedVersion,
			progress,
			_downloadCts.Token);

		if (apkPath == null || !File.Exists(apkPath))
		{
			ShowDownloadError();
			return;
		}

		Preferences.Default.Set(UpdateCoordinator.UpdateJustInstalledKey, true);
		Preferences.Default.Set(UpdateCoordinator.LastRemoteVersionSeenKey, _assignedVersion);
		ProgressLabel.Text = _localization.Get("UpdateStartingInstaller");
		TriggerApkInstall(apkPath);

		try
		{
			await Navigation.PopModalAsync();
		}
		catch
		{
			// Modal may already be dismissed.
		}
	}

	private void ShowDownloadError()
	{
		_isDownloading = false;
		InstallButton.IsEnabled = true;
		LaterButton.IsEnabled = true;
		InstallButton.Text = _localization.Get("UpdateInstall");
		ProgressLabel.IsVisible = true;
		ProgressLabel.Text = _localization.Get("UpdateDownloadFailed");
		ProgressLabel.TextColor = Color.FromArgb("#DC2626");
		SwitchToFallbackMode();
	}

	private void SwitchToFallbackMode()
	{
		if (_isFallbackMode)
		{
			return;
		}

		_isFallbackMode = true;
		InstallButton.Text = _localization.Get("UpdateOpenGitHub");
		InstallButton.Clicked -= OnInstallClicked;
		InstallButton.Clicked += OnFallbackClicked;
	}

	private async void OnFallbackClicked(object? sender, EventArgs e)
	{
		await FallbackToGitHubAsync();
	}

	private static async Task FallbackToGitHubAsync()
	{
		try
		{
			await Launcher.Default.OpenAsync(ReleasesUrl);
		}
		catch
		{
			// Browser may be unavailable.
		}
	}

	private void TriggerApkInstall(string apkPath)
	{
#if ANDROID
		try
		{
			var context = Android.App.Application.Context;
			var file = new Java.IO.File(apkPath);
			var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(
				context,
				$"{context.PackageName}.fileProvider",
				file);

			var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
			intent.SetDataAndType(uri, "application/vnd.android.package-archive");
			intent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission);
			intent.AddFlags(Android.Content.ActivityFlags.NewTask);
			context.StartActivity(intent);
		}
		catch
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				ProgressLabel.IsVisible = true;
				ProgressLabel.Text = _localization.Get("UpdateInstallFailed");
				ProgressLabel.TextColor = Color.FromArgb("#DC2626");
				_isDownloading = false;
				InstallButton.IsEnabled = true;
				LaterButton.IsEnabled = true;
				SwitchToFallbackMode();
			});
		}
#endif
	}
}
