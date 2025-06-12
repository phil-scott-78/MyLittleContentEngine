using System.IO.Abstractions;

namespace MyLittleContentEngine.Services.Infrastructure;

/// <summary>
/// Provides utilities for working with paths and URLs in MyLittleContentEngine.
/// </summary>
internal class PathUtilities(IFileSystem fileSystem)
{
    /// <summary>
    /// Converts a file path to a URL-friendly path.
    /// </summary>
    /// <param name="filePath">The file path to convert.</param>
    /// <param name="baseContentPath">The base content path to make the path relative to.</param>
    /// <returns>A URL-friendly relative path.</returns>
    public string FilePathToUrlPath(string filePath, string baseContentPath)
    {
        var relativePath = fileSystem.Path.GetRelativePath(baseContentPath, filePath);
        var directoryPath = fileSystem.Path.GetDirectoryName(relativePath) ?? string.Empty;
        var fileNameWithoutExtension = fileSystem.Path.GetFileNameWithoutExtension(relativePath).Slugify();

        return fileSystem.Path.Combine(directoryPath, fileNameWithoutExtension).Replace(Path.DirectorySeparatorChar, '/');
    }

    /// <summary>
    /// Combines a base URL with a relative path to create a complete URL.
    /// </summary>
    /// <param name="baseUrl">The base URL.</param>
    /// <param name="relativePath">The relative path to append.</param>
    /// <returns>A complete URL.</returns>
    public static string CombineUrl(string baseUrl, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return baseUrl;
        }
        
        if (!baseUrl.EndsWith('/') && (relativePath.StartsWith('#') || relativePath.StartsWith('?')))
        {
            return $"{baseUrl}{relativePath}";
        }
        
        baseUrl = baseUrl.TrimEnd('/');
        relativePath = relativePath.Trim('/');

        return $"{baseUrl}/{relativePath}";
    }

    /// <summary>
    /// Gets all files matching a pattern in a directory.
    /// </summary>
    /// <param name="directoryPath">The directory to search.</param>
    /// <param name="pattern">The file pattern to match.</param>
    /// <param name="recursive">Whether to search subdirectories.</param>
    /// <returns>A tuple with the array of file paths and the absolute directory path.</returns>
    public (string[] Files, string AbsolutePath) GetFilesInDirectory(
        string directoryPath,
        string pattern,
        bool recursive = true)
    {
        // Configure enumeration options for directory search
        EnumerationOptions enumerationOptions = new()
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = recursive
        };

        // Get all files matching the pattern and return with the content path
        return (fileSystem.Directory.GetFiles(directoryPath, pattern, enumerationOptions), directoryPath);
    }

    /// <summary>
    /// Validates a path and ensures it exists.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="createIfNotExists">Whether to create the directory if it doesn't exist.</param>
    /// <returns>The absolute path.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory doesn't exist and createIfNotExists is false.</exception>
    public string ValidateDirectoryPath(string path, bool createIfNotExists = false)
    {
        var fullPath = fileSystem.Path.GetFullPath(path);

        if (fileSystem.Directory.Exists(fullPath)) return fullPath;

        if (createIfNotExists)
        {
            fileSystem.Directory.CreateDirectory(fullPath);
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
    public string Combine(string basePageUrl, string relativePath)
    {
        return fileSystem.Path.Combine(basePageUrl, relativePath);
    }
}