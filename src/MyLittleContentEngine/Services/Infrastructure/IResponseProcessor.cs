using Microsoft.AspNetCore.Http;

namespace MyLittleContentEngine.Services.Infrastructure;

/// <summary>
/// Processes HTTP response bodies for a specific concern (URL rewriting, CSS class extraction, etc.).
/// Implementations are run in <see cref="Order"/> sequence by <see cref="ResponseProcessingMiddleware"/>,
/// which captures the response body once and passes it through each processor.
/// </summary>
public interface IResponseProcessor
{
    /// <summary>
    /// Execution priority. Lower values run first.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Whether this processor should run for the given response.
    /// Called after the inner pipeline has produced a response.
    /// </summary>
    bool ShouldProcess(HttpContext context);

    /// <summary>
    /// Process the response body and return the (possibly modified) result.
    /// Processors that only observe (e.g. CSS class extraction) should return
    /// <paramref name="responseBody"/> unchanged.
    /// </summary>
    Task<string> ProcessAsync(string responseBody, HttpContext context);
}
