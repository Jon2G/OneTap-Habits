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

			var packageManager = context.PackageManager;
			if (packageManager is null)
			{
				return null;
			}

			byte[]? bytes;
			if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
			{
#pragma warning disable CA1416
				var packageInfo = packageManager.GetPackageInfo(
					packageName,
					PackageInfoFlags.SigningCertificates);
				bytes = GetSignatureBytesFromSigningInfo(packageInfo);
#pragma warning restore CA1416
			}
			else
			{
#pragma warning disable CA1422
				var packageInfo = packageManager.GetPackageInfo(
					packageName,
					PackageInfoFlags.Signatures);
				bytes = GetSignatureBytesFromLegacySignatures(packageInfo);
#pragma warning restore CA1422
			}

			if (bytes is null || bytes.Length == 0)
			{
				return null;
			}

			var hash = SHA1.HashData(bytes);
			return BitConverter.ToString(hash).Replace("-", ":", StringComparison.Ordinal);
		}
		catch
		{
			return null;
		}
	}

	private static byte[]? GetSignatureBytesFromSigningInfo(PackageInfo? packageInfo)
	{
		var signers = packageInfo?.SigningInfo?.GetApkContentsSigners();
		if (signers is null || signers.Length == 0)
		{
			return null;
		}

		return signers[0]?.ToByteArray();
	}

	private static byte[]? GetSignatureBytesFromLegacySignatures(PackageInfo? packageInfo)
	{
		var signatures = packageInfo?.Signatures;
		if (signatures is null || signatures.Count == 0)
		{
			return null;
		}

		return signatures[0]?.ToByteArray();
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
