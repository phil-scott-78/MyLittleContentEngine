using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MyLittleContentEngine.Services.Infrastructure;

/// <summary>
/// Extension methods for registering the WordBreakingMiddleware.
/// </summary>
public static class WordBreakingMiddlewareExtensions
{
    /// <summary>
    /// Adds the WordBreakingMiddleware to the application pipeline with default options.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseWordBreaking(this IApplicationBuilder app)
    {
        return app.UseWordBreaking(new WordBreakingMiddlewareOptions());
    }

    /// <summary>
    /// Adds the WordBreakingMiddleware to the application pipeline with custom options.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="options">Configuration options for the middleware</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseWordBreaking(this IApplicationBuilder app, WordBreakingMiddlewareOptions options)
    {
        return app.UseMiddleware<WordBreakingMiddleware>(options);
    }

    /// <summary>
    /// Adds the WordBreakingMiddleware to the application pipeline with configuration action.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="configureOptions">Action to configure the middleware options</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseWordBreaking(this IApplicationBuilder app, Action<WordBreakingMiddlewareOptions> configureOptions)
    {
        var options = new WordBreakingMiddlewareOptions();
        configureOptions(options);
        return app.UseWordBreaking(options);
    }
}