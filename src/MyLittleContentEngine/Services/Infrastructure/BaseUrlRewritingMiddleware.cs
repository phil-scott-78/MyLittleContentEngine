using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace MyLittleContentEngine.Services.Infrastructure;

/// <summary>
/// Middleware that rewrites root-relative URLs in HTML responses to include the configured BaseUrl.
/// This ensures that links work correctly when the site is deployed to a subdirectory.
/// </summary>
public partial class BaseUrlRewritingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _baseUrl;

    // Patterns to match various HTML elements with URLs
    private static readonly Regex[] UrlPatterns =
    [
        // href attributes in <a> and <link> tags
        HrefRegex(),

        // src attributes in <img>, <script>, <iframe>, <embed>, <source>, <track> tags
        SrcRegex(),

        // action attributes in <form> tags
        FormActionRegex(),

        // data attributes that might contain URLs
        DataAttributeRegex(),

        // CSS url() functions in style attributes
        CssUrlRegex(),

        // CSS @import statements
        CssImportRegex()
    ];

    public BaseUrlRewritingMiddleware(RequestDelegate next, ContentEngineOptions options)
    {
        _next = next;
        _baseUrl = options.BaseUrl;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process if BaseUrl is set and not just "/"
        if (string.IsNullOrWhiteSpace(_baseUrl) || _baseUrl == "/")
        {
            await _next(context);
            return;
        }

        // Capture the original response stream
        var originalBodyStream = context.Response.Body;

        try
        {
            await using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // Only process HTML responses
            if (ShouldProcessResponse(context.Response))
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseContent = await new StreamReader(responseBody).ReadToEndAsync();

                var rewrittenContent = RewriteUrls(responseContent);

                var rewrittenBytes = Encoding.UTF8.GetBytes(rewrittenContent);
                context.Response.ContentLength = rewrittenBytes.Length;

                await originalBodyStream.WriteAsync(rewrittenBytes);
            }
            else
            {
                // For non-HTML responses, just copy the content as-is
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private static bool ShouldProcessResponse(HttpResponse response)
    {
        // Only process successful responses
        if (response.StatusCode is < 200 or >= 300)
            return false;

        // Check content type
        var contentType = response.ContentType;
        if (string.IsNullOrEmpty(contentType))
            return false;

        // Process HTML responses
        return contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private string RewriteUrls(string html)
    {
        return UrlPatterns.Aggregate(html, (current, pattern) => pattern.Replace(current, match =>
        {
            var prefix = match.Groups[1].Value;
            var url = match.Groups[2].Value;
            var suffix = match.Groups[3].Value;

            // Skip if it's not a root-relative URL (doesn't start with /)
            if (!url.StartsWith('/')) return match.Value;

            // Skip if it already contains the base URL
            if (url.StartsWith(_baseUrl, StringComparison.OrdinalIgnoreCase)) return match.Value;

            // Rewrite the URL
            var rewrittenUrl = PrependBaseUrl(url);
            return prefix + rewrittenUrl + suffix;
        }));
    }

    private string PrependBaseUrl(string path)
    {
        // Normalize BaseUrl: ensure it starts with / but doesn't end with /
        var normalizedBaseUrl = _baseUrl.StartsWith('/') ? _baseUrl : "/" + _baseUrl;
        if (normalizedBaseUrl.EndsWith('/') && normalizedBaseUrl.Length > 1)
        {
            normalizedBaseUrl = normalizedBaseUrl[..^1];
        }

        // Special case: if BaseUrl is just "/" (root), don't prepend anything
        if (normalizedBaseUrl == "/")
        {
            return path;
        }

        // If path is just "/", return BaseUrl with trailing slash
        if (path == "/")
        {
            return normalizedBaseUrl + "/";
        }

        // Combine BaseUrl with path
        return normalizedBaseUrl + path;
    }

    [GeneratedRegex("""(<(?:a|link)\b[^>]*?\s+href\s*=\s*["'])(/[^"']*?)(["'][^>]*?>)""", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex HrefRegex();
    [GeneratedRegex("""(<(?:img|script|iframe|embed|source|track)\b[^>]*?\s+src\s*=\s*["'])(/[^"']*?)(["'][^>]*?>)""", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex SrcRegex();
    [GeneratedRegex("""(<form\b[^>]*?\s+action\s*=\s*["'])(/[^"']*?)(["'][^>]*?>)""", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex FormActionRegex();
    [GeneratedRegex("""(<[^>]*?\s+data-[^=]*?\s*=\s*["'])(/[^"']*?)(["'][^>]*?>)""", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex DataAttributeRegex();
    [GeneratedRegex("""(url\s*\(\s*["']?)(/[^"')]*?)(["']?\s*\))""", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex CssUrlRegex();
    [GeneratedRegex("""(@import\s+["'])(/[^"']*?)(["'])""", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex CssImportRegex();
}