using System.Collections.Immutable;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;

namespace MyLittleContentEngine.DocSite.Services;

/// <summary>
/// Content service that registers <c>/_page-data/{url}.json</c> pages so the static
/// site generator produces a data file alongside every rendered HTML page.
/// </summary>
internal class PageDataContentService(
    IMarkdownContentService<DocSiteFrontMatter> markdownContentService) : IContentService
{
    public int SearchPriority => 0;

    public async Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync()
    {
        var pages = await markdownContentService.GetAllContentPagesAsync();
        return pages
            .Select(p =>
            {
                var slug = p.Url.TrimStart('/');
                if (string.IsNullOrEmpty(slug)) slug = "index";
                return new PageToGenerate(
                    new UrlPath($"/_page-data/{slug}.json"),
                    new FilePath($"_page-data/{slug}.json")
                );
            })
            .ToImmutableList();
    }

    public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() =>
        Task.FromResult(ImmutableList<ContentTocItem>.Empty);

    public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() =>
        Task.FromResult(ImmutableList<ContentToCopy>.Empty);

    public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() =>
        Task.FromResult(ImmutableList<CrossReference>.Empty);
}
