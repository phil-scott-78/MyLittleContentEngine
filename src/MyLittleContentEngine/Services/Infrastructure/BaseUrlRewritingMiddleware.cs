using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Http;

namespace MyLittleContentEngine.Services.Infrastructure;

/// <summary>
/// Middleware that rewrites URLs in HTML responses to handle both xref: cross-references and BaseUrl rewriting.
/// First resolves xref: URLs to their actual targets, then rewrites root-relative URLs to include the configured BaseUrl.
/// This ensures that links work correctly when the site is deployed to a subdirectory.
/// Uses AngleSharp for robust HTML parsing and manipulation.
/// </summary>
public partial class BaseUrlRewritingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IXrefResolver _xrefResolver;
    private readonly string _baseUrl;
    private readonly IBrowsingContext _browsingContext;

    // HTML elements and attributes that contain URLs that should be rewritten
    private static readonly Dictionary<string, string[]> UrlAttributes = new()
    {
        { "a", ["href"] },
        { "link", ["href"] },
        { "img", ["src", "srcset"] },
        { "script", ["src"] },
        { "iframe", ["src"] },
        { "embed", ["src"] },
        { "source", ["src", "srcset"] },
        { "track", ["src"] },
        { "form", ["action"] }
    };

    public BaseUrlRewritingMiddleware(RequestDelegate next, OutputOptions? outputOptions, IXrefResolver xrefResolver)
    {
        _next = next;
        _xrefResolver = xrefResolver;
        _baseUrl = outputOptions?.BaseUrl ?? string.Empty;

        // Configure AngleSharp
        var config = Configuration.Default;
        _browsingContext = BrowsingContext.New(config);
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

                await originalBodyStream.WriteAsync(rewrittenBytes);
            }
            else
            {
                // For non-HTML responses, copy the content as-is
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
        // Parse the HTML using AngleSharp
        var document = await _browsingContext.OpenAsync(req => req.Content(html));

        // First, resolve any xref: URLs and tags
        await ResolveXrefsAsync(document);

        // Add data-base-url attribute to body tag if not already present
        AddDataBaseUrlToBody(document);

        // Then apply BaseUrl rewriting to all URL attributes
        RewriteUrlAttributes(document);

        // Return the modified HTML
        return document.ToHtml();
    }

    private async Task ResolveXrefsAsync(IDocument document)
    {
        // First, handle <xref:uid> tags and convert them to <a> links
        await ResolveXrefTagsAsync(document);

        // Second, handle <a href="xref:uid">xref:uid</a> links where href and content match
        await ResolveXrefLinksAsync(document);

        // Then handle existing xref: URLs in href attributes
        var xrefLinks = document.QuerySelectorAll("a[href^='xref:']").Cast<IHtmlAnchorElement>().ToList();

        foreach (var link in xrefLinks)
        {
            var href = link.GetAttribute("href");
            if (href?.StartsWith("xref:") == true)
            {
                var xrefUid = href[5..]; // Remove "xref:" prefix
                var resolvedUrl = await _xrefResolver.ResolveAsync(xrefUid);

                if (resolvedUrl != null)
                {
                    // Replace the xref: URL with the resolved URL
                    link.Href = resolvedUrl;
                }
                else
                {
                    // Replace unresolved xref with a span containing error message
                    var content = link.TextContent;
                    var errorSpan = document.CreateElement("a");
                    errorSpan.SetAttribute("data-xref-error", "Reference not found");
                    errorSpan.SetAttribute("data-xref-uid", xrefUid);
                    errorSpan.SetAttribute("style", "color: red; text-decoration: line-through;");
                    errorSpan.SetAttribute("href", href);
                    errorSpan.TextContent = content;

                    link.Parent?.ReplaceChild(errorSpan, link);
                }
            }
        }
    }

    private async Task ResolveXrefTagsAsync(IDocument document)
    {
        // Find all text nodes that contain <xref:uid> patterns and process them
        // We'll do this by looking at the entire document content and replacing the patterns
        var htmlContent = document.DocumentElement.InnerHtml;

        var startIndex = 0;
        var modificationsMade = false;

        while ((startIndex = htmlContent.IndexOf("<xref:", startIndex, StringComparison.Ordinal)) >= 0)
        {
            var endIndex = htmlContent.IndexOf('>', startIndex);
            if (endIndex == -1) break;

            var xrefTag = htmlContent.Substring(startIndex, endIndex - startIndex + 1);
            var xrefUid = htmlContent.Substring(startIndex + 6, endIndex - startIndex - 6); // Remove "<xref:" and ">"

            var crossRef = await _xrefResolver.ResolveToReferenceAsync(xrefUid);
            string replacement;

            // Create an <a> tag with the resolved URL and title
            replacement = crossRef != null
                ? $"<a href=\"{crossRef.Url}\">{crossRef.Title}</a>" 
                : $"<span data-xref-error=\"Reference not found\" data-xref-uid=\"{xrefUid}\" class=\"text-red-500\">Reference not found: {xrefUid}</span>";

            htmlContent = htmlContent.Replace(xrefTag, replacement);
            modificationsMade = true;
            startIndex = endIndex + 1;
        }

        if (modificationsMade)
        {
            document.DocumentElement.InnerHtml = htmlContent;
        }
    }

    private async Task ResolveXrefLinksAsync(IDocument document)
    {
        // Find <a href="xref:uid">xref:uid</a> links where href and content match
        var xrefLinks = document.QuerySelectorAll("a[href^='xref:']").Cast<IHtmlAnchorElement>()
            .Where(link => link.Href.StartsWith("xref:") &&
                          link.TextContent.StartsWith("xref:") &&
                          link.Href == link.TextContent)
            .ToList();

        foreach (var link in xrefLinks)
        {
            var href = link.GetAttribute("href");
            if (href?.StartsWith("xref:") == true)
            {
                var xrefUid = href[5..]; // Remove "xref:" prefix
                var crossRef = await _xrefResolver.ResolveToReferenceAsync(xrefUid);

                if (crossRef != null)
                {
                    // Replace the entire <a href="xref:uid">xref:uid</a> with resolved link
                    link.Href = crossRef.Url;
                    link.TextContent = crossRef.Title;
                }
                else
                {
                    // Replace unresolved xref with a span containing error message
                    var errorSpan = document.CreateElement("span");
                    errorSpan.SetAttribute("data-xref-error", "Reference not found");
                    errorSpan.SetAttribute("data-xref-uid", xrefUid);
                    errorSpan.SetAttribute("style", "color: red; text-decoration: line-through;");
                    errorSpan.TextContent = $"Reference not found: {xrefUid}";

                    link.Parent?.ReplaceChild(errorSpan, link);
                }
            }
        }
    }

    private void AddDataBaseUrlToBody(IDocument document)
    {
        var body = document.Body;
        if (body?.HasAttribute("data-base-url") == false && !string.IsNullOrEmpty(_baseUrl))
        {
            body.SetAttribute("data-base-url", _baseUrl);
        }
    }

    private void RewriteUrlAttributes(IDocument document)
    {
        foreach (var elementType in UrlAttributes.Keys)
        {
            var elements = document.QuerySelectorAll(elementType);

            foreach (var element in elements)
            {
                var attributes = UrlAttributes[elementType];

                foreach (var attributeName in attributes)
                {
                    var attributeValue = element.GetAttribute(attributeName);
                    if (string.IsNullOrEmpty(attributeValue)) continue;

                    if (attributeName == "srcset")
                    {
                        // Handle srcset attributes specially
                        var rewrittenSrcset = RewriteSrcsetValue(attributeValue);
                        if (rewrittenSrcset != attributeValue)
                        {
                            element.SetAttribute(attributeName, rewrittenSrcset);
                        }
                    }
                    else if (attributeName.StartsWith("data-") && attributeValue.StartsWith('/'))
                    {
                        // Handle data attributes that contain URLs
                        var rewrittenUrl = RewriteUrl(attributeValue);
                        if (rewrittenUrl != attributeValue)
                        {
                            element.SetAttribute(attributeName, rewrittenUrl);
                        }
                    }
                    else if (attributeValue.StartsWith('/'))
                    {
                        // Handle regular URL attributes
                        var rewrittenUrl = RewriteUrl(attributeValue);
                        if (rewrittenUrl != attributeValue)
                        {
                            element.SetAttribute(attributeName, rewrittenUrl);
                        }
                    }
                }

                // Handle data attributes that might contain URLs
                foreach (var attr in element.Attributes.Where(a => a.Name.StartsWith("data-") && a.Value.StartsWith('/')))
                {
                    var rewrittenUrl = RewriteUrl(attr.Value);
                    if (rewrittenUrl != attr.Value)
                    {
                        element.SetAttribute(attr.Name, rewrittenUrl);
                    }
                }

                // Handle CSS url() functions in style attributes
                var styleAttr = element.GetAttribute("style");
                if (!string.IsNullOrEmpty(styleAttr))
                {
                    var rewrittenStyle = RewriteCssUrls(styleAttr);
                    if (rewrittenStyle != styleAttr)
                    {
                        element.SetAttribute("style", rewrittenStyle);
                    }
                }
            }
        }

        // Handle CSS @import statements and url() functions in <style> elements
        var styleElements = document.QuerySelectorAll("style");
        foreach (var styleElement in styleElements)
        {
            var originalCss = styleElement.TextContent;
            if (!string.IsNullOrEmpty(originalCss))
            {
                var rewrittenCss = RewriteCssUrls(originalCss);
                if (rewrittenCss != originalCss)
                {
                    styleElement.TextContent = rewrittenCss;
                }
            }
        }
    }

    private string RewriteSrcsetValue(string srcsetValue)
    {
        // Normalize line endings and whitespace to prevent HTML encoding issues
        // Replace any sequence of whitespace that includes newlines with a single space
        var normalizedSrcset = WebUtility.HtmlDecode(srcsetValue).ReplaceLineEndings(" ");

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
            if (url.StartsWith('/'))
            {
                url = RewriteUrl(url);
            }

            rewrittenSources.Add(url + descriptor);
        }

        return string.Join(", ", rewrittenSources);
    }

    private string RewriteUrl(string url)
    {
        // Skip if it's not a root-relative URL (doesn't start with /)
        if (!url.StartsWith('/')) return url;

        // Skip if it already contains the base URL
        if (url.StartsWith(_baseUrl, StringComparison.OrdinalIgnoreCase)) return url;

        // Rewrite the URL
        return PrependBaseUrl(url);
    }

    private string RewriteCssUrls(string css)
    {
        // Handle CSS url() functions
        css = CssUrlRewriteRegex().Replace(css, match =>
        {
            var url = match.Groups[1].Value;
            var rewrittenUrl = RewriteUrl(url);
            return match.Value.Replace(url, rewrittenUrl);
        });

        // Handle CSS @import statements
        css = CssUrlImportRegex().Replace(css, match =>
        {
            var url = match.Groups[1].Value;
            var rewrittenUrl = RewriteUrl(url);
            return match.Value.Replace(url, rewrittenUrl);
        });

        return css;
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

    [GeneratedRegex("""url\s*\(\s*["']?(/[^"')]*?)["']?\s*\)""", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex CssUrlRewriteRegex();
    [GeneratedRegex("""@import\s+["'](/[^"']*?)["']""", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex CssUrlImportRegex();
}