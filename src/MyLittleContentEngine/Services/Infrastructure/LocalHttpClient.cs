namespace MyLittleContentEngine.Services.Infrastructure;

/// <summary>
/// Production implementation of ILocalHttpClient that wraps HttpClient
/// </summary>
public class LocalHttpClient : ILocalHttpClient, IDisposable
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of LocalHttpClient
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HttpClient instances</param>
    public LocalHttpClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    /// <inheritdoc />
    public Uri? BaseAddress
    {
        get => _httpClient.BaseAddress;
        set => _httpClient.BaseAddress = value;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetAsync(string requestUrl)
    {
        return _httpClient.GetAsync(requestUrl);
    }

    /// <summary>
    /// Disposes the underlying HttpClient
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
    }
}