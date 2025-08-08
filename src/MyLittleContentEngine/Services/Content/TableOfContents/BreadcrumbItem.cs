namespace MyLittleContentEngine.Services.Content.TableOfContents;

/// <summary>
/// Represents a single item in a breadcrumb navigation trail.
/// </summary>
public class BreadcrumbItem
{
    /// <summary>
    /// Gets the display name for this breadcrumb item.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Gets the URL for this breadcrumb item. Null for the current/last item.
    /// </summary>
    public required string? Href { get; init; }
    
    /// <summary>
    /// Gets whether this is the current page (last item in breadcrumb).
    /// </summary>
    public required bool IsCurrent { get; init; }
}