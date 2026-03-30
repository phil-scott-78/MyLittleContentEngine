namespace MyLittleContentEngine.Services.Spa;

/// <summary>
/// Provides page-level metadata (title, description) for SPA navigation envelopes.
/// </summary>
public interface ISpaPageMetadataProvider
{
    /// <summary>
    /// Gets the title and description for a page by URL.
    /// </summary>
    /// <param name="url">The content page URL (e.g. "/" for root, "/pasta-carbonara").</param>
    /// <returns>The metadata, or null if no page exists for this URL.</returns>
    Task<SpaPageMetadata?> GetMetadataAsync(string url);
}

/// <summary>
/// Metadata for a single page in the SPA navigation system.
/// </summary>
/// <param name="Title">The page title.</param>
/// <param name="Description">The page description, if available.</param>
public record SpaPageMetadata(string Title, string? Description);
