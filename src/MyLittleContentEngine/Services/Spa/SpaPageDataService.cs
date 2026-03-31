using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MyLittleContentEngine.Services.Content;

namespace MyLittleContentEngine.Services.Spa;

/// <summary>
/// Coordinates <see cref="ISpaIslandRenderer"/> instances and resolves page metadata
/// from registered <see cref="IContentService"/> instances to produce a complete
/// <see cref="SpaPageEnvelope"/> for a given page slug.
/// </summary>
/// <remarks>
/// Uses <see cref="IServiceProvider"/> for lazy resolution of content services to
/// avoid a circular dependency with <see cref="SpaNavigationContentService"/>.
/// </remarks>
public class SpaPageDataService(
    IEnumerable<ISpaIslandRenderer> islandRenderers,
    IServiceProvider serviceProvider)
{
    /// <summary>
    /// Builds a page data envelope by calling all registered island renderers.
    /// </summary>
    /// <param name="slug">The page slug (URL path without leading slash; "index" for root).</param>
    /// <returns>The assembled envelope, or null if no content service has a page for this slug.</returns>
    public async Task<SpaPageEnvelope?> GetPageDataAsync(string slug)
    {
        var url = SpaSlug.ToUrl(slug);

        var (title, description) = await ResolveMetadataAsync(url);
        if (title is null) return null;

        // When multiple renderers target the same slot, only the last registered
        // one runs (custom renderers override built-in ones).
        var effectiveRenderers = islandRenderers
            .GroupBy(r => r.IslandName)
            .Select(g => g.Last());

        var islands = new Dictionary<string, string>();
        foreach (var renderer in effectiveRenderers)
        {
            var html = await renderer.RenderAsync(url);
            if (html is not null)
                islands[renderer.IslandName] = html;
        }

        return new SpaPageEnvelope
        {
            Title = title,
            Description = description,
            Islands = islands,
        };
    }

    private async Task<(string? Title, string? Description)> ResolveMetadataAsync(string url)
    {
        var contentServices = serviceProvider.GetServices<IContentService>();

        foreach (var service in contentServices)
        {
            if (service is SpaNavigationContentService) continue;

            var pages = await service.GetPagesToGenerateAsync();
            var match = pages.FirstOrDefault(p =>
                string.Equals(p.Url.Value.TrimEnd('/'), url.TrimEnd('/'), StringComparison.OrdinalIgnoreCase) ||
                (url == "/" && string.Equals(p.Url.Value, "/", StringComparison.OrdinalIgnoreCase)));

            if (match?.Metadata is { Title: not null })
            {
                return (match.Metadata.Title, match.Metadata.Description);
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Serializes a <see cref="SpaPageEnvelope"/> to JSON using the source-generated context.
    /// </summary>
    public static string Serialize(SpaPageEnvelope envelope) =>
        JsonSerializer.Serialize(envelope, SpaPageEnvelopeJsonContext.Default.SpaPageEnvelope);
}
