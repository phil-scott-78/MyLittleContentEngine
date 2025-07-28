using MyLittleContentEngine.IntegrationTests.Infrastructure;

namespace MyLittleContentEngine.IntegrationTests.ExampleProjects;

public class MultipleContentSourceExampleUrlTests : IClassFixture<MultipleContentSourceExampleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MultipleContentSourceExampleUrlTests(MultipleContentSourceExampleWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [MemberData(nameof(GetMultipleContentSourceExampleUrls))]
    public async Task MultipleContentSourceExample_Urls_ShouldReturnSuccessStatusCodes(string url, string expectedContent)
    {
        var response = await _client.GetAsync(url);
        await response.ShouldReturnSuccessWithContent(expectedContent);
    }

    public static IEnumerable<object[]> GetMultipleContentSourceExampleUrls()
    {
        yield return ["/", "My Little Content Engine"];
        yield return ["/about", "About"];
        yield return ["/blog/best-pizza-toppings", "pizza"];
        // Note: Skipping /docs routes since DocsFrontMatter is internal and can't be configured in tests
        yield return ["/Portfolio", "Portfolio"];
        yield return ["/Services", "Services"];
    }
}