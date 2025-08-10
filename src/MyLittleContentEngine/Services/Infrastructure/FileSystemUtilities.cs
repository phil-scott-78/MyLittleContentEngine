using System.IO;
using System.IO.Abstractions;
using Testably.Abstractions;

namespace MyLittleContentEngine.Services.Infrastructure;

/// <summary>
/// Provides utilities for working with paths and URLs in MyLittleContentEngine.
/// </summary>
internal class FileSystemUtilities(IFileSystem fileSystem)
{
    /// <summary>
    /// Converts a file path to a URL-friendly path.
    /// </summary>
    /// <param name="filePath">The file path to convert.</param>
    /// <param name="baseContentPath">The base content path to make the path relative to.</param>
    /// <returns>A URL-friendly relative path.</returns>
    public UrlPath FilePathToUrlPath(FilePath filePath, FilePath baseContentPath)
    {
        if (filePath.IsEmpty || baseContentPath.IsEmpty)
            return UrlPath.Empty;

        var relativePath = filePath.GetRelativeTo(baseContentPath);
        var directoryPath = relativePath.GetDirectory();
        var fileNameWithoutExtension = relativePath.GetFileNameWithoutExtension().Slugify();

        var result = FilePath.Combine(directoryPath, fileNameWithoutExtension);
        return result.ToUrlPath();
    }

    /// <summary>
    /// Combines a base URL with a relative path to create a complete URL.
    /// </summary>
    /// <param name="baseUrl">The base URL.</param>
    /// <param name="relativePath">The relative path to append.</param>
    /// <returns>A complete URL.</returns>
    public static UrlPath CombineUrl(UrlPath baseUrl, UrlPath relativePath)
    {
        if (relativePath.IsEmpty)
            return baseUrl;

        // Handle query strings and fragments specially
        var relativeValue = relativePath.Value;
        if (relativeValue.StartsWith('#') || relativeValue.StartsWith('?'))
        {
            return baseUrl.RemoveTrailingSlash().AppendQueryOrFragment(relativeValue);
        }

        // For full URLs (with scheme), handle them differently than relative paths
        var baseValue = baseUrl.Value;
        
        // Remove trailing slash from base and leading slash from relative
        baseValue = baseValue.TrimEnd('/');
        relativeValue = relativeValue.Trim('/');  // Trim both leading and trailing slashes
        
        // Combine with a single slash
        return new UrlPath($"{baseValue}/{relativeValue}");
    }

    /// <summary>
    /// Gets all files matching a pattern in a directory.
    /// </summary>
    /// <param name="directoryPath">The directory to search.</param>
    /// <param name="pattern">The file pattern to match. Can be semicolon-separated for multiple patterns (e.g., "*.md;*.mdx").</param>
    /// <param name="recursive">Whether to search subdirectories.</param>
    /// <returns>A tuple with the array of file paths and the absolute directory path.</returns>
    public (FilePath[] Files, FilePath AbsolutePath) GetFilesInDirectory(
        FilePath directoryPath,
        string pattern,
        bool recursive = true)
    {
        // Configure enumeration options for directory search
        EnumerationOptions enumerationOptions = new()
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = recursive
        };

        // Split the pattern by semicolon to support multiple patterns
        var patterns = pattern.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var allFiles = new List<string>();

        foreach (var singlePattern in patterns)
        {
            var trimmedPattern = singlePattern.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedPattern))
            {
                var files = fileSystem.Directory.GetFiles(directoryPath.Value, trimmedPattern, enumerationOptions);
                allFiles.AddRange(files);
            }
        }

        // Remove duplicates and sort
        var uniqueFiles = allFiles.Distinct().OrderBy(f => f).Select(f => new FilePath(f)).ToArray();

        return (uniqueFiles, directoryPath);
    }

    /// <summary>
    /// Validates a path and ensures it exists.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="createIfNotExists">Whether to create the directory if it doesn't exist.</param>
    /// <returns>The absolute path.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory doesn't exist and createIfNotExists is false.</exception>
    public FilePath ValidateDirectoryPath(FilePath path, bool createIfNotExists = false)
    {
        var fullPath = path.GetFullPath();

        if (fileSystem.Directory.Exists(fullPath.Value)) return fullPath;

        if (createIfNotExists)
        {
            fileSystem.Directory.CreateDirectory(fullPath.Value);
        }
        else
        {
            throw new DirectoryNotFoundException($"Directory not found: {fullPath}");
        }

        return fullPath;
    }

    /// <summary>
    /// Combines a base page URL with a relative path.
    /// </summary>
    /// <param name="basePageUrl"></param>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    public FilePath Combine(FilePath basePageUrl, FilePath relativePath)
    {
        return FilePath.Combine(basePageUrl, relativePath);
    }
}