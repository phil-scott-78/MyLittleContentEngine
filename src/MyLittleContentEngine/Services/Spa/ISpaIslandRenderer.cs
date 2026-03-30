namespace MyLittleContentEngine.Services.Spa;

/// <summary>
/// Renders HTML content for a named SPA island.
/// Implement this interface to provide custom island content during SPA navigation.
/// </summary>
public interface ISpaIslandRenderer
{
    /// <summary>
    /// Gets the island name. Must match the <c>data-spa-island</c> attribute in the layout markup.
    /// </summary>
    string IslandName { get; }

    /// <summary>
    /// Renders HTML content for the given page URL.
    /// </summary>
    /// <param name="url">The content page URL (e.g. "/" for root, "/pasta-carbonara").</param>
    /// <returns>The HTML string to inject into the island, or null if this page has no content for this island.</returns>
    Task<string?> RenderAsync(string url);
}
