using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Services.Infrastructure;

namespace MyLittleContentEngine.Services.Web;

/// <summary>
/// Service for generating search index data for client-side searching
/// </summary>
public partial class SearchIndexService
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions;
    private string _searchIndexCache = string.Empty;
    private readonly IEnumerable<IContentService> _contentServices;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SearchIndexService> _logger;

    /// <summary>
    /// Service for generating search index data for client-side searching
    /// </summary>
    public SearchIndexService(IEnumerable<IContentService> contentServices,
        IHttpClientFactory httpClientFactory,
        IContentEngineFileWatcher fileWatcher,
        ILogger<SearchIndexService> logger)
    {
        _contentServices = contentServices;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        fileWatcher.SubscribeToMetadataUpdate(() => _searchIndexCache = string.Empty);
    }

    static SearchIndexService()
    {
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Generates a search index JSON document
    /// </summary>
    /// <param name="baseUrl">The base URL for the application</param>
    /// <returns>JSON string containing the search index</returns>
    public async Task<string> GenerateSearchIndexAsync(string baseUrl)
    {
        if (!string.IsNullOrWhiteSpace(_searchIndexCache))
        {
            return _searchIndexCache;
        }

        var searchIndex = new SearchIndex();

        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(baseUrl);

        foreach (var contentService in _contentServices)
        {
            var pages = await contentService.GetPagesToGenerateAsync();

            foreach (var page in pages)
            {
                try
                {
                    var document = await ProcessPageAsync(httpClient, page, contentService.SearchPriority);
                    if (document != null)
                    {
                        searchIndex.Documents.Add(document);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process page {Url} for search index", page.Url);
                }
            }
        }

        _searchIndexCache = JsonSerializer.Serialize(searchIndex, _jsonSerializerOptions);
        return _searchIndexCache;
    }

    private async Task<SearchIndexDocument?> ProcessPageAsync(HttpClient httpClient, PageToGenerate page,
        int searchPriority)
    {
        try
        {
            var response = await httpClient.GetAsync(page.Url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch page {Url} - Status: {StatusCode}", page.Url, response.StatusCode);
                return null;
            }

            var html = await response.Content.ReadAsStringAsync();
            var document = ExtractContentFromHtml(html, page, searchPriority);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing page {Url}", page.Url);
            return null;
        }
    }

    private SearchIndexDocument ExtractContentFromHtml(string html, PageToGenerate page, int searchPriority)
    {
        var document = new SearchIndexDocument
        {
            Url = page.Url,
            Title = page.Metadata?.Title ?? ExtractTitle(html),
            Description = page.Metadata?.Description ?? ExtractDescription(html),
            SearchPriority = searchPriority
        };

        // Extract main content from <main> tag
        var mainContent = ExtractMainContent(html);
        if (!string.IsNullOrEmpty(mainContent))
        {
            document.Content = CleanTextContent(mainContent);
            document.Headings = ExtractHeadings(mainContent);
        }

        return document;
    }

    private string ExtractTitle(string html)
    {
        var titleMatch = TitleRegex().Match(html);
        return titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : string.Empty;
    }

    private string ExtractDescription(string html)
    {
        var descriptionMatch = DescriptionRegex().Match(html);
        return descriptionMatch.Success ? descriptionMatch.Groups[1].Value.Trim() : string.Empty;
    }

    private string ExtractMainContent(string html)
    {
        // First, try to find the main tag
        var mainMatch = MainTagRegex().Match(html);

        if (mainMatch.Success)
        {
            var mainContent = mainMatch.Groups[1].Value;

            // Within the main content, look for an article tag
            var articleMatch = ArticleTagRegex().Match(mainContent);

            if (articleMatch.Success)
            {
                return articleMatch.Groups[1].Value;
            }

            // If no article tag found, return the main content
            return mainContent;
        }

        // If no main tag found, fallback to looking for article anywhere in the document
        var fallbackArticleMatch = ArticleTagRegex().Match(html);
        if (fallbackArticleMatch.Success)
        {
            return fallbackArticleMatch.Groups[1].Value;
        }

        // Last resort: return the entire HTML
        return html;
    }

    private string CleanTextContent(string html)
    {
        // Remove script and style tags
        var cleanHtml = CleanHtmlRegex().Replace(html, string.Empty);

        // Remove code blocks (pre tags)
        cleanHtml = CodeBlockRegex().Replace(cleanHtml, string.Empty);

        // Remove HTML tags but keep the content
        cleanHtml = RemoveHtmlTagRegex().Replace(cleanHtml, " ");

        // Clean up whitespace
        cleanHtml = RemoveWhitespaceRegex().Replace(cleanHtml, " ");

        return cleanHtml.Trim();
    }

    private List<string> ExtractHeadings(string html)
    {
        var headings = new List<string>();

        var headingMatches = HeadingRegex().Matches(html);

        foreach (Match match in headingMatches)
        {
            var level = int.Parse(match.Groups[1].Value);
            var text = CleanTextContent(match.Groups[2].Value);

            if (!string.IsNullOrEmpty(text))
            {
                headings.Add($"{level}:{text}");
            }
        }

        return headings;
    }


    // Generated regex patterns for improved performance
    [GeneratedRegex(@"<title[^>]*>([^<]+)</title>", RegexOptions.IgnoreCase)]
    private static partial Regex TitleRegex();

    [GeneratedRegex(@"<meta[^>]*name=[""']description[""'][^>]*content=[""']([^""']+)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex DescriptionRegex();

    [GeneratedRegex(@"<main[^>]*>(.*?)</main>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex MainTagRegex();

    [GeneratedRegex(@"<article[^>]*>(.*?)</article>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ArticleTagRegex();

    [GeneratedRegex(@"<(script|style)[^>]*>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline, "en-US")]
    private static partial Regex CleanHtmlRegex();

    [GeneratedRegex(@"<pre[^>]*>.*?</pre>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CodeBlockRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex RemoveHtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex RemoveWhitespaceRegex();

    [GeneratedRegex(@"<h([1-6])[^>]*>([^<]+)</h\1>", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex HeadingRegex();
}