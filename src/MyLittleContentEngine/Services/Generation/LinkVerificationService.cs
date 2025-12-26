using System.Collections.Immutable;
using AngleSharp;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Models;

namespace MyLittleContentEngine.Services.Generation;

/// <summary>
/// Service for verifying internal links in generated HTML during static site generation.
/// Parses HTML content to extract links and validates them against a set of known valid pages.
/// </summary>
/// <param name="logger">Logger for diagnostic output</param>
internal class LinkVerificationService(ILogger<LinkVerificationService> logger)
{
    private readonly IBrowsingContext _browsingContext = BrowsingContext.New(Configuration.Default);

    // HTML elements and their URL-containing attributes that should be validated
    private static readonly Dictionary<string, (string[] Attributes, LinkType Type)> UrlElementsToValidate = new()
    {
        { "a", (["href"], LinkType.Href) },
        { "link", (["href"], LinkType.Href) },
        { "img", (["src", "srcset"], LinkType.Src) },
        { "script", (["src"], LinkType.Src) },
        { "iframe", (["src"], LinkType.Src) },
        { "embed", (["src"], LinkType.Src) },
        { "source", (["src", "srcset"], LinkType.Src) },
        { "track", (["src"], LinkType.Src) },
        { "form", (["action"], LinkType.Href) }
    };

    /// <summary>
    /// Validates all internal links in the provided HTML content.
    /// </summary>
    /// <param name="htmlContent">The HTML content to validate</param>
    /// <param name="sourcePage">The URL path of the page being validated</param>
    /// <param name="validPages">Set of all valid page paths in the site</param>
    /// <param name="baseUrl">The base URL for the site (used for subfolder deployments)</param>
    /// <returns>A list of broken links found in the HTML content</returns>
    public async Task<ImmutableList<BrokenLink>> ValidateLinksAsync(
        string htmlContent,
        UrlPath sourcePage,
        ImmutableHashSet<string> validPages,
        UrlPath baseUrl)
    {
        var brokenLinks = ImmutableList.CreateBuilder<BrokenLink>();

        // Parse the HTML using AngleSharp
        var document = await _browsingContext.OpenAsync(req => req.Content(htmlContent));

        // Extract and validate links from all configured element types
        foreach (var (elementType, (attributes, linkType)) in UrlElementsToValidate)
        {
            var elements = document.QuerySelectorAll(elementType);

            foreach (var element in elements)
            {
                foreach (var attributeName in attributes)
                {
                    var attributeValue = element.GetAttribute(attributeName);
                    if (string.IsNullOrWhiteSpace(attributeValue))
                    {
                        continue;
                    }

                    // Special handling for srcset attribute (can contain multiple URLs)
                    if (attributeName == "srcset")
                    {
                        var srcsetUrls = ParseSrcsetAttribute(attributeValue);
                        foreach (var url in srcsetUrls)
                        {
                            ValidateSingleLink(url, sourcePage, validPages, baseUrl, elementType, linkType, brokenLinks);
                        }
                    }
                    else
                    {
                        ValidateSingleLink(attributeValue, sourcePage, validPages, baseUrl, elementType, linkType, brokenLinks);
                    }
                }
            }
        }

        return brokenLinks.ToImmutable();
    }

    /// <summary>
    /// Validates a single link and adds it to the broken links collection if invalid.
    /// </summary>
    private void ValidateSingleLink(
        string link,
        UrlPath sourcePage,
        ImmutableHashSet<string> validPages,
        UrlPath baseUrl,
        string elementType,
        LinkType linkType,
        ImmutableList<BrokenLink>.Builder brokenLinks)
    {
        // Skip external links, data URLs, and anchor-only links
        if (!IsInternalLink(link))
        {
            return;
        }

        // Normalize the link (strip query strings, anchors, BaseUrl prefix)
        var normalizedLink = NormalizeLink(link, baseUrl);

        // Check if the link exists in the valid pages set
        if (!validPages.Contains(normalizedLink))
        {
            logger.LogDebug("Broken link found: {link} in page {sourcePage}", link, sourcePage.Value);
            brokenLinks.Add(new BrokenLink(sourcePage, link, linkType, elementType));
        }
    }

    /// <summary>
    /// Determines if a link is an internal link (not external, not a special protocol).
    /// </summary>
    private static bool IsInternalLink(string link)
    {
        // External protocols to skip
        if (link.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            link.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            link.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            link.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
            link.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
            link.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Skip anchor-only links
        if (link.StartsWith('#'))
        {
            return false;
        }

        // Skip empty links
        if (string.IsNullOrWhiteSpace(link))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Normalizes a link by stripping query strings, anchors, and BaseUrl prefixes.
    /// </summary>
    private static string NormalizeLink(string link, UrlPath baseUrl)
    {
        // Strip query string
        var queryIndex = link.IndexOf('?');
        if (queryIndex >= 0)
        {
            link = link[..queryIndex];
        }

        // Strip fragment/anchor
        var anchorIndex = link.IndexOf('#');
        if (anchorIndex >= 0)
        {
            link = link[..anchorIndex];
        }

        // Remove BaseUrl prefix if present
        if (!string.IsNullOrEmpty(baseUrl.Value) && baseUrl.Value != "/")
        {
            var baseUrlNormalized = baseUrl.Value.TrimEnd('/');
            if (link.StartsWith(baseUrlNormalized, StringComparison.OrdinalIgnoreCase))
            {
                link = link[baseUrlNormalized.Length..];
            }
        }

        // Ensure it starts with /
        if (!link.StartsWith('/'))
        {
            link = "/" + link;
        }

        return link;
    }

    /// <summary>
    /// Parses a srcset attribute value and extracts all URLs.
    /// Format: "url1 descriptor1, url2 descriptor2"
    /// Example: "image-480w.jpg 480w, image-800w.jpg 800w"
    /// </summary>
    private static IEnumerable<string> ParseSrcsetAttribute(string srcsetValue)
    {
        // Split by comma to get individual source entries
        var sources = srcsetValue.Split(',');

        foreach (var source in sources)
        {
            var trimmedSource = source.Trim();
            if (string.IsNullOrEmpty(trimmedSource))
            {
                continue;
            }

            // Split by whitespace to separate URL from descriptor (e.g., "480w", "2x")
            var parts = trimmedSource.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                yield return parts[0]; // The URL is always the first part
            }
        }
    }
}
