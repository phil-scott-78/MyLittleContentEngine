using System.Collections.Concurrent;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Infrastructure;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// Service for discovering and managing folder-level metadata files (_index.metadata.yml).
/// Provides metadata lookup for folders to enable custom display names and ordering in navigation.
/// </summary>
internal class FolderMetadataService
{
    private const string FolderMetadataFileName = "_index.metadata.yml";

    private readonly AsyncLazy<ConcurrentDictionary<string, Metadata>> _metadataCache;
    private readonly IEnumerable<IContentOptions> _contentOptions;
    private readonly ContentEngineOptions _contentEngineOptions;
    private readonly FileSystemUtilities _fileSystemUtilities;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<FolderMetadataService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FolderMetadataService"/> class.
    /// </summary>
    /// <param name="contentOptions">The collection of registered content services to discover folder roots.</param>
    /// <param name="contentEngineOptions">Configuration options containing the YAML deserializer.</param>
    /// <param name="fileSystemUtilities">Utilities for file system operations.</param>
    /// <param name="fileSystem">File system abstraction for testability.</param>
    /// <param name="logger">Logger instance.</param>
    public FolderMetadataService(
        IEnumerable<IContentOptions> contentOptions,
        ContentEngineOptions contentEngineOptions,
        FileSystemUtilities fileSystemUtilities,
        IFileSystem fileSystem,
        ILogger<FolderMetadataService> logger)
    {
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
