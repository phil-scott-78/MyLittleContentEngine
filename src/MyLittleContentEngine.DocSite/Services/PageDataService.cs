using System.Text.Json;
using System.Text.Json.Serialization;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;

namespace MyLittleContentEngine.DocSite.Services;

/// <summary>
/// Data payload returned per-page for client-side SPA navigation.
/// </summary>
public record PageData
{
    /// <summary>Gets the page title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the page description, if set.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the canonical URL for this page, if configured.</summary>
    public string? CanonicalUrl { get; init; }

    /// <summary>Gets the rendered HTML that belongs inside the prose &lt;main&gt; element.</summary>
    public required string HtmlContent { get; init; }

    /// <summary>Gets the hierarchical outline entries for the right-sidebar TOC.</summary>
    public required PageOutlineEntry[] Outline { get; init; }

    /// <summary>Gets the previous page in the navigation sequence, or null if none.</summary>
    public PageNavLink? PreviousPage { get; init; }

    /// <summary>Gets the next page in the navigation sequence, or null if none.</summary>
    public PageNavLink? NextPage { get; init; }
}

/// <summary>A single entry in the page heading outline.</summary>
public record PageOutlineEntry(string Title, string Id, PageOutlineEntry[] Children);

/// <summary>A link to the previous or next page.</summary>
public record PageNavLink(string Name, string Href);

[JsonSerializable(typeof(PageData))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class PageDataJsonContext : JsonSerializerContext;

/// <summary>
/// Generates <see cref="PageData"/> for a given page URL.
/// Used both by the runtime endpoint and during static generation.
/// </summary>
internal class PageDataService(
    IMarkdownContentService<DocSiteFrontMatter> markdownContentService,
    ITableOfContentService tableOfContentService,
    DocSiteOptions docSiteOptions)
{
    internal async Task<PageData?> GetPageDataAsync(string slug)
    {
        var page = await markdownContentService.GetRenderedContentPageByUrlOrDefault(slug);
        if (page == null)
            return null;

        var navInfo = await tableOfContentService.GetNavigationInfoAsync(page.Value.Page.Url);

        var pageData = new PageData
        {
            Title = page.Value.Page.FrontMatter.Title,
            Description = string.IsNullOrEmpty(page.Value.Page.FrontMatter.Description)
                ? null
                : page.Value.Page.FrontMatter.Description,
            CanonicalUrl = docSiteOptions.CanonicalBaseUrl != null
                ? docSiteOptions.CanonicalBaseUrl.TrimEnd('/') + page.Value.Page.Url
                : null,
            HtmlContent = page.Value.HtmlContent,
            Outline = MapOutline(page.Value.Page.Outline),
            PreviousPage = navInfo?.PreviousPage?.Href != null
                ? new PageNavLink(navInfo.PreviousPage.Name, navInfo.PreviousPage.Href)
                : null,
            NextPage = navInfo?.NextPage?.Href != null
                ? new PageNavLink(navInfo.NextPage.Name, navInfo.NextPage.Href)
                : null,
        };

        return pageData;
    }

    internal static string Serialize(PageData data) =>
        JsonSerializer.Serialize(data, PageDataJsonContext.Default.PageData);

    private static PageOutlineEntry[] MapOutline(OutlineEntry[] entries) =>
        entries.Select(e => new PageOutlineEntry(e.Title, e.Id, MapOutline(e.Children))).ToArray();
}