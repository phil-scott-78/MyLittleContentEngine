using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Infrastructure;

namespace MyLittleContentEngine.Services.Content.TableOfContents;

internal class TableOfContentService : ITableOfContentService
{
    private const string FolderMetadataFileName = "_index.metadata.yml";

    private readonly ConcurrentDictionary<Type, IContentService> _contentServices;
    private readonly AsyncLazy<ConcurrentDictionary<string, Metadata>> _metadataCache;
    private readonly IEnumerable<IContentOptions> _contentOptions;
    private readonly ContentEngineOptions _contentEngineOptions;
    private readonly FileSystemUtilities _fileSystemUtilities;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<TableOfContentService> _logger;

    public TableOfContentService(
        IEnumerable<IContentService> contentServices,
        IEnumerable<IContentOptions> contentOptions,
        ContentEngineOptions contentEngineOptions,
        FileSystemUtilities fileSystemUtilities,
        IFileSystem fileSystem,
        ILogger<TableOfContentService> logger)
    {
        _contentServices = new ConcurrentDictionary<Type, IContentService>(
            contentServices.ToDictionary(service => service.GetType(), service => service));
        _contentOptions = contentOptions;
        _contentEngineOptions = contentEngineOptions;
        _fileSystemUtilities = fileSystemUtilities;
        _fileSystem = fileSystem;
        _logger = logger;

        // Set up the metadata cache - AsyncLazy handles thread-safe initialization
        _metadataCache = new AsyncLazy<ConcurrentDictionary<string, Metadata>>(
            DiscoverFolderMetadataAsync,
            AsyncLazyFlags.RetryOnFailure);
    }

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

    public async Task<(NavigationTreeItem? Previous, NavigationTreeItem? Next)> GetNextPreviousAsync(
        string currentUrl, string section)
    {
        var services = _contentServices.Values;
        var allPages = await GetPageTitlesWithOrderAsync(services, section);
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

    public async Task<ImmutableList<NavigationTreeItem>> GetNavigationTocAsync(string currentUrl, string section)
    {
        var services = _contentServices.Values;

        return await GetTableOfContentEntries(currentUrl, services, section);
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
        ICollection<IContentService> services, string? section = null)
    {
        // Collect all pages (Title, Url, Order)
        var pageTitlesWithOrder = await GetPageTitlesWithOrderAsync(services, section);

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

        // Enrich tree with folder metadata
        await EnrichTreeWithFolderMetadata(root, string.Empty);

        // Build the top‐level entries from the root node
        return BuildEntries(root, currentUrl).ToImmutableList();
    }

    /// <summary>
    /// Recursively enriches the tree with folder metadata from _index.metadata.yml files.
    /// </summary>
    /// <param name="node">The current tree node.</param>
    /// <param name="currentPath">The accumulated path from the root to this node.</param>
    private async Task EnrichTreeWithFolderMetadata(TreeNode node, string currentPath)
    {
        // Get folder metadata for this path
        if (!string.IsNullOrEmpty(currentPath))
        {
            node.FolderMetadata = await GetFolderMetadata(currentPath);
        }

        // Recursively enrich children with updated path
        foreach (var child in node.Children.Values)
        {
            var childPath = string.IsNullOrEmpty(currentPath)
                ? child.Segment
                : $"{currentPath}/{child.Segment}";

            await EnrichTreeWithFolderMetadata(child, childPath);
        }
    }

    private static async Task<List<PageWithOrder>> GetPageTitlesWithOrderAsync(ICollection<IContentService> services,
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

    public async Task<ImmutableList<string>> GetSectionsAsync()
    {
        var services = _contentServices.Values;
        var sections = new HashSet<string>();

        foreach (var contentService in services)
        {
            // Add the service's default section
            sections.Add(contentService.DefaultSection);

            // Add any item-level section overrides
            var tocEntries = await contentService.GetContentTocEntriesAsync();
            foreach (var tocEntry in tocEntries)
            {
                var effectiveSection = tocEntry.Section ?? contentService.DefaultSection;
                sections.Add(effectiveSection);
            }
        }

        // Convert to sorted list for consistent ordering
        return sections.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToImmutableList();
    }

    public async Task<NavigationInfo?> GetNavigationInfoAsync(string currentUrl)
    {
        var services = _contentServices.Values;

        // Find the page matching the current URL
        PageWithOrder? foundPage = null;
        string? effectiveSection = null;

        foreach (var contentService in services)
        {
            var tocEntries = await contentService.GetContentTocEntriesAsync();

            var match = tocEntries.FirstOrDefault(tocEntry => NavigationUrlComparer.AreEqual(tocEntry.Url, currentUrl));
            if (match != null)
            {
                foundPage = new PageWithOrder(match.Title, match.Url, match.Order, match.HierarchyParts);
                effectiveSection = match.Section ?? contentService.DefaultSection;
                break;
            }
        }

        if (foundPage == null)
        {
            return null;
        }

        // Build breadcrumb trail
        var breadcrumbs = await BuildBreadcrumbsAsync(foundPage, effectiveSection ?? "", services);

        // Get all pages for next/previous navigation
        var allPages = await GetPageTitlesWithOrderAsync(services, effectiveSection);

        // Find previous and next pages
        var previous = allPages
            .Where(p => p.Order < foundPage.Order)
            .OrderByDescending(p => p.Order)
            .FirstOrDefault();

        var next = allPages
            .Where(p => p.Order > foundPage.Order)
            .OrderBy(p => p.Order)
            .FirstOrDefault();

        // Format section name for display
        var sectionName = FormatSectionName(effectiveSection ?? "");
        var sectionPath = effectiveSection?.ToLowerInvariant() ?? "";

        return new NavigationInfo
        {
            SectionName = sectionName,
            SectionPath = sectionPath,
            PageTitle = foundPage.PageTitle,
            Breadcrumbs = breadcrumbs,
            PreviousPage = AsNavigationTreeItem(previous),
            NextPage = AsNavigationTreeItem(next)
        };

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

    private async Task<ImmutableList<BreadcrumbItem>> BuildBreadcrumbsAsync(
        PageWithOrder currentPage,
        string section,
        ICollection<IContentService> services)
    {
        var allPages = await GetPageTitlesWithOrderAsync(services);
        var breadcrumbs = new List<BreadcrumbItem>();
        
        // Find the actual home page instead of hardcoding
        var homePage = allPages.FirstOrDefault(p => 
            NavigationUrlComparer.AreEqual(p.Url, "/") || 
            NavigationUrlComparer.AreEqual(p.Url, "/index"));
        
        if (homePage != null)
        {
            breadcrumbs.Add(new BreadcrumbItem
            {
                Name = homePage.PageTitle,
                Href = "/",
                IsCurrent = false
            });
        }
        else
        {
            // Fallback to "Home" if no home page found
            breadcrumbs.Add(new BreadcrumbItem
            {
                Name = "Home",
                Href = "/",
                IsCurrent = false
            });
        }

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
                    breadcrumbs.Add(new BreadcrumbItem
                    {
                        Name = sectionPage.PageTitle,
                        Href = sectionUrl,
                        IsCurrent = false
                    });
                }
                else
                {
                    // Section doesn't have a page, set href to null
                    breadcrumbs.Add(new BreadcrumbItem
                    {
                        Name = FormatSectionName(section),
                        Href = null,
                        IsCurrent = false
                    });
                }
            }
        }

        // Build breadcrumbs from hierarchy parts
        if (currentPage.HierarchyParts.Length > 0)
        {
            var sectionPages = await GetPageTitlesWithOrderAsync(services, section);

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
                    breadcrumbs.Add(new BreadcrumbItem
                    {
                        Name = parentPage.PageTitle,
                        Href = parentPage.Url.StartsWith('/') ? parentPage.Url : '/' + parentPage.Url,
                        IsCurrent = false
                    });
                }
                else
                {
                    // No page found at this URL, set href to null
                    breadcrumbs.Add(new BreadcrumbItem
                    {
                        Name = FormatSegmentName(currentPage.HierarchyParts[i]),
                        Href = null,  // No actual page, so no link
                        IsCurrent = false
                    });
                }
            }
        }

        // Add current page as the last breadcrumb (without href)
        breadcrumbs.Add(new BreadcrumbItem
        {
            Name = currentPage.PageTitle,
            Href = null,
            IsCurrent = true
        });

        return breadcrumbs.ToImmutableList();
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

    /// <summary>
    /// Gets folder metadata for the specified folder path.
    /// </summary>
    /// <param name="folderPath">The folder path to look up (e.g., "getting-started" or "docs/guides").</param>
    /// <returns>The metadata if found, otherwise null.</returns>
    public async Task<Metadata?> GetFolderMetadata(string folderPath)
    {
        var cache = await _metadataCache;
        var normalizedPath = NormalizeFolderPath(folderPath);
        return cache.GetValueOrDefault(normalizedPath);
    }

    /// <summary>
    /// Discovers all _index.metadata.yml files across registered content service paths.
    /// </summary>
    /// <returns>A dictionary mapping normalized folder paths to their metadata.</returns>
    private async Task<ConcurrentDictionary<string, Metadata>> DiscoverFolderMetadataAsync()
    {
        var metadataDict = new ConcurrentDictionary<string, Metadata>(StringComparer.OrdinalIgnoreCase);
        var contentRoots = GetContentRoots();

        foreach (var (absolutePath, baseSegment) in contentRoots)
        {
            try
            {
                if (!_fileSystem.Directory.Exists(absolutePath))
                {
                    _logger.LogWarning("Content root directory does not exist: {ContentRoot}", absolutePath);
                    continue;
                }

                await DiscoverMetadataInDirectoryAsync(absolutePath, absolutePath, metadataDict, baseSegment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering folder metadata in {ContentRoot}", absolutePath);
            }
        }

        _logger.LogInformation("Discovered {Count} folder metadata files", metadataDict.Count);
        return metadataDict;
    }

    /// <summary>
    /// Recursively discovers metadata files in a directory and its subdirectories.
    /// </summary>
    private async Task DiscoverMetadataInDirectoryAsync(
        string currentDirectory,
        string contentRoot,
        ConcurrentDictionary<string, Metadata> metadataDict,
        string baseSegment)
    {
        var metadataFilePath = _fileSystem.Path.Combine(currentDirectory, FolderMetadataFileName);

        if (_fileSystem.File.Exists(metadataFilePath))
        {
            try
            {
                var metadata = await ParseMetadataFileAsync(metadataFilePath);
                if (metadata != null)
                {
                    // Build the relative folder path from content root
                    var relativePath = _fileSystem.Path.GetRelativePath(contentRoot, currentDirectory);

                    // Combine base segment with relative path to create cache key that matches lookup paths
                    var cacheKey = string.IsNullOrEmpty(baseSegment)
                        ? NormalizeFolderPath(relativePath)
                        : NormalizeFolderPath($"{baseSegment}/{relativePath}");

                    metadataDict[cacheKey] = metadata;
                    _logger.LogDebug("Loaded folder metadata for {FolderPath} from {FilePath}",
                        cacheKey, metadataFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse metadata file: {FilePath}", metadataFilePath);
            }
        }

        // Recursively process subdirectories
        try
        {
            var subdirectories = _fileSystem.Directory.GetDirectories(currentDirectory);
            foreach (var subdirectory in subdirectories)
            {
                // Skip common build and package directories
                var dirName = _fileSystem.Path.GetFileName(subdirectory);
                if (dirName.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals("node_modules", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                await DiscoverMetadataInDirectoryAsync(subdirectory, contentRoot, metadataDict, baseSegment);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error accessing subdirectories in {Directory}", currentDirectory);
        }
    }

    /// <summary>
    /// Parses a metadata YAML file into a Metadata object.
    /// </summary>
    private async Task<Metadata?> ParseMetadataFileAsync(string filePath)
    {
        try
        {
            var yamlContent = await _fileSystem.File.ReadAllTextAsync(filePath);

            if (string.IsNullOrWhiteSpace(yamlContent))
            {
                _logger.LogWarning("Metadata file is empty: {FilePath}", filePath);
                return null;
            }

            var metadata = _contentEngineOptions.FrontMatterDeserializer.Deserialize<Metadata>(yamlContent);
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize metadata file: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Gets all content roots with their associated base URL segments from registered content services.
    /// </summary>
    /// <returns>A list of tuples containing absolute content paths and their corresponding base URL segments.</returns>
    private List<(string absolutePath, string baseSegment)> GetContentRoots()
    {
        var contentRoots = new List<(string, string)>();

        foreach (var contentOption in _contentOptions)
        {
            var contentPath = contentOption.ContentPath;
            if (!contentPath.IsEmpty)
            {
                try
                {
                    var absolutePath = _fileSystemUtilities.ValidateDirectoryPath(contentPath);
                    var baseSegment = ExtractBaseSegment(contentOption.BasePageUrl);
                    contentRoots.Add((absolutePath.Value, baseSegment));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not validate content path: {ContentPath}", contentPath);
                }
            }
        }

        return contentRoots;
    }

    /// <summary>
    /// Normalizes a folder path for consistent lookups (lowercase, forward slashes, trim slashes).
    /// </summary>
    private static string NormalizeFolderPath(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || folderPath == ".")
        {
            return string.Empty;
        }

        // Convert to forward slashes and lowercase
        var normalized = folderPath
            .Replace('\\', '/')
            .Trim('/')
            .ToLowerInvariant();

        return normalized;
    }

    /// <summary>
    /// Extracts the base URL segment(s) from a BasePageUrl for use in cache keys.
    /// </summary>
    /// <param name="basePageUrl">The base page URL from content options.</param>
    /// <returns>A string containing all URL segments joined by forward slashes, or empty string if BasePageUrl is empty.</returns>
    /// <remarks>
    /// Examples:
    /// - Empty/Root: "" → ""
    /// - Single segment: "/console" → "console"
    /// - Multiple segments: "/docs/api" → "docs/api"
    /// This ensures cache keys accurately reflect the URL structure and can differentiate
    /// between content services with the same folder names but different base URLs.
    /// </remarks>
    private static string ExtractBaseSegment(UrlPath basePageUrl)
    {
        if (basePageUrl.IsEmpty)
            return string.Empty;

        var segments = basePageUrl.GetSegments();
        return segments.Length > 0 ? string.Join('/', segments) : string.Empty;
    }
}