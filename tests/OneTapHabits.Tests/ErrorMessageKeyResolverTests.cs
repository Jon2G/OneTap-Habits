using OneTapHabits.Services;
using Xunit;

namespace OneTapHabits.Tests;

public class ErrorMessageKeyResolverTests
{
	[Fact]
	public void Resolve_MapsEmptyStringError()
	{
		var key = ErrorMessageKeyResolver.Resolve(new Exception("Given String is empty or null"));
		Assert.Equal(ErrorMessageKeyResolver.MissingFields, key);
	}

	[Fact]
	public void Resolve_MapsWrongPassword()
	{
		var key = ErrorMessageKeyResolver.Resolve(new Exception("ERROR_WRONG_PASSWORD"));
		Assert.Equal(ErrorMessageKeyResolver.InvalidCredentials, key);
	}

	[Fact]
	public void Resolve_MapsNetworkError()
	{
		var key = ErrorMessageKeyResolver.Resolve(
			new Exception("A network error (such as timeout) has occurred."));

		Assert.Equal(ErrorMessageKeyResolver.Network, key);
	}

	[Fact]
	public void Resolve_UsesUnknownForUnrecognizedError()
	{
		var key = ErrorMessageKeyResolver.Resolve(new Exception("Some obscure internal failure"));
		Assert.Equal(ErrorMessageKeyResolver.Unknown, key);
	}
}
