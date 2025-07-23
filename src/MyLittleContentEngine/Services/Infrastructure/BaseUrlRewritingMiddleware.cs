using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MyLittleContentEngine.Services.Infrastructure;

/// <summary>
/// Middleware that rewrites URLs in HTML responses to handle both xref: cross-references and BaseUrl rewriting.
/// First resolves xref: URLs to their actual targets, then rewrites root-relative URLs to include the configured BaseUrl.
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

    // Pattern to match xref: URLs in href attributes
    private static readonly Regex XrefPattern = XrefRegex();

    public BaseUrlRewritingMiddleware(RequestDelegate next, ContentEngineOptions options)
    {
        _next = next;
        _baseUrl = options.BaseUrl;
    }

    public async Task InvokeAsync(HttpContext context)
    {
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

                var rewrittenContent = await RewriteUrlsAsync(responseContent, context.RequestServices);

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

    private async Task<string> RewriteUrlsAsync(string html, IServiceProvider services)
    {
        // First, resolve any xref: URLs
        var xrefResolver = services.GetService<IXrefResolver>();
        if (xrefResolver != null)
        {
            html = await ResolveXrefsAsync(html, xrefResolver);
        }

        // Then apply BaseUrl rewriting to the result
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

    private async Task<string> ResolveXrefsAsync(string html, IXrefResolver xrefResolver)
    {
        var matches = XrefPattern.Matches(html).Reverse().ToList();
        
        foreach (var match in matches)
        {
            var prefix = match.Groups[1].Value;
            var xrefUid = match.Groups[2].Value;
            var suffix = match.Groups[3].Value;

            var resolvedUrl = await xrefResolver.ResolveAsync(xrefUid);
            if (resolvedUrl != null)
            {
                // Replace the xref: URL with the resolved URL
                var replacement = prefix + resolvedUrl + suffix;
                html = html.Remove(match.Index, match.Length).Insert(match.Index, replacement);
            }
            else
            {
                // Replace unresolved xref with a span containing error message
                // Extract the content between the opening and closing tags from the suffix
                var suffixValue = suffix; // This contains ">Unknown Documentation</a>"
                var contentStartIndex = suffixValue.IndexOf('>') + 1;
                var contentEndIndex = suffixValue.LastIndexOf('<');
                var content = contentEndIndex > contentStartIndex ? 
                    suffixValue.Substring(contentStartIndex, contentEndIndex - contentStartIndex) : 
                    "Reference not found";
                
                var replacement = $"<span data-xref-error=\"Reference not found\" data-xref-uid=\"{xrefUid}\" style=\"color: red; text-decoration: line-through;\">{content}</span>";
                html = html.Remove(match.Index, match.Length).Insert(match.Index, replacement);
            }
        }

        return html;
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
    [GeneratedRegex("""(<(?:a|link)\b[^>]*?\s+href\s*=\s*["'])xref:([^"']*?)(["'][^>]*?>)""", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex XrefRegex();
}