using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;

namespace MyLittleContentEngine.Services.Spa;

/// <summary>
/// Content service that registers <c>/_spa-data/{slug}.json</c> pages so the static
/// site generator produces a JSON data file alongside every rendered HTML page.
/// Collects pages from all registered <see cref="IContentService"/> instances
/// (excluding other <see cref="SpaNavigationContentService"/> instances to avoid recursion).
/// </summary>
/// <remarks>
/// Uses <see cref="IServiceProvider"/> for lazy resolution of content services to
/// avoid a circular dependency (this service is itself registered as an <see cref="IContentService"/>).
/// </remarks>
internal class SpaNavigationContentService(
    IServiceProvider serviceProvider,
    SpaNavigationOptions options) : IContentService
{
    public int SearchPriority => 0;

    public async Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync()
    {
        var contentServices = serviceProvider.GetServices<IContentService>();
        var dataPath = options.DataPath.TrimStart('/');
        var builder = ImmutableList.CreateBuilder<PageToGenerate>();

        foreach (var service in contentServices)
        {
            // Skip other SpaNavigationContentService instances to avoid recursion.
            if (service is SpaNavigationContentService) continue;

            var pages = await service.GetPagesToGenerateAsync();
            foreach (var page in pages)
            {
                // Only generate JSON for HTML pages (skip binaries, CSS, JS, etc.)
                if (page.IsBinary) continue;
                var ext = System.IO.Path.GetExtension(page.OutputFile.Value);
                if (!string.IsNullOrEmpty(ext) &&
                    !ext.Equals(".html", StringComparison.OrdinalIgnoreCase) &&
                    !ext.Equals(".htm", StringComparison.OrdinalIgnoreCase))
                    continue;

                var slug = SpaSlug.FromUrl(page.Url);
                builder.Add(new PageToGenerate(
                    new UrlPath($"/{dataPath}/{slug}.json"),
                    new FilePath($"{dataPath}/{slug}.json")));
            }
        }

        return builder.ToImmutable();
    }

    public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() =>
        Task.FromResult(ImmutableList<ContentTocItem>.Empty);

    public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() =>
        Task.FromResult(ImmutableList<ContentToCopy>.Empty);

    public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() =>
        Task.FromResult(ImmutableList<CrossReference>.Empty);
}
