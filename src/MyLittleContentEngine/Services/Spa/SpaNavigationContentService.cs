using System.Collections.Immutable;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;

namespace MyLittleContentEngine.Services.Spa;

/// <summary>
/// Content service that registers <c>/_spa-data/{slug}.json</c> pages so the static
/// site generator produces a JSON data file alongside every rendered HTML page.
/// </summary>
internal class SpaNavigationContentService<TFrontMatter>(
    IMarkdownContentService<TFrontMatter> markdownContentService,
    SpaNavigationOptions options) : IContentService
    where TFrontMatter : class, IFrontMatter, new()
{
    public int SearchPriority => 0;

    public async Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync()
    {
        var pages = await markdownContentService.GetAllContentPagesAsync();
        var dataPath = options.DataPath.TrimStart('/');

        return pages
            .Where(p => !p.FrontMatter.IsDraft)
            .Select(p =>
            {
                var slug = SpaSlug.FromUrl(p.Url);
                return new PageToGenerate(
                    new UrlPath($"/{dataPath}/{slug}.json"),
                    new FilePath($"{dataPath}/{slug}.json"));
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
