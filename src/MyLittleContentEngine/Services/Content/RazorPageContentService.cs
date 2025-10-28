using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content.TableOfContents;
using YamlDotNet.Serialization;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// Content service responsible for discovering Razor pages and their associated metadata from sidecar .yml files.
/// This service replaces the functionality previously handled by RoutesHelper.GetRoutesToRender().
///
/// Discovery Process:
/// 1. At initialization, recursively scans all project roots for .razor files (excluding bin/, obj/, node_modules/)
/// 2. Builds a cache of component names to file paths for fast lookup
/// 3. For each discovered component, looks for metadata files using the naming convention: "ComponentName.razor.metadata.yml"
///
/// The metadata file must be located in the same directory as the Razor component file.
/// For example, for an "Index.razor" component, it looks for "Index.razor.metadata.yml" in the same directory.
///
/// Razor components can be placed in any directory within the project (e.g., Components/Pages, Content, custom folders).
/// </summary>
internal class RazorPageContentService : IContentService
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<RazorPageContentService> _logger;
    private readonly IDeserializer _yamlDeserializer;
    private readonly Lazy<Dictionary<string, string>> _razorFileCache;

    /// <inheritdoc />
    public int SearchPriority => 5; // Medium priority for Razor pages

    /// <summary>
    /// Initializes a new instance of the <see cref="RazorPageContentService"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction for accessing files</param>
    /// <param name="logger">Logger for diagnostic output</param>
    /// <param name="options">Content engine configuration options</param>
    public RazorPageContentService(
        IFileSystem fileSystem,
        ILogger<RazorPageContentService> logger,
        ContentEngineOptions options)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _yamlDeserializer = options.FrontMatterDeserializer;
        _razorFileCache = new Lazy<Dictionary<string, string>>(BuildRazorFileCache);
    }

    /// <inheritdoc />
    public async Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync()
    {
        var pages = ImmutableList<PageToGenerate>.Empty;

        // Get all assemblies that might contain Razor components
        var assemblies = GetRelevantAssemblies();

        foreach (var assembly in assemblies)
        {
            // Get all components that are Blazor components with routes
            var components = GetComponentsFromAssembly(assembly);

            foreach (var component in components)
            {
                var routes = GetRoutesFromComponent(component);
                foreach (var route in routes)
                {
                    // Try to find and load metadata from sidecar .yml file
                    var metadata = await TryLoadMetadataAsync(component);
                    var outputFile = route.TrimStart('/').Replace('/', Path.DirectorySeparatorChar).ToLowerInvariant();
                    if (string.IsNullOrEmpty(outputFile))
                    {
                        outputFile = "index";
                    }

                    outputFile = $"{outputFile}.html";
                    var pageToGenerate = new PageToGenerate(route, outputFile, metadata);

                    pages = pages.Add(pageToGenerate);
                }
            }
        }

        return pages;
    }

    /// <inheritdoc />
    public async Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync()
    {
        var tocItems = ImmutableList<ContentTocItem>.Empty;

        // Get all assemblies that might contain Razor components
        var assemblies = GetRelevantAssemblies();

        foreach (var assembly in assemblies)
        {
            // Get all components that are Blazor components with routes
            var components = GetComponentsFromAssembly(assembly).ToArray();

            foreach (var component in components)
            {
                var routes = GetRoutesFromComponent(component).ToArray();
                if (!routes.Any())
                {
                    continue;
                }

                // Try to find and load metadata from sidecar .yml file
                var metadata = await TryLoadMetadataAsync(component);

                // Only include pages that have metadata in the TOC
                if (metadata != null)
                {
                    // Use the first route for the TOC entry
                    var primaryRoute = routes.First();

                    // Create hierarchy parts from the URL path
                    var hierarchyParts = CreateHierarchyParts(primaryRoute);

                    var tocItem = new ContentTocItem(
                        Title: metadata.Title ?? component.Name,
                        Url: primaryRoute,
                        Order: metadata.Order,
                        HierarchyParts: hierarchyParts,
                        Section: metadata.Section
                    );

                    tocItems = tocItems.Add(tocItem);
                }
            }
        }

        return tocItems;
    }

    /// <inheritdoc />
    public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync()
    {
        // Razor pages don't have content to copy by default
        return Task.FromResult(ImmutableList<ContentToCopy>.Empty);
    }

    /// <inheritdoc />
    public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync()
    {
        // Razor pages don't have cross-references by default
        return Task.FromResult(ImmutableList<CrossReference>.Empty);
    }

    /// <summary>
    /// Gets all assemblies that might contain Razor components, including referenced packages.
    /// </summary>
    /// <returns>Collection of assemblies to scan for components</returns>
    private static IEnumerable<Assembly> GetRelevantAssemblies()
    {
        var assemblies = new HashSet<Assembly>();
        var entryAssembly = Assembly.GetEntryAssembly();

        if (entryAssembly != null)
        {
            assemblies.Add(entryAssembly);

            // Add all referenced assemblies that might contain Razor components
            foreach (var referencedAssembly in entryAssembly.GetReferencedAssemblies())
            {
                try
                {
                    var assembly = Assembly.Load(referencedAssembly);

                    // Only include assemblies that are likely to contain Razor components
                    if (CouldContainRazorComponents(assembly))
                    {
                        assemblies.Add(assembly);
                    }
                }
                catch
                {
                    // Skip assemblies that can't be loaded (security, missing dependencies, etc.)
                }
            }
        }

        // Also check currently loaded assemblies in the current domain
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (CouldContainRazorComponents(assembly))
            {
                assemblies.Add(assembly);
            }
        }

        return assemblies;
    }

    /// <summary>
    /// Determines if an assembly is likely to contain Razor components.
    /// </summary>
    /// <param name="assembly">The assembly to check</param>
    /// <returns>True if the assembly might contain Razor components</returns>
    private static bool CouldContainRazorComponents(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name ?? string.Empty;

        // Skip system assemblies and common third-party libraries that won't have components
        if (assemblyName.StartsWith("System.") ||
            assemblyName.StartsWith("Microsoft.") ||
            assemblyName.StartsWith("netstandard") ||
            assemblyName.StartsWith("mscorlib") ||
            assemblyName.StartsWith("Newtonsoft.") ||
            assemblyName.StartsWith("YamlDotNet") ||
            assemblyName.StartsWith("Markdig") ||
            assemblyName.StartsWith("TextMate") ||
            assemblyName.Equals("TestableIO.System.IO.Abstractions") ||
            assemblyName.Equals("TestableIO.System.IO.Abstractions.Wrappers"))
        {
            return false;
        }

        // Include if it references ASP.NET Core Components (indicating it might have Razor components)
        var referencedAssemblies = assembly.GetReferencedAssemblies();
        return referencedAssemblies.Any(r =>
            r.Name?.Contains("Microsoft.AspNetCore.Components") == true ||
            r.Name?.Contains("Microsoft.AspNetCore.Mvc") == true);
    }

    /// <summary>
    /// Gets Blazor components from a specific assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for components</param>
    /// <returns>Collection of component types</returns>
    private static IEnumerable<Type> GetComponentsFromAssembly(Assembly assembly)
    {
        try
        {
            return assembly
                .GetExportedTypes()
                .Where(t => t.IsSubclassOf(typeof(ComponentBase)) && !t.IsAbstract);
        }
        catch (Exception)
        {
            // Some assemblies might not be accessible or might throw exceptions
            return [];
        }
    }

    /// <summary>
    /// Extracts route templates from a Blazor component that don't contain parameters.
    /// </summary>
    /// <param name="component">The component type to extract routes from</param>
    /// <returns>Collection of route templates without parameters</returns>
    private static IEnumerable<string> GetRoutesFromComponent(Type component)
    {
        return component
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Where(attr => !attr.Template.Contains('{')) // Ignore parameterized routes
            .Select(attr => attr.Template);
    }

    /// <summary>
    /// Attempts to load metadata from a sidecar .yml file for the given component.
    /// </summary>
    /// <param name="component">The component type to find metadata for</param>
    /// <returns>Metadata object if sidecar file exists and is valid, null otherwise</returns>
    private async Task<Metadata?> TryLoadMetadataAsync(Type component)
    {
        var sidecarPath = GetSidecarFilePath(component);
        if (sidecarPath == null || !_fileSystem.File.Exists(sidecarPath))
        {
            return null;
        }

        try
        {
            var yamlContent = await _fileSystem.File.ReadAllTextAsync(sidecarPath);
            if (string.IsNullOrWhiteSpace(yamlContent))
            {
                return null;
            }

            var metadata = _yamlDeserializer.Deserialize<Metadata>(yamlContent);
            _logger.LogDebug("Loaded metadata for component {ComponentName} from {SidecarPath}",
                component.Name, sidecarPath);

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse metadata from sidecar file {SidecarPath} for component {ComponentName}",
                sidecarPath, component.Name);
            return null;
        }
    }

    /// <summary>
    /// Determines the expected path for a sidecar .yml file for the given component.
    /// Looks for files like "ComponentName.razor.metadata.yml" in the same directory as the Razor component.
    /// </summary>
    /// <param name="component">The component type to find sidecar file for</param>
    /// <returns>The expected sidecar file path, or null if it cannot be determined</returns>
    private string? GetSidecarFilePath(Type component)
    {
        // For a component like "Index" or "About", look for "Index.razor.metadata.yml" or "About.razor.metadata.yml"
        // in the same directory as the Razor component file

        // Get the assembly location to help find the component's directory
        var assemblyLocation = component.Assembly.Location;
        if (string.IsNullOrEmpty(assemblyLocation))
        {
            return null;
        }

        var assemblyDir = _fileSystem.Path.GetDirectoryName(assemblyLocation);
        if (string.IsNullOrEmpty(assemblyDir))
        {
            return null;
        }

        // Look in the project source directories for the sidecar file
        var projectRoot = FindProjectRoot(assemblyDir);
        if (projectRoot == null)
        {
            return null;
        }

        // Search for the metadata file in the same directory as the Razor component
        var componentName = component.Name;
        var metadataFileName = $"{componentName}.razor.metadata.yml";

        return FindMetadataFileSideBySide(projectRoot, componentName, metadataFileName);
    }

    /// <summary>
    /// Searches for a metadata file in the same directory as the Razor component.
    /// </summary>
    /// <param name="projectRoot">The project root directory</param>
    /// <param name="componentName">The name of the component</param>
    /// <param name="metadataFileName">The metadata file name to search for</param>
    /// <returns>The path to the metadata file if found, null otherwise</returns>
    private string? FindMetadataFileSideBySide(string projectRoot, string componentName, string metadataFileName)
    {
        try
        {
            if (!_fileSystem.Directory.Exists(projectRoot))
            {
                return null;
            }

            // Look for the Razor component file first, then check for metadata file in same directory
            var razorFileName = $"{componentName}.razor";
            var componentPath = FindRazorComponentFile(razorFileName);

            if (componentPath != null)
            {
                var componentDirectory = _fileSystem.Path.GetDirectoryName(componentPath);
                if (!string.IsNullOrEmpty(componentDirectory))
                {
                    var metadataPath = _fileSystem.Path.Combine(componentDirectory, metadataFileName);
                    return _fileSystem.File.Exists(metadataPath) ? metadataPath : null;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            // Log the exception but don't fail - this is optional metadata
            _logger.LogDebug(ex, "Error searching for metadata file {FileName} for component {ComponentName}", metadataFileName, componentName);
            return null;
        }
    }

    /// <summary>
    /// Finds a Razor component file using the cached component lookup.
    /// The cache is built once at initialization by recursively scanning all project roots for .razor files.
    /// </summary>
    /// <param name="razorFileName">The Razor file name to search for</param>
    /// <returns>The path to the Razor file if found, null otherwise</returns>
    private string? FindRazorComponentFile(string razorFileName)
    {
        // Extract component name from filename (e.g., "Index" from "Index.razor")
        var componentName = _fileSystem.Path.GetFileNameWithoutExtension(razorFileName);

        // Lookup in the cache (built once at initialization via recursive search)
        return _razorFileCache.Value.GetValueOrDefault(componentName);
    }

    /// <summary>
    /// Attempts to find the project root directory by looking for common project files.
    /// </summary>
    /// <param name="startDirectory">Directory to start searching from</param>
    /// <returns>Project root directory, or null if not found</returns>
    private string? FindProjectRoot(string startDirectory)
    {
        var currentDir = startDirectory;

        while (!string.IsNullOrEmpty(currentDir))
        {
            try
            {
                // Look for common project indicators
                if (_fileSystem.Directory.Exists(currentDir) &&
                    _fileSystem.Directory.GetFiles(currentDir, "*.csproj").Length > 0)
                {
                    return currentDir;
                }

                var parentDir = _fileSystem.Directory.GetParent(currentDir)?.FullName;
                if (parentDir == currentDir) // Reached root
                {
                    break;
                }
                currentDir = parentDir;
            }
            catch (DirectoryNotFoundException)
            {
                // Handle case where directory doesn't exist (in tests)
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// Creates hierarchy parts from a URL path for table of contents organization.
    /// Returns all URL segments to maintain consistency with other content services.
    /// </summary>
    /// <param name="url">The URL path to create hierarchy from</param>
    /// <returns>Array of hierarchy parts containing all URL segments</returns>
    private static string[] CreateHierarchyParts(string url)
    {
        // Remove leading slash and split by forward slashes to get all segments
        // This ensures Razor pages are placed at the same hierarchy level as other content types
        return url.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Builds a cache of all Razor component files in the project by recursively searching from project roots.
    /// This cache maps component names to their file paths for fast lookup.
    /// </summary>
    /// <returns>Dictionary mapping component names to their full file paths</returns>
    private Dictionary<string, string> BuildRazorFileCache()
    {
        var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var assemblies = GetRelevantAssemblies();

        // First, collect all unique project roots from assemblies
        var uniqueProjectRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in assemblies)
        {
            var assemblyLocation = assembly.Location;
            if (string.IsNullOrEmpty(assemblyLocation))
            {
                continue;
            }

            var assemblyDir = _fileSystem.Path.GetDirectoryName(assemblyLocation);
            if (string.IsNullOrEmpty(assemblyDir))
            {
                continue;
            }

            var projectRoot = FindProjectRoot(assemblyDir);
            if (projectRoot != null && _fileSystem.Directory.Exists(projectRoot))
            {
                uniqueProjectRoots.Add(projectRoot);
            }
        }

        // Now scan each unique project root for .razor files
        foreach (var projectRoot in uniqueProjectRoots)
        {
            try
            {
                // Recursively find all .razor files in the project
                var enumerationOptions = new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true
                };

                var razorFiles = _fileSystem.Directory.GetFiles(projectRoot, "*.razor", enumerationOptions);

                foreach (var filePath in razorFiles)
                {
                    // Skip files in build output or dependency directories
                    if (ShouldExcludeFile(filePath))
                    {
                        continue;
                    }

                    var fileName = _fileSystem.Path.GetFileName(filePath);
                    // Get component name without .razor extension
                    var componentName = _fileSystem.Path.GetFileNameWithoutExtension(fileName);

                    // Store first match only (don't overwrite if component name already exists)
                    if (!cache.TryGetValue(componentName, out var value))
                    {
                        cache[componentName] = filePath;
                        _logger.LogTrace("Cached Razor component: {ComponentName} -> {FilePath}", componentName, filePath);
                    }
                    else
                    {
                        _logger.LogDebug("Duplicate component name found: {ComponentName}. Using first match: {ExistingPath}, ignoring: {DuplicatePath}",
                            componentName, value, filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error scanning for Razor files in project root {ProjectRoot}", projectRoot);
            }
        }

        _logger.LogDebug("Built Razor file cache with {Count} components", cache.Count);
        return cache;
    }

    /// <summary>
    /// Determines if a file path should be excluded from the Razor component cache.
    /// Excludes build output directories (bin, obj) and dependency directories (node_modules).
    /// </summary>
    /// <param name="filePath">The file path to check</param>
    /// <returns>True if the file should be excluded, false otherwise</returns>
    private bool ShouldExcludeFile(string filePath)
    {
        var normalizedPath = filePath.Replace('\\', '/');

        // Exclude bin, obj, and node_modules directories
        return normalizedPath.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
               normalizedPath.Contains("/obj/", StringComparison.OrdinalIgnoreCase) ||
               normalizedPath.Contains("/node_modules/", StringComparison.OrdinalIgnoreCase);
    }
}