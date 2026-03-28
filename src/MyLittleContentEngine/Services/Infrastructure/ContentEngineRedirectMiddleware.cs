using Microsoft.AspNetCore.Http;
using MyLittleContentEngine.Services.Content;

namespace MyLittleContentEngine.Services.Infrastructure;

/// <summary>
/// Middleware that issues HTTP 301 redirects for paths configured in _redirects.yml.
/// Works at both runtime (<c>dotnet run</c>) and during static build, where
/// <c>OutputGenerationService</c> intercepts the 301 and writes the redirect HTML file.
/// </summary>
internal sealed class ContentEngineRedirectMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context,
        RedirectContentService redirectService,
        OutputOptions outputOptions)
    {
        var mappings = await redirectService.GetRedirectMappingsAsync();

        var path = context.Request.Path.Value?.TrimEnd('/') ?? "/";
        if (path == "") path = "/";

        if (mappings.TryGetValue(path, out var target))
        {
            var finalTarget = LinkRewriter.RewriteUrl(target, "/", outputOptions.BaseUrl);
            context.Response.StatusCode = 301;
            context.Response.Headers.Location = finalTarget;
            return;
        }

        await next(context);
    }
}
