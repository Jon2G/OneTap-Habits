using OneTapHabits.Services;
using Xunit;

namespace OneTapHabits.Tests;

public class GoogleServicesJsonParserTests
{
	private const string SampleWithWebClient = """
		{
		  "project_info": {
		    "project_id": "onetap-habits"
		  },
		  "client": [
		    {
		      "api_key": [
		        { "current_key": "AIzaSyExampleKey" }
		      ],
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

	private const string SampleWebClientOnlyUnderOtherPlatform = """
		{
		  "client": [
		    {
		      "client_info": {
		        "android_client_info": { "package_name": "com.jon2g.onetaphabits" }
		      },
		      "oauth_client": [
		        { "client_type": 1, "client_id": "android-client" }
		      ],
		      "services": {
		        "appinvite_service": {
		          "other_platform_oauth_client": [
		            { "client_type": 3, "client_id": "69895168896-web.apps.googleusercontent.com" }
		          ]
		        }
		      }
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
	public void TryGetWebClientId_ReturnsType3FromOtherPlatformOauthClient()
	{
		var clientId = GoogleServicesJsonParser.TryGetWebClientId(SampleWebClientOnlyUnderOtherPlatform);
		Assert.Equal("69895168896-web.apps.googleusercontent.com", clientId);
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

	[Fact]
	public void TryGetProjectId_ReturnsProjectInfoValue()
	{
		Assert.Equal("onetap-habits", GoogleServicesJsonParser.TryGetProjectId(SampleWithWebClient));
	}

	[Fact]
	public void TryGetApiKey_ReturnsCurrentKey()
	{
		Assert.Equal("AIzaSyExampleKey", GoogleServicesJsonParser.TryGetApiKey(SampleWithWebClient));
	}
}
