using MyLittleContentEngine.Services.Infrastructure;

namespace MyLittleContentEngine.IntegrationTests.Infrastructure;

/// <summary>
/// Test implementation of ILocalHttpClient that uses a WebApplicationFactory's TestServer
/// instead of making external HTTP requests
/// </summary>
public class TestServerLocalHttpClient : ILocalHttpClient
{
    private readonly HttpClient _testHttpClient;

    /// <summary>
    /// Initializes a new instance using the TestServer's HttpClient
    /// </summary>
    /// <param name="testHttpClient">HttpClient from WebApplicationFactory</param>
    public TestServerLocalHttpClient(HttpClient testHttpClient)
    {
        _testHttpClient = testHttpClient ?? throw new ArgumentNullException(nameof(testHttpClient));
    }

    /// <inheritdoc />
    public Uri? BaseAddress
    {
        get => _testHttpClient.BaseAddress;
        set => _testHttpClient.BaseAddress = value;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetAsync(string requestUrl)
    {
        return _testHttpClient.GetAsync(requestUrl);
    }
}