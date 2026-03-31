namespace MyLittleContentEngine.Services.Content.TableOfContents;

/// <summary>
/// Represents a node in the hierarchical navigation tree.
/// </summary>
public class NavigationTreeItem
{
    /// <summary>Gets the display name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the URL, or null for non-navigable folder nodes.</summary>
    public required string? Href { get; init; }

    /// <summary>Gets the child navigation items.</summary>
    public required NavigationTreeItem[] Items { get; init; }

    /// <summary>Gets the sort order.</summary>
    public required int Order { get; init; } = int.MaxValue;

    /// <summary>Gets whether this item or a descendant is the current page.</summary>
    public required bool IsSelected { get; init; }
}
