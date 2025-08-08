using System.Collections.Immutable;

namespace MyLittleContentEngine.Services.Content.TableOfContents;

/// <summary>
/// Contains navigation information for a given URL including section and breadcrumbs.
/// </summary>
public class NavigationInfo
{
    /// <summary>
    /// Gets the section name for the current URL (e.g., "CLI", "Console", "Documentation").
    /// </summary>
    public required string SectionName { get; init; }
    
    /// <summary>
    /// Gets the section path segment for the current URL (e.g., "cli", "console", "docs").
    /// </summary>
    public required string SectionPath { get; init; }
    
    /// <summary>
    /// Gets the breadcrumb trail from root to current page.
    /// </summary>
    public required ImmutableList<BreadcrumbItem> Breadcrumbs { get; init; }
    
    /// <summary>
    /// Gets the current page title.
    /// </summary>
    public required string PageTitle { get; init; }
    
    /// <summary>
    /// Gets the previous page in the navigation sequence, if any.
    /// </summary>
    public NavigationTreeItem? PreviousPage { get; init; }
    
    /// <summary>
    /// Gets the next page in the navigation sequence, if any.
    /// </summary>
    public NavigationTreeItem? NextPage { get; init; }
}