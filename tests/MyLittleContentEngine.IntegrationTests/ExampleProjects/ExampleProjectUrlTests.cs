using MyLittleContentEngine.IntegrationTests.Infrastructure;

namespace MyLittleContentEngine.IntegrationTests.ExampleProjects;

public class MinimalExampleUrlTests : IClassFixture<MinimalExampleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MinimalExampleUrlTests(MinimalExampleWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [MemberData(nameof(GetMinimalExampleUrlTestData))]
    public async Task MinimalExample_ShouldReturnSuccessWithExpectedContent(string url, string expectedContent)
    {
        // Act
        var response = await _client.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldReturnSuccessWithContent(expectedContent);
    }

    public static IEnumerable<object[]> GetMinimalExampleUrlTestData()
    {
        yield return ["/", "My Little Content Engine"];
        yield return ["/sub-folder/page-one", "Welcome"];
    }
}

public class BlogExampleUrlTests : IClassFixture<BlogExampleWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BlogExampleUrlTests(BlogExampleWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [MemberData(nameof(GetBlogExampleUrlTestData))]
    public async Task BlogExample_ShouldReturnSuccessWithExpectedContent(string url, string expectedContent)
    {
        // Act
        var response = await _client.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldReturnSuccessWithContent(expectedContent);
    }

    public static IEnumerable<object[]> GetBlogExampleUrlTestData()
    {
        yield return ["/", "Calvin"];
        yield return ["/blog/2024/03/chewing-magazine-review", "Calvin"];
    }
}