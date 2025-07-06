using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MyLittleContentEngine.Services.Web;

/// <summary>
/// Service for generating search index data for client-side searching
/// </summary>
public class SearchIndexService
{
    private readonly IEnumerable<IContentService> _contentServices;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SearchIndexService> _logger;

    public SearchIndexService(
        IEnumerable<IContentService> contentServices,
        IHttpClientFactory httpClientFactory,
        ILogger<SearchIndexService> logger)
    {
        _contentServices = contentServices;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Generates a search index JSON document
    /// </summary>
    /// <param name="baseUrl">The base URL for the application</param>
    /// <returns>JSON string containing the search index</returns>
    public async Task<string> GenerateSearchIndexAsync(string baseUrl)
    {
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
                    var document = await ProcessPageAsync(httpClient, page);
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

        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(searchIndex, options);
    }

    private async Task<SearchIndexDocument?> ProcessPageAsync(HttpClient httpClient, PageToGenerate page)
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
            var document = ExtractContentFromHtml(html, page);
            
            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing page {Url}", page.Url);
            return null;
        }
    }

    private SearchIndexDocument ExtractContentFromHtml(string html, PageToGenerate page)
    {
        var document = new SearchIndexDocument
        {
            Url = page.Url,
            Title = page.Metadata?.Title ?? ExtractTitle(html),
            Description = page.Metadata?.Description ?? ExtractDescription(html)
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
        var titleMatch = Regex.Match(html, @"<title[^>]*>([^<]+)</title>", RegexOptions.IgnoreCase);
        return titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : string.Empty;
    }

    private string ExtractDescription(string html)
    {
        var descriptionMatch = Regex.Match(html, @"<meta[^>]*name=[""']description[""'][^>]*content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        return descriptionMatch.Success ? descriptionMatch.Groups[1].Value.Trim() : string.Empty;
    }

    private string ExtractMainContent(string html)
    {
        var mainMatch = Regex.Match(html, @"<main[^>]*>(.*?)</main>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return mainMatch.Success ? mainMatch.Groups[1].Value : html;
    }

    private string CleanTextContent(string html)
    {
        // Remove script and style tags
        var cleanHtml = Regex.Replace(html, @"<(script|style)[^>]*>.*?</\1>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        // Remove HTML tags but keep the content
        cleanHtml = Regex.Replace(cleanHtml, @"<[^>]+>", " ");
        
        // Clean up whitespace
        cleanHtml = Regex.Replace(cleanHtml, @"\s+", " ");
        
        return cleanHtml.Trim();
    }

    private List<SearchIndexHeading> ExtractHeadings(string html)
    {
        var headings = new List<SearchIndexHeading>();
        
        var headingMatches = Regex.Matches(html, @"<h([1-6])[^>]*>([^<]+)</h\1>", RegexOptions.IgnoreCase);
        
        foreach (Match match in headingMatches)
        {
            var level = int.Parse(match.Groups[1].Value);
            var text = CleanTextContent(match.Groups[2].Value);
            
            if (!string.IsNullOrEmpty(text))
            {
                headings.Add(new SearchIndexHeading
                {
                    Text = text,
                    Level = level,
                    Priority = GetHeadingPriority(level)
                });
            }
        }

        return headings;
    }

    private int GetHeadingPriority(int level)
    {
        return level switch
        {
            1 => 100,  // H1 - highest priority
            2 => 80,   // H2 - high priority
            3 => 60,   // H3 - medium-high priority
            4 => 40,   // H4 - medium priority
            5 => 20,   // H5 - low priority
            6 => 10,   // H6 - lowest priority
            _ => 0
        };
    }
}