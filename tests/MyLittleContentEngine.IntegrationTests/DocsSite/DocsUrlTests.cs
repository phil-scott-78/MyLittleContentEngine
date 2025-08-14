using MyLittleContentEngine.IntegrationTests.Infrastructure;

namespace MyLittleContentEngine.IntegrationTests.DocsSite;

public class DocsUrlTests : IClassFixture<DocsWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DocsUrlTests(DocsWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [MemberData(nameof(GetDocsUrlTestData))]
    public async Task DocsUrls_ShouldReturnSuccessWithExpectedContent(string url, string expectedContent)
    {
        // Act
        var response = await _client.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        await response.ShouldReturnSuccessWithContent(expectedContent);
    }

    public static IEnumerable<object[]> GetDocsUrlTestData()
    {
        yield return ["/", "My Little Content Engine"];
        yield return ["/getting-started/creating-first-site", "Creating"];
        yield return ["/api", "API Reference"];
    }
}