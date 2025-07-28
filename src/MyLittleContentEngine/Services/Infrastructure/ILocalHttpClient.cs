namespace MyLittleContentEngine.Services.Infrastructure;

/// <summary>
/// Abstraction for HTTP client operations to enable testing without requiring a running server
/// </summary>
public interface ILocalHttpClient
{
    /// <summary>
    /// Gets or sets the base address for HTTP requests
    /// </summary>
    Uri? BaseAddress { get; set; }

    /// <summary>
    /// Sends a GET request to the specified URL
    /// </summary>
    /// <param name="requestUrl">The URL to request</param>
    /// <returns>HTTP response message</returns>
    Task<HttpResponseMessage> GetAsync(string requestUrl);
}