using System.Security.Cryptography;
using Android.Content.PM;
using Android.OS;

namespace OneTapHabits.Platforms.Android.Services;

internal static class ApkSigningCertificateHelper
{
	public static string? TryGetSha1Fingerprint(global::Android.Content.Context context)
	{
		try
		{
			var packageName = context.PackageName;
			if (string.IsNullOrWhiteSpace(packageName))
			{
				return null;
			}

			var flags = Build.VERSION.SdkInt >= BuildVersionCodes.P
				? PackageInfoFlags.SigningCertificates
				: PackageInfoFlags.Signatures;

			var packageInfo = context.PackageManager?.GetPackageInfo(packageName, flags);
			if (packageInfo is null)
			{
				return null;
			}

			var signatures = Build.VERSION.SdkInt >= BuildVersionCodes.P
				? packageInfo.SigningInfo?.GetApkContentsSigners()
				: packageInfo.Signatures;

			if (signatures is null || signatures.Count == 0)
			{
				return null;
			}

			var bytes = signatures[0]!.ToByteArray();
			var hash = SHA1.HashData(bytes);
			return BitConverter.ToString(hash).Replace("-", ":", StringComparison.Ordinal);
		}
		catch
		{
			return null;
		}
	}

	public static bool IsSha1RegisteredInGoogleServices(global::Android.Content.Context context, string? sha1Fingerprint)
	{
		if (string.IsNullOrWhiteSpace(sha1Fingerprint))
		{
			return false;
		}

		var normalized = NormalizeSha1(sha1Fingerprint);
		if (normalized.Length == 0)
		{
			return false;
		}

		try
		{
			using var stream = context.Assets?.Open("google-services.json");
			if (stream is null)
			{
				return false;
			}

			using var reader = new StreamReader(stream);
			var json = reader.ReadToEnd();
			return json.Contains(normalized, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	public static int CountRegisteredAndroidOAuthHashes(global::Android.Content.Context context)
	{
		try
		{
			using var stream = context.Assets?.Open("google-services.json");
			if (stream is null)
			{
				return 0;
			}

			using var reader = new StreamReader(stream);
			var json = reader.ReadToEnd();
			return System.Text.RegularExpressions.Regex.Matches(
				json,
				"\"certificate_hash\"",
				System.Text.RegularExpressions.RegexOptions.CultureInvariant).Count;
		}
		catch
		{
			return 0;
		}
	}

	private static string NormalizeSha1(string sha1Fingerprint) =>
		sha1Fingerprint.Replace(":", string.Empty, StringComparison.Ordinal);
}
