using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace MyLittleContentEngine.Services.Content.TableOfContents;

internal class TableOfContentService(IEnumerable<IContentService> contentServices) : ITableOfContentService
{
    private readonly ConcurrentDictionary<Type, IContentService> _contentServices =
        new(contentServices.ToDictionary(service => service.GetType(), service => service));

    public async Task<(NavigationTreeItem? Previous, NavigationTreeItem? Next)> GetNextPreviousAsync(
        string currentUrl)
    {
        var services = _contentServices.Values;
        var allPages = await GetPageTitlesWithOrderAsync(services);
        var currentPage = allPages.FirstOrDefault(p => NavigationUrlComparer.AreEqual(p.Url, currentUrl));
        if (currentPage == null)
        {
            return (null, null);
        }

        var previous = allPages
            .Where(p => p.Order < currentPage.Order)
            .OrderByDescending(p => p.Order)
            .FirstOrDefault();

        var next = allPages
            .Where(p => p.Order > currentPage.Order)
            .OrderBy(p => p.Order)
            .FirstOrDefault();

        return (AsNavigationTreeItem(previous), AsNavigationTreeItem(next));

        NavigationTreeItem? AsNavigationTreeItem(PageWithOrder? page)
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

    public async Task<ImmutableList<NavigationTreeItem>> GetNavigationTocAsync(string currentUrl)
    {
        var services = _contentServices.Values;

        return await GetTableOfContentEntries(currentUrl, services);
    }

    public async Task<ImmutableList<NavigationTreeItem>> GetNavigationTocAsync<T>(string currentUrl)
        where T : IContentService
    {
        if (!_contentServices.TryGetValue(typeof(T), out var contentService))
        {
            throw new InvalidOperationException($"Content service of type {typeof(T).Name} is not registered.");
        }

        return await GetTableOfContentEntries(currentUrl, [contentService]);
    }

    private async Task<ImmutableList<NavigationTreeItem>> GetTableOfContentEntries(string currentUrl,
        ICollection<IContentService> services)
    {
        // Collect all pages (Title, Url, Order)
        var pageTitlesWithOrder = await GetPageTitlesWithOrderAsync(services);

        // Build the tree of URL segments
        var root = new TreeNode { Segment = "" };

        foreach (var (pageTitle, url, order, hierarchyParts) in pageTitlesWithOrder)
        {
            // Use the provided hierarchy parts instead of parsing the URL
            var currentNode = root;
            foreach (var segment in hierarchyParts)
            {
                if (!currentNode.Children.TryGetValue(segment, out var next))
                {
                    next = new TreeNode { Segment = segment };
                    currentNode.Children[segment] = next;
                }

                currentNode = next;
            }

            // Mark the final node in this path as a page
            var lastSegment = hierarchyParts.Length > 0 ? hierarchyParts[^1] : string.Empty;
            currentNode.HasPage = true;
            currentNode.Title = pageTitle;
            currentNode.Order = order;
            currentNode.Url = url.StartsWith('/') ? url : '/' + url;
            currentNode.IsIndex = string.Equals(lastSegment, "index", StringComparison.OrdinalIgnoreCase);
        }

        // Build the top‐level entries from the root node
        return BuildEntries(root, currentUrl).ToImmutableList();
    }

    private static async Task<List<PageWithOrder>> GetPageTitlesWithOrderAsync(ICollection<IContentService> services)
    {
        var pageTitlesWithOrder = new List<PageWithOrder>();

        foreach (var contentService in services)
        {
            var tocEntries = await contentService.GetContentTocEntriesAsync();
            var pageWithOrders = tocEntries.Select(tocEntry => new PageWithOrder(tocEntry.Title, tocEntry.Url, tocEntry.Order, tocEntry.HierarchyParts));
            pageTitlesWithOrder.AddRange(pageWithOrders);
        }

        return pageTitlesWithOrder;
    }

    // Recursive helper: build a single TOC entry (including its children)
    private NavigationTreeItem BuildEntry(TreeNode node, string currentUrl)
    {
        var children = BuildEntries(node, currentUrl).ToArray();
        var handler = NavigationNodeHandler.GetHandler(node);
        return handler.BuildNavigationItem(node, currentUrl, children);
    }

    // Recursive helper: build a list of TOC entries from a given tree node's children
    private List<NavigationTreeItem> BuildEntries(TreeNode parentNode, string currentUrl)
    {
        var entries = parentNode.Children.Values.Select(childNode => BuildEntry(childNode, currentUrl)).ToList();

        // Sort siblings by their Order property (ascending)
        return entries
            .OrderBy(e => e.Order)
            .ToList();
    }
}