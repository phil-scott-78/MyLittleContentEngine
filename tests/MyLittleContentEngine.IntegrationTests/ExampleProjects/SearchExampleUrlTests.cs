using MyLittleContentEngine.IntegrationTests.Infrastructure;

namespace MyLittleContentEngine.IntegrationTests.ExampleProjects;

public class SearchExampleUrlTests : IClassFixture<SearchExampleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SearchExampleUrlTests(SearchExampleWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [MemberData(nameof(GetSearchExampleUrls))]
    public async Task SearchExample_Urls_ShouldReturnSuccessStatusCodes(string url, string expectedContent)
    {
        var response = await _client.GetAsync(url);
        await response.ShouldReturnSuccessWithContent(expectedContent);
    }

    public static IEnumerable<object[]> GetSearchExampleUrls()
    {
        yield return ["/", "Random Content Site"];
        
        // this should work, but the search service uses the HttpClient to request to the current app for a url 
        // which fails under a test load
        //        yield return new object[] { "/search-index.json", "/random" };
    }
}