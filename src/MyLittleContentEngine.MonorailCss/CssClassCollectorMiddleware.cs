using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MyLittleContentEngine.MonorailCss;

public partial class CssClassCollectorMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, CssClassCollector collector, ILogger<CssClassCollectorMiddleware> logger)
    {
        var url = context.Request.Path;

        if (!collector.ShouldProcess(url))
        {
            await next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;
        
        try
        {
            await using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            await next(context); // Run the rest of the pipeline
            
            // Make sure the response is HTML before proceeding
            var contentType = context.Response.ContentType;
            if (string.IsNullOrEmpty(contentType) || !contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(originalBodyStream);
                return;
            }
            
            logger.LogTrace("Gathering CSS for {url}", url);
            try
            {
                collector.BeginProcessing();

                memoryStream.Seek(0, SeekOrigin.Begin);
                var html = await new StreamReader(memoryStream).ReadToEndAsync();

                var classMatches = CssClassGatherRegex().Matches(html);
                var allClasses = classMatches
                    .SelectMany(m => m.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    .Distinct()
                    .ToList();

                logger.LogTrace("Gathered {count} CSS classes", allClasses.Count());
                collector.AddClasses(url, allClasses);
            }
            finally
            {
                collector.EndProcessing();

            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            await memoryStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            // Always restore the original stream, even when exceptions occur
            context.Response.Body = originalBodyStream;
        }
    }

    [GeneratedRegex("""class\s*=\s*["']([^"']+)["']""", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex CssClassGatherRegex();
}