namespace OneTapHabits.Services;

public static class ErrorMessageKeyResolver
{
	public const string Unknown = "ErrorUnknown";
	public const string MissingFields = "ErrorMissingFields";
	public const string EmailInvalid = "ErrorEmailInvalid";
	public const string InvalidCredentials = "ErrorInvalidCredentials";
	public const string EmailInUse = "ErrorEmailInUse";
	public const string WeakPassword = "ErrorWeakPassword";
	public const string Network = "ErrorNetwork";
	public const string TooManyRequests = "ErrorTooManyRequests";
	public const string GoogleNotConfigured = "ErrorGoogleNotConfigured";

	public static string Resolve(Exception ex)
	{
		var text = CollectMessages(ex);
		if (string.IsNullOrWhiteSpace(text))
		{
			return Unknown;
		}

		if (ContainsAny(text,
			    "given string is empty or null",
			    "cannot be null",
			    "cannot be empty",
			    "must not be empty"))
		{
			return MissingFields;
		}

		if (ContainsAny(text,
			    "badly formatted",
			    "invalid email",
			    "error_invalid_email"))
		{
			return EmailInvalid;
		}

		if (ContainsAny(text,
			    "wrong password",
			    "invalid credential",
			    "invalid login credentials",
			    "error_wrong_password",
			    "password is invalid or the user does not have a password"))
		{
			return InvalidCredentials;
		}

		if (ContainsAny(text,
			    "no user record",
			    "user not found",
			    "error_user_not_found",
			    "user may have been deleted"))
		{
			return InvalidCredentials;
		}

		if (ContainsAny(text,
			    "already in use",
			    "error_email_already_in_use"))
		{
			return EmailInUse;
		}

		if (ContainsAny(text,
			    "weak password",
			    "password should be at least",
			    "error_weak_password"))
		{
			return WeakPassword;
		}

		if (ContainsAny(text,
			    "network",
			    "timeout",
			    "unreachable",
			    "unable to resolve host",
			    "connection"))
		{
			return Network;
		}

		if (ContainsAny(text, "too many requests", "error_too_many_requests"))
		{
			return TooManyRequests;
		}

		if (ContainsAny(text,
			    "google sign-in is not configured",
			    "enable google in firebase",
			    "sha-1 fingerprints",
			    "default_web_client_id",
			    "package certificate hash",
			    "certificate hash"))
		{
			return GoogleNotConfigured;
		}

		if (ContainsAny(text,
			    "permission_denied",
			    "missing or insufficient permissions",
			    "firestore",
			    "cloud firestore"))
		{
			return Network;
		}

		return Unknown;
	}

	private static string CollectMessages(Exception ex)
	{
		var parts = new List<string>();
		for (var current = ex; current is not null; current = current.InnerException)
		{
			if (!string.IsNullOrWhiteSpace(current.Message))
			{
				parts.Add(current.Message);
			}
		}

		return string.Join(' ', parts);
	}

	private static bool ContainsAny(string haystack, params string[] needles)
	{
		foreach (var needle in needles)
		{
			if (haystack.Contains(needle, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}
}
