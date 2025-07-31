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
    /// Gets the next and previous pages in the content sequence relative to the specified URL.
    /// </summary>
    /// <param name="currentUrl">The URL of the current page.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the previous and next <see cref="NavigationTreeItem"/>, which may be null if not found.</returns>
    Task<(NavigationTreeItem? Previous, NavigationTreeItem? Next)> GetNextPreviousAsync(
        string currentUrl);
}