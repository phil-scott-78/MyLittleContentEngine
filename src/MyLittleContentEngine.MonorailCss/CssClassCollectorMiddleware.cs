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

            // Only process HTML and JSON responses
            var contentType = context.Response.ContentType;
            var isHtml = contentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true;
            var isJson = contentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true;

            if (!isHtml && !isJson)
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(originalBodyStream);
                return;
            }

            logger.LogTrace("Gathering CSS from {ContentType} response for {Url}", isJson ? "JSON" : "HTML", url);
            try
            {
                collector.BeginProcessing();

                memoryStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

                // JSON responses contain HTML with escaped quotes. JavaScriptEncoder.Default
                // encodes " as \u0022 and may also produce \". Unescape both forms so the
                // class-attribute regex can match.
                var textToScan = isJson
                    ? JsonUnescapeRegex().Replace(responseBody, m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString())
                        .Replace("\\\"", "\"")
                    : responseBody;

                var classMatches = CssClassGatherRegex().Matches(textToScan);
                var allClasses = classMatches
                    .SelectMany(m => m.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    .Distinct()
                    .ToList();

                logger.LogTrace("Gathered {Count} CSS classes", allClasses.Count);
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

    [GeneratedRegex("""\\u([0-9a-fA-F]{4})""")]
    private static partial Regex JsonUnescapeRegex();
}