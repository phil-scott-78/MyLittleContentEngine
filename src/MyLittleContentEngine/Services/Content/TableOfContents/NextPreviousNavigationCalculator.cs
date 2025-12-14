namespace MyLittleContentEngine.Services.Content.TableOfContents;

/// <summary>
/// Static helper for calculating previous and next navigation items in a sequential page list.
/// </summary>
internal static class NextPreviousNavigationCalculator
{
    /// <summary>
    /// Calculates the previous and next pages relative to the current page in an ordered sequence.
    /// </summary>
    /// <param name="allPages">All pages in the sequence, ordered by their Order property.</param>
    /// <param name="currentPage">The current page to find neighbors for.</param>
    /// <returns>A tuple containing the previous and next NavigationTreeItems, or null if none exist.</returns>
    public static (NavigationTreeItem? Previous, NavigationTreeItem? Next) Calculate(
        IList<PageWithOrder> allPages,
        PageWithOrder currentPage)
    {
        var previous = allPages
            .Where(p => p.Order < currentPage.Order)
            .OrderByDescending(p => p.Order)
            .FirstOrDefault();

        var next = allPages
            .Where(p => p.Order > currentPage.Order)
            .OrderBy(p => p.Order)
            .FirstOrDefault();

        return (ToNavigationTreeItem(previous), ToNavigationTreeItem(next));
    }

    /// <summary>
    /// Converts a PageWithOrder to a NavigationTreeItem.
    /// </summary>
    private static NavigationTreeItem? ToNavigationTreeItem(PageWithOrder? page)
    {
        if (page == null) return null;

        return new NavigationTreeItem
        {
            Name = page.PageTitle,
            Href = page.Url.StartsWith('/') ? page.Url : '/' + page.Url,
            Order = page.Order,
            IsSelected = false,
            Items = []
        };
    }
}
