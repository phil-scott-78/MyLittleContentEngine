using System.Collections.Immutable;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;

namespace MyLittleContentEngine.Services.Web;

/// <summary>
/// Service for generating sitemap.xml and RSS feed files for a MyLittleContentEngine website.
/// </summary>
internal class SitemapRssService
{
    private readonly ContentEngineOptions _options;
    private readonly IEnumerable<IContentService> _contentServices;

    /// <summary>
    /// Initializes a new instance of the <see cref="SitemapRssService"/> class.
    /// </summary>
    /// <param name="options">The MyLittleContentEngine options.</param>
    /// <param name="contentServices">The collection of content services.</param>
    public SitemapRssService(
        ContentEngineOptions options,
        IEnumerable<IContentService> contentServices
    )
    {
        _options = options;
        _contentServices = contentServices;
    }

    /// <summary>
    /// Generates a sitemap.xml file with optional hreflang alternate links for multi-locale sites.
    /// </summary>
    /// <returns>The XML string representation of the sitemap.</returns>
    public async Task<string> GenerateSitemap()
    {
        var baseUrl = GetBaseUrl();

        // Create the sitemap root element
        XNamespace ns = "https://www.sitemaps.org/schemas/sitemap/0.9";
        XNamespace xhtml = "http://www.w3.org/1999/xhtml";

        var root = new XElement(ns + "urlset");

        // Add xhtml namespace if localization is configured
        var localization = _options.Localization;
        if (localization != null && localization.Locales.Count > 1)
        {
            root.Add(new XAttribute(XNamespace.Xmlns + "xhtml", xhtml.NamespaceName));
        }

        // Collect all pages from content services
        var pagesToGenerate = ImmutableList<PageToGenerate>.Empty;

        foreach (var content in _contentServices)
        {
            var pages = await content.GetPagesToGenerateAsync();
            pagesToGenerate = pagesToGenerate.AddRange(pages);
        }

        // Build a lookup of content-relative URL → locale URLs for hreflang
        var localeUrlMap = BuildLocaleUrlMap(pagesToGenerate, localization);

        // Add each page to the sitemap
        foreach (var (url, _, metadata, _) in pagesToGenerate)
        {
            // Construct the full URL: combine base URL with page URL
            var pageUrl = new UrlPath(url).EnsureLeadingSlash().Value;
            var fullUrl = baseUrl + pageUrl;

            var urlElement = new XElement(ns + "url",
                new XElement(ns + "loc", fullUrl));

            // Add lastmod if available
            if (metadata?.LastMod != null)
            {
                urlElement.Add(new XElement(ns + "lastmod",
                    metadata.LastMod.Value.ToString("yyyy-MM-dd")));
            }

            // Add hreflang alternate links if multi-locale
            if (localization != null && localization.Locales.Count > 1)
            {
                var contentRelativeUrl = StripLocalePrefix(url, localization);

                if (localeUrlMap.TryGetValue(contentRelativeUrl, out var alternates))
                {
                    foreach (var (locale, localeUrl) in alternates)
                    {
                        var hreflang = localization.Locales.TryGetValue(locale, out var info)
                            ? info.HtmlLang ?? locale
                            : locale;

                        urlElement.Add(new XElement(xhtml + "link",
                            new XAttribute("rel", "alternate"),
                            new XAttribute("hreflang", hreflang),
                            new XAttribute("href", baseUrl + new UrlPath(localeUrl).EnsureLeadingSlash().Value)));
                    }
                }
            }

            root.Add(urlElement);
        }

        // Create XML document
        var document = new XDocument(new XDeclaration("1.0", "utf-8", null), root);

        // Return the XML as a string
        var sb = new StringBuilder();

        await using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, Async = true }))
        {
            document.Save(writer);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates an RSS feed XML file.
    /// </summary>
    /// <returns>The XML string representation of the RSS feed.</returns>
    public async Task<string> GenerateRssFeed()
    {
        var baseUrl = GetBaseUrl();

        // Create the feed
        var feed = new SyndicationFeed(
            _options.SiteTitle,
            _options.SiteDescription,
            new Uri(baseUrl))
        {
            Language = "en-us",
            LastUpdatedTime = DateTimeOffset.UtcNow
        };

        // Collect items for the RSS feed
        var items = new List<SyndicationItem>();

        // Go through all content services to find posts that should be in the RSS feed
        foreach (var contentService in _contentServices)
        {
            var pages = (await contentService.GetPagesToGenerateAsync())
                .Where(p => p.Metadata?.RssItem == true)
                .ToList();

            var syndicationItems = pages
                .Where(page => page.Metadata != null && !string.IsNullOrEmpty(page.Metadata?.Title))
                .Select(page => GetSyndicationItem(page.Url, page.Metadata!, baseUrl));
            items.AddRange(syndicationItems);
        }

        feed.Items = items;

        // Write the feed to a string
        var sb = new StringBuilder();
        await using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, Async = true }))
        {
            var formatter = new Rss20FeedFormatter(feed);
            formatter.WriteTo(writer);
        }

        return sb.ToString();
    }

    private static SyndicationItem GetSyndicationItem(string url, Metadata metadata, string baseUrl)
    {
        // Construct the full URL: combine base URL with page URL
        var pageUrl = new UrlPath(url).EnsureLeadingSlash().Value;
        var fullUrl = baseUrl + pageUrl;

        return new SyndicationItem(
            metadata.Title,
            metadata.Description ?? string.Empty,
            new Uri(fullUrl),
            url,
            metadata.LastMod.HasValue
                ? new DateTimeOffset(metadata.LastMod.Value)
                : DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Builds a map from content-relative URL → list of (locale, full URL) pairs.
    /// Used to generate hreflang alternate links in the sitemap.
    /// </summary>
    private static Dictionary<string, List<(string Locale, string Url)>> BuildLocaleUrlMap(
        ImmutableList<PageToGenerate> pages,
        LocalizationOptions? localization)
    {
        var map = new Dictionary<string, List<(string, string)>>(StringComparer.OrdinalIgnoreCase);

        if (localization == null || localization.Locales.Count <= 1)
            return map;

        foreach (var page in pages)
        {
            var contentRelativeUrl = StripLocalePrefix(page.Url, localization);
            var locale = DetectLocaleFromUrl(page.Url, localization);

            if (!map.TryGetValue(contentRelativeUrl, out var list))
            {
                list = [];
                map[contentRelativeUrl] = list;
            }

            list.Add((locale, page.Url));
        }

        return map;
    }

    /// <summary>
    /// Strips the locale prefix from a URL to get the content-relative path.
    /// </summary>
    private static string StripLocalePrefix(string url, LocalizationOptions localization)
    {
        var trimmed = url.TrimStart('/');
        var firstSlash = trimmed.IndexOf('/');
        var firstSegment = firstSlash >= 0 ? trimmed[..firstSlash] : trimmed;

        if (!string.IsNullOrEmpty(firstSegment)
            && localization.Locales.ContainsKey(firstSegment)
            && !string.Equals(firstSegment, localization.DefaultLocale, StringComparison.OrdinalIgnoreCase))
        {
            return firstSlash >= 0 ? trimmed[(firstSlash + 1)..] : "";
        }

        return trimmed;
    }

    /// <summary>
    /// Detects the locale from a URL based on its prefix.
    /// </summary>
    private static string DetectLocaleFromUrl(string url, LocalizationOptions localization)
    {
        var trimmed = url.TrimStart('/');
        var firstSlash = trimmed.IndexOf('/');
        var firstSegment = firstSlash >= 0 ? trimmed[..firstSlash] : trimmed;

        if (!string.IsNullOrEmpty(firstSegment)
            && localization.Locales.ContainsKey(firstSegment)
            && !string.Equals(firstSegment, localization.DefaultLocale, StringComparison.OrdinalIgnoreCase))
        {
            return firstSegment;
        }

        return localization.DefaultLocale;
    }

    private string GetBaseUrl()
    {
        // First, try to get it from options
        if (!string.IsNullOrWhiteSpace(_options.CanonicalBaseUrl))
        {
            // CanonicalBaseUrl is expected to be a full URL like "https://example.com"
            // We need to ensure it doesn't end with a slash for proper URL construction
            return _options.CanonicalBaseUrl.TrimEnd('/');
        }

        return "https://example.com";
    }
}
