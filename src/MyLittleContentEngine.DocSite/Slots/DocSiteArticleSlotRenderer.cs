using MyLittleContentEngine.DocSite.Slots.Components;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;
using MyLittleContentEngine.Services.Spa;

namespace MyLittleContentEngine.DocSite.Slots;

/// <summary>
/// Renders the full article content for the DocSite "content" slot:
/// header (h1), prose-wrapped markdown, and previous/next navigation.
/// </summary>
internal class DocSiteArticleSlotRenderer(
    IMarkdownContentService<DocSiteFrontMatter> contentService,
    ITableOfContentService tocService,
    ComponentRenderer renderer) : RazorIslandRenderer<DocSiteArticle>(renderer)
{
    public override string IslandName => "content";

    protected override async Task<IDictionary<string, object?>?> BuildParametersAsync(string url)
    {
        var result = await contentService.GetRenderedContentPageByUrlOrDefault(url);
        if (result is null) return null;

        var navInfo = await tocService.GetNavigationInfoAsync(result.Value.Page.Url);

        return new Dictionary<string, object?>
        {
            [nameof(DocSiteArticle.Title)] = result.Value.Page.FrontMatter.Title,
            [nameof(DocSiteArticle.HtmlContent)] = result.Value.HtmlContent,
            [nameof(DocSiteArticle.PreviousPageName)] = navInfo?.PreviousPage?.Name,
            [nameof(DocSiteArticle.PreviousPageHref)] = navInfo?.PreviousPage?.Href,
            [nameof(DocSiteArticle.NextPageName)] = navInfo?.NextPage?.Name,
            [nameof(DocSiteArticle.NextPageHref)] = navInfo?.NextPage?.Href,
        };
    }
}
