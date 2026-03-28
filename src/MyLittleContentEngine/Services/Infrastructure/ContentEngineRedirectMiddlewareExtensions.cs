using Microsoft.AspNetCore.Builder;

namespace MyLittleContentEngine.Services.Infrastructure;

internal static class ContentEngineRedirectMiddlewareExtensions
{
    internal static IApplicationBuilder UseContentEngineRedirects(this IApplicationBuilder app)
        => app.UseMiddleware<ContentEngineRedirectMiddleware>();
}
