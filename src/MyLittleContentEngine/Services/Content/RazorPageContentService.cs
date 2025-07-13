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
/// Searches for metadata files using the naming convention: "ComponentName.razor.metadata.yml"
/// The metadata file must be located in the same directory as the Razor component file.
/// For example, for an "Index.razor" component, it looks for "Index.razor.metadata.yml" in the same directory.
/// </summary>
internal class RazorPageContentService : IContentService
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<RazorPageContentService> _logger;
    private readonly ContentEngineOptions _options;
    private readonly IDeserializer _yamlDeserializer;

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
        _options = options;
        _yamlDeserializer = options.FrontMatterDeserializer;
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
                    
                    var pageToGenerate = new PageToGenerate(route, _fileSystem.Path.Combine(route, _options.IndexPageHtml), metadata);

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
            var components = GetComponentsFromAssembly(assembly);

            foreach (var component in components)
            {
                var routes = GetRoutesFromComponent(component);
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
                        HierarchyParts: hierarchyParts
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
                catch (Exception)
                {
                    // Skip assemblies that can't be loaded (security, missing dependencies, etc.)
                    continue;
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
            var componentPath = FindRazorComponentFile(projectRoot, razorFileName);
            
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
    /// Finds a Razor component file in common component directories.
    /// </summary>
    /// <param name="projectRoot">The project root directory</param>
    /// <param name="razorFileName">The Razor file name to search for</param>
    /// <returns>The path to the Razor file if found, null otherwise</returns>
    private string? FindRazorComponentFile(string projectRoot, string razorFileName)
    {
        // Common directories where Razor components are typically located
        var commonDirectories = new[]
        {
            "Components/Pages",
            "Components",
            "Pages", 
            "Views",
            "Areas",
            "src/Components/Pages",
            "src/Components",
            "src/Pages"
        };

        foreach (var dir in commonDirectories)
        {
            var searchPath = _fileSystem.Path.Combine(projectRoot, dir);
            if (_fileSystem.Directory.Exists(searchPath))
            {
                var filePath = _fileSystem.Path.Combine(searchPath, razorFileName);
                if (_fileSystem.File.Exists(filePath))
                {
                    return filePath;
                }
            }
        }

        return null;
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
    /// </summary>
    /// <param name="url">The URL path to create hierarchy from</param>
    /// <returns>Array of hierarchy parts</returns>
    private static string[] CreateHierarchyParts(string url)
    {
        // Remove leading slash and split by forward slashes
        var parts = url.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        // If empty (root path), return empty array
        if (parts.Length == 0)
        {
            return [];
        }

        // For simple single-level paths like "/services" or "/portfolio", 
        // we'll put them at the root level with no hierarchy
        return parts.Length == 1 ? parts : parts[..^1]; // All parts except the last one
    }
}