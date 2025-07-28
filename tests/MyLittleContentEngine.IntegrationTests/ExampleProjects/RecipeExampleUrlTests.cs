using MyLittleContentEngine.IntegrationTests.Infrastructure;

namespace MyLittleContentEngine.IntegrationTests.ExampleProjects;

public class RecipeExampleUrlTests : IClassFixture<RecipeExampleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RecipeExampleUrlTests(RecipeExampleWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [MemberData(nameof(GetRecipeExampleUrls))]
    public async Task RecipeExample_Urls_ShouldReturnSuccessStatusCodes(string url, string expectedContent)
    {
        var response = await _client.GetAsync(url, TestContext.Current.CancellationToken);
        await response.ShouldReturnSuccessWithContent(expectedContent);
    }

    public static IEnumerable<object[]> GetRecipeExampleUrls()
    {
        yield return ["/", "beer"];
        yield return ["/recipes/chili", "chili"];
        yield return ["/recipes/bacon-wrapped-jalapenos", "jalapenos"];
        yield return ["/recipes/beer-cheese", "beer-cheese"];
    }
}