using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MyLittleContentEngine.Services.Infrastructure;

/// <summary>
/// Extension methods for configuring the BaseUrlRewritingMiddleware.
/// </summary>
public static class BaseUrlRewritingMiddlewareExtensions
{
    /// <summary>
    /// Adds BaseUrlRewritingMiddleware to the application pipeline.
    /// This middleware automatically rewrites root-relative URLs in HTML responses
    /// to include the configured BaseUrl from OutputOptions.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for method chaining</returns>
    /// <remarks>
    /// <para>
    /// This middleware should be added early in the pipeline, typically before
    /// static file middleware and routing middleware, to ensure all HTML content
    /// is processed.
    /// </para>
    /// <para>
    /// The middleware only processes HTML responses (Content-Type: text/html)
    /// and only when OutputOptions.BaseUrl is set to something other than an empty string.
    /// </para>
    /// <para>
    /// Example usage:
    /// </para>
    /// <code>
    /// app.UseBaseUrlRewriting();
    /// app.UseStaticFiles();
    /// app.UseRouting();
    /// </code>
    /// </remarks>
    public static IApplicationBuilder UseBaseUrlRewriting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<BaseUrlRewritingMiddleware>();
    }
}