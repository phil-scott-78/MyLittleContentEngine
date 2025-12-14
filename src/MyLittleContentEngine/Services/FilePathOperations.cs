using System.IO.Abstractions;

namespace MyLittleContentEngine.Services;

/// <summary>
/// Provides path manipulation and I/O operations for FilePath values.
/// This service handles all operations that require IFileSystem abstraction.
/// </summary>
public class FilePathOperations
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the FilePathOperations class.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction to use for all operations.</param>
    public FilePathOperations(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// Determines whether the specified path is an absolute path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>true if the path is absolute; otherwise, false.</returns>
    public bool IsAbsolute(FilePath path)
    {
        return !path.IsEmpty && _fileSystem.Path.IsPathRooted(path.Value);
    }

    /// <summary>
    /// Determines whether the specified path is a relative path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>true if the path is relative; otherwise, false.</returns>
    public bool IsRelative(FilePath path)
    {
        return !IsAbsolute(path);
    }

    /// <summary>
    /// Validates that the path doesn't contain invalid characters.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>true if the path is valid; otherwise, false.</returns>
    public bool IsValid(FilePath path)
    {
        if (path.IsEmpty)
            return true;

        try
        {
            var invalidChars = _fileSystem.Path.GetInvalidPathChars();
            return !path.Value.Any(c => invalidChars.Contains(c));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Combines multiple file path segments into a single path.
    /// </summary>
    /// <param name="paths">The paths to combine.</param>
    /// <returns>The combined path.</returns>
    public FilePath Combine(params FilePath[] paths)
    {
        if (paths.Length == 0)
            return FilePath.Empty;

        var nonEmptyPaths = paths
            .Where(p => !p.IsEmpty)
            .Select(p => p.Value)
            .ToArray();

        if (nonEmptyPaths.Length == 0)
            return FilePath.Empty;

        return new FilePath(_fileSystem.Path.Combine(nonEmptyPaths));
    }

    /// <summary>
    /// Combines a base path with additional string segments.
    /// </summary>
    /// <param name="basePath">The base path.</param>
    /// <param name="segments">Additional path segments to combine.</param>
    /// <returns>The combined path.</returns>
    public FilePath Combine(FilePath basePath, params string[] segments)
    {
        if (segments.Length == 0)
            return basePath;

        var allSegments = new List<string> { basePath.Value };
        allSegments.AddRange(segments.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new FilePath(_fileSystem.Path.Combine(allSegments.ToArray()));
    }

    /// <summary>
    /// Gets the directory containing this file path.
    /// </summary>
    /// <param name="path">The path to get the directory from.</param>
    /// <returns>The directory path.</returns>
    public FilePath GetDirectory(FilePath path)
    {
        if (path.IsEmpty)
            return FilePath.Empty;

        var directory = _fileSystem.Path.GetDirectoryName(path.Value);
        return new FilePath(directory);
    }

    /// <summary>
    /// Gets the file name without the directory path.
    /// </summary>
    /// <param name="path">The path to get the file name from.</param>
    /// <returns>The file name.</returns>
    public string GetFileName(FilePath path)
    {
        if (path.IsEmpty)
            return string.Empty;

        return _fileSystem.Path.GetFileName(path.Value);
    }

    /// <summary>
    /// Gets the file name without the extension.
    /// </summary>
    /// <param name="path">The path to get the file name from.</param>
    /// <returns>The file name without extension.</returns>
    public string GetFileNameWithoutExtension(FilePath path)
    {
        if (path.IsEmpty)
            return string.Empty;

        return _fileSystem.Path.GetFileNameWithoutExtension(path.Value);
    }

    /// <summary>
    /// Gets the file extension including the period (e.g., ".txt").
    /// </summary>
    /// <param name="path">The path to get the extension from.</param>
    /// <returns>The file extension.</returns>
    public string GetExtension(FilePath path)
    {
        if (path.IsEmpty)
            return string.Empty;

        return _fileSystem.Path.GetExtension(path.Value);
    }

    /// <summary>
    /// Changes the file extension.
    /// </summary>
    /// <param name="path">The path to change the extension of.</param>
    /// <param name="extension">The new extension.</param>
    /// <returns>The path with the new extension.</returns>
    public FilePath ChangeExtension(FilePath path, string extension)
    {
        if (path.IsEmpty)
            return FilePath.Empty;

        return new FilePath(_fileSystem.Path.ChangeExtension(path.Value, extension));
    }

    /// <summary>
    /// Gets the relative path from a base path to this path.
    /// </summary>
    /// <param name="path">The target path.</param>
    /// <param name="basePath">The base path.</param>
    /// <returns>The relative path.</returns>
    public FilePath GetRelativeTo(FilePath path, FilePath basePath)
    {
        if (path.IsEmpty || basePath.IsEmpty)
            return FilePath.Empty;

        var relativePath = _fileSystem.Path.GetRelativePath(basePath.Value, path.Value);
        return new FilePath(relativePath);
    }

    /// <summary>
    /// Gets the full absolute path, resolving any relative segments.
    /// </summary>
    /// <param name="path">The path to get the full path for.</param>
    /// <returns>The full absolute path.</returns>
    public FilePath GetFullPath(FilePath path)
    {
        if (path.IsEmpty)
            return FilePath.Empty;

        return new FilePath(_fileSystem.Path.GetFullPath(path.Value));
    }

    /// <summary>
    /// Creates a FilePath from a UrlPath.
    /// </summary>
    /// <param name="urlPath">The URL path to convert.</param>
    /// <returns>The converted file path.</returns>
    public FilePath FromUrlPath(UrlPath urlPath)
    {
        if (urlPath.IsEmpty)
            return FilePath.Empty;

        // Convert forward slashes to the appropriate path separator
        var filePath = urlPath.Value.Replace('/', _fileSystem.Path.DirectorySeparatorChar);
        return new FilePath(filePath);
    }

    /// <summary>
    /// Gets the parent directory path.
    /// For a file path, returns the directory containing the file.
    /// For a directory path, returns the parent directory.
    /// </summary>
    /// <param name="path">The path to get the parent of.</param>
    /// <returns>The parent directory path.</returns>
    public FilePath GetParent(FilePath path)
    {
        if (path.IsEmpty)
            return FilePath.Empty;

        return GetDirectory(path);
    }

    /// <summary>
    /// Checks if the file exists on the file system.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>true if the file exists; otherwise, false.</returns>
    public bool FileExists(FilePath path)
    {
        if (path.IsEmpty)
            return false;

        return _fileSystem.File.Exists(path.Value);
    }

    /// <summary>
    /// Checks if the directory exists on the file system.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>true if the directory exists; otherwise, false.</returns>
    public bool DirectoryExists(FilePath path)
    {
        if (path.IsEmpty)
            return false;

        return _fileSystem.Directory.Exists(path.Value);
    }

    /// <summary>
    /// Ensures the directory for this file path exists, creating it if necessary.
    /// </summary>
    /// <param name="path">The path to ensure the directory exists for.</param>
    public void EnsureDirectoryExists(FilePath path)
    {
        if (path.IsEmpty)
            return;

        var directory = GetDirectory(path);
        if (!directory.IsEmpty && !DirectoryExists(directory))
        {
            _fileSystem.Directory.CreateDirectory(directory.Value);
        }
    }

    /// <summary>
    /// Gets the parent directory path using file system operations.
    /// </summary>
    /// <param name="path">The path to get the parent directory of.</param>
    /// <returns>The parent directory path.</returns>
    public FilePath GetParentDirectory(FilePath path)
    {
        if (path.IsEmpty)
            return FilePath.Empty;

        var parent = _fileSystem.Directory.GetParent(path.Value)?.FullName;
        return new FilePath(parent);
    }
}
