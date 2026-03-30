using System.Text.Json;

namespace MyLittleContentEngine.Services.Spa;

/// <summary>
/// Coordinates <see cref="ISpaIslandRenderer"/> instances and an <see cref="ISpaPageMetadataProvider"/>
/// to produce a complete <see cref="SpaPageEnvelope"/> for a given page slug.
/// </summary>
public class SpaPageDataService(
    IEnumerable<ISpaIslandRenderer> islandRenderers,
    ISpaPageMetadataProvider metadataProvider)
{
    /// <summary>
    /// Builds a page data envelope by calling all registered island renderers.
    /// </summary>
    /// <param name="slug">The page slug (URL path without leading slash; "index" for root).</param>
    /// <returns>The assembled envelope, or null if the metadata provider returns no data for this slug.</returns>
    public async Task<SpaPageEnvelope?> GetPageDataAsync(string slug)
    {
        var url = SpaSlug.ToUrl(slug);

        var metadata = await metadataProvider.GetMetadataAsync(url);
        if (metadata is null) return null;

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
            Title = metadata.Title,
            Description = metadata.Description,
            Islands = islands,
        };
    }

    /// <summary>
    /// Serializes a <see cref="SpaPageEnvelope"/> to JSON using the source-generated context.
    /// </summary>
    public static string Serialize(SpaPageEnvelope envelope) =>
        JsonSerializer.Serialize(envelope, SpaPageEnvelopeJsonContext.Default.SpaPageEnvelope);
}
