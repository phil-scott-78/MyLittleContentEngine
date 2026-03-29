using Microsoft.AspNetCore.Builder;

namespace MyLittleContentEngine.Services.Infrastructure;

/// <summary>
/// Extension methods for configuring response processing in the pipeline.
/// </summary>
public static class ResponseProcessingExtensions
{
    /// <summary>
    /// Adds <see cref="ResponseProcessingMiddleware"/> to the pipeline, which captures the
    /// response body once and runs all registered <see cref="IResponseProcessor"/> implementations.
    /// Replaces the need for multiple body-capturing middlewares.
    /// </summary>
    public static IApplicationBuilder UseResponseProcessing(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ResponseProcessingMiddleware>();
    }
}
