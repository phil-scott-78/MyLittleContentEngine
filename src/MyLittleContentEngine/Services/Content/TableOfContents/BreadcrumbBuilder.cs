using System.Collections.Immutable;
using System.Globalization;

namespace MyLittleContentEngine.Services.Content.TableOfContents;

/// <summary>
/// Builds breadcrumb navigation trails from root to current page.
/// </summary>
internal class BreadcrumbBuilder
{
    private readonly ICollection<IContentService> _services;
    private readonly List<BreadcrumbItem> _breadcrumbs = new();

    public BreadcrumbBuilder(ICollection<IContentService> services)
    {
        _services = services;
    }

    /// <summary>
    /// Builds a complete breadcrumb trail for the specified page.
    /// </summary>
    /// <param name="currentPage">The current page to build breadcrumbs for.</param>
    /// <param name="section">The section the page belongs to.</param>
    /// <returns>An immutable list of breadcrumb items from root to current page.</returns>
    public async Task<ImmutableList<BreadcrumbItem>> BuildAsync(
        PageWithOrder currentPage,
        string section)
    {
        var allPages = await GetPageTitlesWithOrderAsync(_services);

        AddHomeBreadcrumb(allPages);
        AddSectionBreadcrumb(currentPage, section, allPages);
        await AddHierarchyBreadcrumbs(currentPage, section);
        AddCurrentPageBreadcrumb(currentPage);

        return _breadcrumbs.ToImmutableList();
    }

    /// <summary>
    /// Adds the home page as the first breadcrumb.
    /// </summary>
    private void AddHomeBreadcrumb(List<PageWithOrder> allPages)
    {
        // Find the actual home page instead of hardcoding
        var homePage = allPages.FirstOrDefault(p =>
            NavigationUrlComparer.AreEqual(p.Url, "/") ||
            NavigationUrlComparer.AreEqual(p.Url, "/index"));

        if (homePage != null)
        {
            _breadcrumbs.Add(new BreadcrumbItem
            {
                Name = homePage.PageTitle,
                Href = "/",
                IsCurrent = false
            });
        }
        else
        {
            // Fallback to "Home" if no home page found
            _breadcrumbs.Add(new BreadcrumbItem
            {
                Name = "Home",
                Href = "/",
                IsCurrent = false
            });
        }
    }

    /// <summary>
    /// Adds the section breadcrumb if applicable.
    /// </summary>
    private void AddSectionBreadcrumb(
        PageWithOrder currentPage,
        string section,
        List<PageWithOrder> allPages)
    {
        // Add section breadcrumb if we have a section
        if (!string.IsNullOrEmpty(section))
        {
            // Check if the current page IS the section index page
            var sectionUrl = $"/{section.ToLowerInvariant()}";
            var isCurrentPageSectionIndex = NavigationUrlComparer.AreEqual(currentPage.Url, sectionUrl) ||
                                           NavigationUrlComparer.AreEqual(currentPage.Url, $"{sectionUrl}/index");

            // Skip section breadcrumb if we're already on the section's index page
            // (it will be added as the current page breadcrumb at the end)
            if (!isCurrentPageSectionIndex)
            {
                // Check if the section URL is a valid link
                var sectionPage = allPages.FirstOrDefault(p =>
                    NavigationUrlComparer.AreEqual(p.Url, sectionUrl) ||
                    NavigationUrlComparer.AreEqual(p.Url, $"{sectionUrl}/index"));

                if (sectionPage != null)
                {
                    // Section has a valid page, include href
                    _breadcrumbs.Add(new BreadcrumbItem
                    {
                        Name = sectionPage.PageTitle,
                        Href = sectionUrl,
                        IsCurrent = false
                    });
                }
                else
                {
                    // Section doesn't have a page, set href to null
                    _breadcrumbs.Add(new BreadcrumbItem
                    {
                        Name = FormatSectionName(section),
                        Href = null,
                        IsCurrent = false
                    });
                }
            }
        }
    }

    /// <summary>
    /// Adds intermediate breadcrumbs from the hierarchy path.
    /// </summary>
    private async Task AddHierarchyBreadcrumbs(
        PageWithOrder currentPage,
        string section)
    {
        // Build breadcrumbs from hierarchy parts
        if (currentPage.HierarchyParts.Length > 0)
        {
            var sectionPages = await GetPageTitlesWithOrderAsync(_services, section);

            // Skip the first hierarchy part if it matches the section name (case-insensitive)
            var hierarchyStartIndex = 0;
            if (!string.IsNullOrEmpty(section) &&
                currentPage.HierarchyParts.Length > 0 &&
                currentPage.HierarchyParts[0].Equals(section, StringComparison.OrdinalIgnoreCase))
            {
                hierarchyStartIndex = 1;
            }

            // Build intermediate breadcrumbs from hierarchy
            var pathSegments = new List<string>();
            for (var i = hierarchyStartIndex; i < currentPage.HierarchyParts.Length - 1; i++)
            {
                // Use lowercase for URL paths
                pathSegments.Add(currentPage.HierarchyParts[i].ToLowerInvariant());
                var partialPath = string.Join("/", pathSegments);

                // Try to find a page for this partial path
                var parentUrl = string.IsNullOrEmpty(section)
                    ? $"/{partialPath}"
                    : $"/{section.ToLowerInvariant()}/{partialPath}";

                var parentPage = sectionPages.FirstOrDefault(p =>
                    NavigationUrlComparer.AreEqual(p.Url, parentUrl) ||
                    NavigationUrlComparer.AreEqual(p.Url, $"{parentUrl}/index"));

                if (parentPage != null)
                {
                    _breadcrumbs.Add(new BreadcrumbItem
                    {
                        Name = parentPage.PageTitle,
                        Href = parentPage.Url.StartsWith('/') ? parentPage.Url : '/' + parentPage.Url,
                        IsCurrent = false
                    });
                }
                else
                {
                    // No page found at this URL, set href to null
                    _breadcrumbs.Add(new BreadcrumbItem
                    {
                        Name = FormatSegmentName(currentPage.HierarchyParts[i]),
                        Href = null,  // No actual page, so no link
                        IsCurrent = false
                    });
                }
            }
        }
    }

    /// <summary>
    /// Adds the current page as the final breadcrumb.
    /// </summary>
    private void AddCurrentPageBreadcrumb(PageWithOrder currentPage)
    {
        // Add current page as the last breadcrumb (without href)
        _breadcrumbs.Add(new BreadcrumbItem
        {
            Name = currentPage.PageTitle,
            Href = null,
            IsCurrent = true
        });
    }

    private static async Task<List<PageWithOrder>> GetPageTitlesWithOrderAsync(
        ICollection<IContentService> services,
        string? section = null)
    {
        var pageTitlesWithOrder = new List<PageWithOrder>();

        foreach (var contentService in services)
        {
            var tocEntries = await contentService.GetContentTocEntriesAsync();

            // Filter by section if specified
            var filteredEntries = tocEntries.Where(tocEntry =>
            {
                var effectiveSection = tocEntry.Section ?? contentService.DefaultSection;
                return section == null || effectiveSection.Equals(section, StringComparison.OrdinalIgnoreCase);
            });

            var pageWithOrders = filteredEntries.Select(tocEntry => new PageWithOrder(tocEntry.Title, tocEntry.Url, tocEntry.Order, tocEntry.HierarchyParts));
            pageTitlesWithOrder.AddRange(pageWithOrders);
        }

        return pageTitlesWithOrder;
    }

    private static string FormatSectionName(string section)
    {
        if (string.IsNullOrEmpty(section))
            return "Home";

        // Handle common abbreviations
        return section.ToUpperInvariant() switch
        {
            "CLI" => "CLI",
            "API" => "API",
            "FAQ" => "FAQ",
            _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(section.ToLowerInvariant())
        };
    }

    private static string FormatSegmentName(string segment)
    {
        // Convert kebab-case or snake_case to Title Case
        var words = segment.Replace('-', ' ').Replace('_', ' ');
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(words.ToLowerInvariant());
    }
}
