using System.Collections.Immutable;

namespace MyLittleContentEngine.Services.Content.TableOfContents;

/// <summary>
/// Defines the contract for a service that generates and provides table of contents (TOC) data.
/// </summary>
public interface ITableOfContentService
{
    /// <summary>
    /// Gets the navigation table of contents for all registered content services.
    /// </summary>
    /// <param name="currentUrl">The URL of the currently active page, used to mark the corresponding TOC entry as selected.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of root <see cref="NavigationTreeItem"/> objects.</returns>
    Task<ImmutableList<NavigationTreeItem>> GetNavigationTocAsync(string currentUrl);

    /// <summary>
    /// Gets the navigation table of contents for a specific content service type.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="IContentService"/> to generate the TOC for.</typeparam>
    /// <param name="currentUrl">The URL of the currently active page, used to mark the corresponding TOC entry as selected.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of root <see cref="NavigationTreeItem"/> objects for the specified content service.</returns>
    Task<ImmutableList<NavigationTreeItem>> GetNavigationTocAsync<T>(string currentUrl)
        where T : IContentService;

    /// <summary>
    /// Gets the navigation table of contents for a specific section.
    /// </summary>
    /// <param name="currentUrl">The URL of the currently active page, used to mark the corresponding TOC entry as selected.</param>
    /// <param name="section">The section to filter by. Empty string returns global content (no section).</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of root <see cref="NavigationTreeItem"/> objects for the specified section.</returns>
    Task<ImmutableList<NavigationTreeItem>> GetNavigationTocAsync(string currentUrl, string section);

    /// <summary>
    /// Gets the next and previous pages in the content sequence relative to the specified URL.
    /// </summary>
    /// <param name="currentUrl">The URL of the current page.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the previous and next <see cref="NavigationTreeItem"/>, which may be null if not found.</returns>
    /// <remarks>This method is obsolete. Use GetNavigationInfoAsync instead, which includes next/previous navigation along with other navigation data.</remarks>
    [Obsolete("Use GetNavigationInfoAsync instead, which includes next/previous navigation along with breadcrumbs and section information.")]
    Task<(NavigationTreeItem? Previous, NavigationTreeItem? Next)> GetNextPreviousAsync(
        string currentUrl);

    /// <summary>
    /// Gets the next and previous pages in the content sequence relative to the specified URL within a specific section.
    /// </summary>
    /// <param name="currentUrl">The URL of the current page.</param>
    /// <param name="section">The section to filter by. Empty string searches global content (no section).</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the previous and next <see cref="NavigationTreeItem"/>, which may be null if not found.</returns>
    /// <remarks>This method is obsolete. Use GetNavigationInfoAsync instead, which includes next/previous navigation along with other navigation data.</remarks>
    [Obsolete("Use GetNavigationInfoAsync instead, which includes next/previous navigation along with breadcrumbs and section information.")]
    Task<(NavigationTreeItem? Previous, NavigationTreeItem? Next)> GetNextPreviousAsync(
        string currentUrl, string section);

    /// <summary>
    /// Gets all available sections defined across all content services.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of section names. Empty string represents global content (no section).</returns>
    Task<ImmutableList<string>> GetSectionsAsync();

    /// <summary>
    /// Gets navigation information for a specific URL including section and breadcrumb trail.
    /// </summary>
    /// <param name="currentUrl">The URL to get navigation information for.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the navigation information, or null if the URL is not found.</returns>
    Task<NavigationInfo?> GetNavigationInfoAsync(string currentUrl);
}