using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace MyLittleContentEngine.Services.Content.TableOfContents;

public class TableOfContentEntry
{
    public required string Name { get; init; }
    public required string? Href { get; init; }
    public required TableOfContentEntry[] Items { get; init; }
    public required int Order { get; init; } = int.MaxValue;
    public required bool IsSelected { get; init; }
}

// Internal tree node class
internal record TreeNode
{
    public string Segment { get; init; } = "";

    public Dictionary<string, TreeNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool HasPage { get; set; }
    public bool IsIndex { get; set; }
    public string? Title { get; set; }
    public string? Url { get; set; }
    public int Order { get; set; }
}

internal record PageWithOrder(string PageTitle, string Url, int Order);

public interface ITableOfContentService
{
    Task<ImmutableList<TableOfContentEntry>> GetNavigationTocAsync(string currentUrl);

    Task<ImmutableList<TableOfContentEntry>> GetNavigationTocAsync<T>(string currentUrl)
        where T : IContentService;

    Task<(TableOfContentEntry? Previous, TableOfContentEntry? Next)> GetNextPreviousAsync(
        string currentUrl);
}

internal class TableOfContentService(IEnumerable<IContentService> contentServices) : ITableOfContentService
{
    private readonly ConcurrentDictionary<Type, IContentService> _contentServices =
        new(contentServices.ToDictionary(service => service.GetType(), service => service));

    public async Task<(TableOfContentEntry? Previous, TableOfContentEntry? Next)> GetNextPreviousAsync(
        string currentUrl)
    {
        var services = _contentServices.Values;
        var allPages = await GetPageTitlesWithOrderAsync(services);
        var currentPage = allPages.FirstOrDefault(p => HrefEquals(p.Url, currentUrl));
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

        return (AsTocEntry(previous), AsTocEntry(next));

        TableOfContentEntry? AsTocEntry(PageWithOrder? page)
        {
            if (page == null) return null;

            return new TableOfContentEntry
            {
                Name = page.PageTitle,
                // anytime we are talking about whole site navigation, we should rely on baseHref being set properly
                // and use relative URLs
                Href = page.Url.StartsWith('/') ? page.Url.TrimStart('/') : page.Url,
                Order = page.Order,
                IsSelected = false,
                Items = []
            };
        }
    }

    public async Task<ImmutableList<TableOfContentEntry>> GetNavigationTocAsync(string currentUrl)
    {
        var services = _contentServices.Values;

        return await GetTableOfContentEntries(currentUrl, services);
    }

    public async Task<ImmutableList<TableOfContentEntry>> GetNavigationTocAsync<T>(string currentUrl)
        where T : IContentService
    {
        if (!_contentServices.TryGetValue(typeof(T), out var contentService))
        {
            throw new InvalidOperationException($"Content service of type {typeof(T).Name} is not registered.");
        }

        return await GetTableOfContentEntries(currentUrl, [contentService]);
    }

    private async Task<ImmutableList<TableOfContentEntry>> GetTableOfContentEntries(string currentUrl,
        ICollection<IContentService> services)
    {
        // Collect all pages (Title, Url, Order)
        var pageTitlesWithOrder = await GetPageTitlesWithOrderAsync(services);

        // Build the tree of URL segments
        var root = new TreeNode { Segment = "" };

        foreach (var (pageTitle, url, order) in pageTitlesWithOrder)
        {
            // Normalize and split the URL into segments
            var segments = url.Trim('/').Split(['/'], StringSplitOptions.RemoveEmptyEntries);

            var currentNode = root;
            foreach (var segment in segments)
            {
                if (!currentNode.Children.TryGetValue(segment, out var next))
                {
                    next = new TreeNode { Segment = segment };
                    currentNode.Children[segment] = next;
                }

                currentNode = next;
            }

            // Mark the final node in this path as a page
            var lastSegment = segments.Length > 0 ? segments[^1] : string.Empty;
            currentNode.HasPage = true;
            currentNode.Title = pageTitle;
            currentNode.Order = order;
            // anytime we are talking about whole site navigation, we should rely on baseHref being set properly
            // and use relative URLs
            currentNode.Url = url.StartsWith('/') ? url.TrimStart('/') : url;
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
            var pages = await contentService.GetTocEntriesToGenerateAsync();
            foreach (var page in pages)
            {
                if (page.Metadata?.Title == null) continue;
                pageTitlesWithOrder.Add(new PageWithOrder(page.Metadata.Title, page.Url, page.Metadata.Order));
            }
        }

        return pageTitlesWithOrder;
    }

    // Recursive helper: build a single TOC entry (including its children)
    private TableOfContentEntry BuildEntry(TreeNode node, string currentUrl)
    {
        // First, build entries for all of this node's children
        var childEntries = BuildEntries(node, currentUrl);

        // Determine if any descendant is selected
        var anyDescendantSelected = childEntries.Any(e => e.IsSelected);

        // If this node represents an "index" page, treat it as a folder with href property
        if (node is { HasPage: true, IsIndex: true })
        {
            var isSelected = HrefEquals(node.Url, currentUrl) ||
                             anyDescendantSelected;
            return new TableOfContentEntry
            {
                Name = node.Title!,
                Href = node.Url,
                Items = childEntries.ToArray(), // These are children of the index page node itself
                Order = node.Order,
                IsSelected = isSelected
            };
        }

        // If this node has a page (non-index) and may also have child pages
        if (node.HasPage)
        {
            var isSelected = HrefEquals(node.Url, currentUrl) ||
                             anyDescendantSelected;
            return new TableOfContentEntry
            {
                Name = node.Title!,
                Href = node.Url,
                Items = childEntries.ToArray(), // These are children of this page node
                Order = node.Order,
                IsSelected = isSelected
            };
        }

        // Otherwise, this node is a folder (node.HasPage is false).
        // Check if this folder node has a direct child that is an index page.
        var indexChildTreeNode = node.Children.Values
            .FirstOrDefault(childNode => childNode is { IsIndex: true, HasPage: true });

        if (indexChildTreeNode != null)
        {
            // This folder has a direct child that's an index page.
            // The folder entry should adopt the index page's properties.
            // Its items should be the other children of this folder, plus any children of the index page itself.

            TableOfContentEntry? indexChildTocEntry = null;
            var indexOfIndexChildTocEntry = -1;

            for (var i = 0; i < childEntries.Count; i++)
            {
                if (childEntries[i].Href != null &&
                    indexChildTreeNode.Url != null &&
                    HrefEquals(childEntries[i].Href, indexChildTreeNode.Url))
                {
                    indexChildTocEntry = childEntries[i];
                    indexOfIndexChildTocEntry = i;
                    break;
                }
            }

            if (indexChildTocEntry != null)
            {
                var itemsForFolder = childEntries.Where((_, i) => i != indexOfIndexChildTocEntry).ToList();
                itemsForFolder.AddRange(indexChildTocEntry.Items); // Add children of the index page itself

                // Re-sort the combined list of items by their Order property
                itemsForFolder = itemsForFolder.OrderBy(e => e.Order).ToList();

                var isSelectedForAbsorbedFolder = HrefEquals(indexChildTreeNode.Url, currentUrl) ||
                                                  itemsForFolder.Any(item => item.IsSelected);

                return new TableOfContentEntry
                {
                    Name = indexChildTreeNode.Title!, // Use index page's title
                    Href = indexChildTreeNode.Url, // Use index page's URL
                    Items = itemsForFolder.ToArray(),
                    Order = indexChildTreeNode.Order, // Use index page's order
                    IsSelected = isSelectedForAbsorbedFolder
                };
            }
            // If indexChildTocEntry was not found (shouldn't happen if indexChildTreeNode exists),
            // fall through to default folder handling.
        }

        // Default folder handling (folder with no direct index child page, or if above logic failed)
        // Folder name is the URL segment; the folder has no href
        // Order = minimum Order among its children (or Int32.MaxValue if none)
        var folderOrder = childEntries.Count != 0
            ? childEntries.Min(e => e.Order)
            : int.MaxValue;

        return new TableOfContentEntry
        {
            Name = FolderToTitle(node),
            Href = null,
            Items = childEntries.ToArray(),
            Order = folderOrder,
            IsSelected = anyDescendantSelected
        };
    }

    private static string FolderToTitle(TreeNode node)
    {
        const string dashReplacement = "(!gonna be a dash here!)";
        return node.Segment
            .Replace("--", dashReplacement)
            .Replace("-", " ")
            .Replace(dashReplacement, "-")
            .ToApaTitleCase();
    }

    // Recursive helper: build a list of TOC entries from a given tree node's children
    private List<TableOfContentEntry> BuildEntries(TreeNode parentNode, string currentUrl)
    {
        var entries = parentNode.Children.Values.Select(childNode => BuildEntry(childNode, currentUrl)).ToList();

        // Sort siblings by their Order property (ascending)
        return entries
            .OrderBy(e => e.Order)
            .ToList();
    }

    private static bool HrefEquals(string? href1, string? href2)
    {
        if (href1 == null && href2 == null) return true;
        if (href1 == null || href2 == null) return false;

        var normalizeHref1 = NormalizeHref(href1);
        var normalizedHref2 = NormalizeHref(href2);
        return normalizeHref1.Equals(normalizedHref2);
    }

    private static string NormalizeHref(string href)
    {
        if (href == "/") return "/index";
        if (!href.StartsWith('/'))
        {
            href = $"/{href}";
        }

        if (href.EndsWith('/'))
        {
            href = href + "index";
        }

        return href.ToLowerInvariant();
    }
}