using System.Runtime.Versioning;
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

			var packageInfo = Build.VERSION.SdkInt >= BuildVersionCodes.P
				? GetPackageInfoWithSigningCertificates(context, packageName)
				: GetPackageInfoWithLegacySignatures(context, packageName);
			if (packageInfo is null)
			{
				return null;
			}

			var bytes = Build.VERSION.SdkInt >= BuildVersionCodes.P
				? GetSignatureBytesFromSigningInfo(packageInfo)
				: GetSignatureBytesFromLegacySignatures(packageInfo);
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

	[SupportedOSPlatform("android28.0")]
	private static PackageInfo? GetPackageInfoWithSigningCertificates(
		global::Android.Content.Context context,
		string packageName) =>
		context.PackageManager?.GetPackageInfo(packageName, PackageInfoFlags.SigningCertificates);

	[SupportedOSPlatform("android24.0")]
	private static PackageInfo? GetPackageInfoWithLegacySignatures(
		global::Android.Content.Context context,
		string packageName) =>
		context.PackageManager?.GetPackageInfo(packageName, PackageInfoFlags.Signatures);

	[SupportedOSPlatform("android28.0")]
	private static byte[]? GetSignatureBytesFromSigningInfo(PackageInfo packageInfo)
	{
		var signers = packageInfo.SigningInfo?.GetApkContentsSigners();
		if (signers is null || signers.Count == 0)
		{
			return null;
		}

		return signers[0]?.ToByteArray();
	}

	[SupportedOSPlatform("android24.0")]
	private static byte[]? GetSignatureBytesFromLegacySignatures(PackageInfo packageInfo)
	{
		var signatures = packageInfo.Signatures;
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
