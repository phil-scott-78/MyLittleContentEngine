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
    private readonly IXrefResolver _xrefResolver;
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

    // Special pattern for srcset attributes which contain multiple URLs with descriptors
    private static readonly Regex SrcsetPattern = SrcsetRegex();

    // Pattern to match xref: URLs in href attributes
    private static readonly Regex XrefPattern = XrefRegex();
    
    // Pattern to match <xref: tags (e.g., <xref:docs.guides.linking-documents-and-media>)
    private static readonly Regex XrefTagPattern = XrefTagRegex();
    
    // Pattern to match <a href="xref:uid">xref:uid</a> where href and content must match
    private static readonly Regex XrefLinkPattern = XrefLinkRegex();

    public BaseUrlRewritingMiddleware(RequestDelegate next, OutputOptions? outputOptions, IXrefResolver xrefResolver)
    {
        _next = next;
        _xrefResolver = xrefResolver;
        _baseUrl = outputOptions?.BaseUrl ?? string.Empty;
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

                var rewrittenContent = await RewriteUrlsAsync(responseContent);

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

    private async Task<string> RewriteUrlsAsync(string html)
    {
        // First, resolve any xref: URLs
        html = await ResolveXrefsAsync(html);

        // Handle srcset attributes separately since they contain multiple URLs
        html = RewriteSrcsetUrls(html);

        // Then apply BaseUrl rewriting to the result for other URL patterns
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

    private async Task<string> ResolveXrefsAsync(string html)
    {
        // First, handle <xref:uid> tags and convert them to <a> links
        html = await ResolveXrefTagsAsync(html);
        
        // Second, handle <a href="xref:uid">xref:uid</a> links where href and content match
        html = await ResolveXrefLinksAsync(html);
        
        // Then handle existing xref: URLs in href attributes
        var matches = XrefPattern.Matches(html).Reverse().ToList();
        
        foreach (var match in matches)
        {
            var prefix = match.Groups[1].Value;
            var xrefUid = match.Groups[2].Value;
            var suffix = match.Groups[3].Value;

            var resolvedUrl = await _xrefResolver.ResolveAsync(xrefUid);
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

    private async Task<string> ResolveXrefTagsAsync(string html)
    {
        var matches = XrefTagPattern.Matches(html).Reverse().ToList();
        
        foreach (var match in matches)
        {
            var xrefUid = match.Groups[1].Value;

            var crossRef = await _xrefResolver.ResolveToReferenceAsync(xrefUid);
            if (crossRef != null)
            {
                // Create an <a> tag with the resolved URL and title
                var replacement = $"<a href=\"{crossRef.Url}\">{crossRef.Title}</a>";
                html = html.Remove(match.Index, match.Length).Insert(match.Index, replacement);
            }
            else
            {
                // Replace unresolved xref with a span containing error message
                var replacement = $"<span data-xref-error=\"Reference not found\" data-xref-uid=\"{xrefUid}\" style=\"color: red; text-decoration: line-through;\">Reference not found: {xrefUid}</span>";
                html = html.Remove(match.Index, match.Length).Insert(match.Index, replacement);
            }
        }

        return html;
    }

    private async Task<string> ResolveXrefLinksAsync(string html)
    {
        var matches = XrefLinkPattern.Matches(html).Reverse().ToList();
        
        foreach (var match in matches)
        {
            var xrefUid = match.Groups[1].Value;

            var crossRef = await _xrefResolver.ResolveToReferenceAsync(xrefUid);
            if (crossRef != null)
            {
                // Replace the entire <a href="xref:uid">xref:uid</a> with resolved link
                var replacement = $"<a href=\"{crossRef.Url}\">{crossRef.Title}</a>";
                html = html.Remove(match.Index, match.Length).Insert(match.Index, replacement);
            }
            else
            {
                // Replace unresolved xref with a span containing error message
                var replacement = $"<span data-xref-error=\"Reference not found\" data-xref-uid=\"{xrefUid}\" style=\"color: red; text-decoration: line-through;\">Reference not found: {xrefUid}</span>";
                html = html.Remove(match.Index, match.Length).Insert(match.Index, replacement);
            }
        }

        return html;
    }

    private string RewriteSrcsetUrls(string html)
    {
        return SrcsetPattern.Replace(html, match =>
        {
            var prefix = match.Groups[1].Value; // Everything before srcset value
            var srcsetValue = match.Groups[2].Value; // The srcset attribute value
            var suffix = match.Groups[3].Value; // Everything after srcset value

            // Process each URL in the srcset value
            var rewrittenSrcset = RewriteSrcsetValue(srcsetValue);
            
            return prefix + rewrittenSrcset + suffix;
        });
    }

    private string RewriteSrcsetValue(string srcsetValue)
    {
        // Normalize line endings and whitespace to prevent HTML encoding issues
        // Replace any sequence of whitespace that includes newlines with a single space
        var normalizedSrcset = System.Text.RegularExpressions.Regex.Replace(srcsetValue, @"\s*\n\s*", " ");
        
        // Split by comma to get individual source entries
        var sources = normalizedSrcset.Split(',');
        var rewrittenSources = new List<string>();

        foreach (var source in sources)
        {
            var trimmedSource = source.Trim();
            if (string.IsNullOrEmpty(trimmedSource)) continue;

            // Split by whitespace to separate URL from descriptor (e.g., "480w", "2x")
            var parts = trimmedSource.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            var url = parts[0];
            var descriptor = parts.Length > 1 ? " " + string.Join(" ", parts[1..]) : "";

            // Only rewrite root-relative URLs that don't already contain the base URL
            // Use the same logic as PrependBaseUrl to normalize the base URL for comparison
            if (url.StartsWith('/'))
            {
                var normalizedBaseUrl = _baseUrl.StartsWith('/') ? _baseUrl : "/" + _baseUrl;
                if (normalizedBaseUrl.EndsWith('/') && normalizedBaseUrl.Length > 1)
                {
                    normalizedBaseUrl = normalizedBaseUrl[..^1];
                }
                
                // Only skip rewriting if the URL already starts with the normalized base URL
                // and the base URL is not just "/" (root)
                if (normalizedBaseUrl != "/" && url.StartsWith(normalizedBaseUrl, StringComparison.OrdinalIgnoreCase))
                {
                    // URL already contains base URL, don't rewrite
                }
                else
                {
                    url = PrependBaseUrl(url);
                }
            }

            rewrittenSources.Add(url + descriptor);
        }

        return string.Join(", ", rewrittenSources);
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
    [GeneratedRegex("""<xref:([^>]+)>""", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex XrefTagRegex();
    [GeneratedRegex("""<a\b[^>]*?\s+href\s*=\s*["']xref:([^"']*?)["'][^>]*?>xref:\1</a>""", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex XrefLinkRegex();
    [GeneratedRegex("""(<img\b[^>]*?\s+srcset\s*=\s*["'])([^"']*?)(['"][^>]*?>)""", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex SrcsetRegex();
}