using OneTapHabits.Services;
using Xunit;

namespace OneTapHabits.Tests;

public class GoogleServicesJsonParserTests
{
	private const string SampleWithWebClient = """
		{
		  "client": [
		    {
		      "oauth_client": [
		        { "client_type": 1, "client_id": "android-client" },
		        { "client_type": 3, "client_id": "123456789-web.apps.googleusercontent.com" }
		      ]
		    }
		  ]
		}
		""";

	private const string SampleWithoutWebClient = """
		{
		  "client": [
		    {
		      "oauth_client": [
		        { "client_type": 1, "client_id": "android-only" }
		      ]
		    }
		  ]
		}
		""";

	[Fact]
	public void TryGetWebClientId_ReturnsType3ClientId()
	{
		var clientId = GoogleServicesJsonParser.TryGetWebClientId(SampleWithWebClient);
		Assert.Equal("123456789-web.apps.googleusercontent.com", clientId);
	}

	[Fact]
	public void TryGetWebClientId_ReturnsNull_WhenNoType3Client()
	{
		Assert.Null(GoogleServicesJsonParser.TryGetWebClientId(SampleWithoutWebClient));
	}

	[Fact]
	public void TryGetWebClientId_ReturnsNull_ForEmptyOrInvalidJson()
	{
		Assert.Null(GoogleServicesJsonParser.TryGetWebClientId(string.Empty));
		Assert.Null(GoogleServicesJsonParser.TryGetWebClientId("{ not json"));
	}
}
