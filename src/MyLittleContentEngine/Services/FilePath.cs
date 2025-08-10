using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using Testably.Abstractions;

namespace MyLittleContentEngine.Services;

/// <summary>
/// Represents a file system path with automatic normalization and validation.
/// Handles path separators consistently across platforms and provides safe path operations.
/// This is a pure value object - for I/O operations, use the extension methods in FilePathExtensions.
/// </summary>
public readonly struct FilePath : IEquatable<FilePath>
{
    internal static IFileSystem FileSystem = new RealFileSystem();

    private readonly string _value;

    /// <summary>
    /// Gets the normalized path value.
    /// </summary>
    public string Value => _value ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether this path is empty.
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(_value);

    /// <summary>
    /// Gets a value indicating whether this is an absolute path.
    /// </summary>
    public bool IsAbsolute => !IsEmpty && FileSystem.Path.IsPathRooted(_value);

    /// <summary>
    /// Gets a value indicating whether this is a relative path.
    /// </summary>
    public bool IsRelative => !IsAbsolute;

    /// <summary>
    /// Gets the empty FilePath instance.
    /// </summary>
    public static FilePath Empty { get; } = new(string.Empty);

    /// <summary>
    /// Initializes a new instance of the FilePath struct.
    /// </summary>
    /// <param name="path">The path to normalize and store.</param>
    public FilePath(string? path)
    {
        _value = NormalizePath(path);
    }

    /// <summary>
    /// Creates a FilePath from a string.
    /// </summary>
    public static implicit operator FilePath(string? path) => new(path);

    /// <summary>
    /// Converts a FilePath to its string representation.
    /// </summary>
    public static implicit operator string(FilePath path) => path.Value;

    /// <summary>
    /// Combines two file paths using the appropriate path separator.
    /// </summary>
    public static FilePath operator /(FilePath left, FilePath right)
    {
        return Combine(left, right);
    }

    /// <summary>
    /// Combines multiple file path segments into a single path.
    /// </summary>
    public static FilePath Combine(params FilePath[] paths)
    {
        if (paths.Length == 0)
            return Empty;

        var nonEmptyPaths = paths
            .Where(p => !p.IsEmpty)
            .Select(p => p.Value)
            .ToArray();

        if (nonEmptyPaths.Length == 0)
            return Empty;

        return new FilePath(FileSystem.Path.Combine(nonEmptyPaths));
    }

    /// <summary>
    /// Gets the directory containing this file path.
    /// </summary>
    public FilePath GetDirectory()
    {
        if (IsEmpty)
            return Empty;

        var directory = FileSystem.Path.GetDirectoryName(_value);
        return new FilePath(directory);
    }

    /// <summary>
    /// Gets the file name without the directory path.
    /// </summary>
    public string GetFileName()
    {
        if (IsEmpty)
            return string.Empty;

        return FileSystem.Path.GetFileName(_value);
    }

    /// <summary>
    /// Gets the file name without the extension.
    /// </summary>
    public string GetFileNameWithoutExtension()
    {
        if (IsEmpty)
            return string.Empty;

        return FileSystem.Path.GetFileNameWithoutExtension(_value);
    }

    /// <summary>
    /// Gets the file extension including the period (e.g., ".txt").
    /// </summary>
    public string GetExtension()
    {
        if (IsEmpty)
            return string.Empty;

        return FileSystem.Path.GetExtension(_value);
    }

    /// <summary>
    /// Changes the file extension.
    /// </summary>
    public FilePath ChangeExtension(string extension)
    {
        if (IsEmpty)
            return Empty;

        return new FilePath(FileSystem.Path.ChangeExtension(_value, extension));
    }

    /// <summary>
    /// Gets the relative path from a base path to this path.
    /// </summary>
    public FilePath GetRelativeTo(FilePath basePath)
    {
        if (IsEmpty || basePath.IsEmpty)
            return Empty;

        var relativePath = FileSystem.Path.GetRelativePath(basePath.Value, _value);
        return new FilePath(relativePath);
    }

    /// <summary>
    /// Gets the full absolute path, resolving any relative segments.
    /// </summary>
    public FilePath GetFullPath()
    {
        if (IsEmpty)
            return Empty;

        return new FilePath(FileSystem.Path.GetFullPath(_value));
    }

    /// <summary>
    /// Converts this file path to a URL-friendly path.
    /// </summary>
    public UrlPath ToUrlPath()
    {
        if (IsEmpty)
            return UrlPath.Empty;

        // Replace backslashes with forward slashes and ensure consistent separators
        var urlPath = _value.Replace('\\', '/');
        return new UrlPath(urlPath);
    }

    /// <summary>
    /// Creates a FilePath from a UrlPath.
    /// </summary>
    public static FilePath FromUrlPath(UrlPath urlPath)
    {
        if (urlPath.IsEmpty)
            return Empty;

        // Convert forward slashes to the appropriate path separator
        var filePath = urlPath.Value.Replace('/', FileSystem.Path.DirectorySeparatorChar);
        return new FilePath(filePath);
    }

    /// <summary>
    /// Gets the parent directory path.
    /// For a file path, returns the directory containing the file.
    /// For a directory path, returns the parent directory.
    /// </summary>
    public FilePath GetParent()
    {
        if (IsEmpty)
            return Empty;

        return GetDirectory();
    }

    /// <summary>
    /// Removes leading slashes from the path.
    /// Useful when converting URL paths to file paths for combining with base directories.
    /// </summary>
    public FilePath RemoveLeadingSlash()
    {
        if (IsEmpty || !_value.StartsWith('/') && !_value.StartsWith('\\'))
            return this;

        return new FilePath(_value.TrimStart('/', '\\'));
    }

    /// <summary>
    /// Normalizes a file path string by handling various edge cases.
    /// </summary>
    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        path = path.Trim();

        // Expand environment variables
        path = Environment.ExpandEnvironmentVariables(path);

        // Handle tilde for home directory (Unix-like systems)
        if (path.StartsWith("~/") || path == "~")
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = path == "~" ? home : FileSystem.Path.Combine(home, path[2..]);
        }

        return path;
    }

    /// <summary>
    /// Validates that the path doesn't contain invalid characters.
    /// </summary>
    public bool IsValid()
    {
        if (IsEmpty)
            return true;

        try
        {
            var invalidChars = FileSystem.Path.GetInvalidPathChars();
            return !_value.Any(c => invalidChars.Contains(c));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Combines this path with additional segments.
    /// </summary>
    public FilePath Combine(params string[] segments)
    {
        if (segments.Length == 0)
            return this;

        var allSegments = new List<string> { _value };
        allSegments.AddRange(segments.Where(s => !string.IsNullOrWhiteSpace(s)));
        
        return new FilePath(FileSystem.Path.Combine(allSegments.ToArray()));
    }

    public override string ToString() => Value;

    public override bool Equals(object? obj) => obj is FilePath other && Equals(other);

    public bool Equals(FilePath other)
    {
        // Use case-insensitive comparison on Windows, case-sensitive on Unix
        var comparison = OperatingSystem.IsWindows() 
            ? StringComparison.OrdinalIgnoreCase 
            : StringComparison.Ordinal;
            
        return string.Equals(_value, other._value, comparison);
    }

    public override int GetHashCode()
    {
        if (_value == null) return 0;
        
        var comparison = OperatingSystem.IsWindows() 
            ? StringComparer.OrdinalIgnoreCase 
            : StringComparer.Ordinal;
            
        return comparison.GetHashCode(_value);
    }

    public static bool operator ==(FilePath left, FilePath right) => left.Equals(right);

    public static bool operator !=(FilePath left, FilePath right) => !left.Equals(right);

    /// <summary>
    /// Tries to parse a string as a FilePath.
    /// </summary>
    public static bool TryParse(string? input, [NotNullWhen(true)] out FilePath? result)
    {
        try
        {
            var o = new FilePath(input);
            if (o.IsValid())
            {
                result = o;
                return true;
            }

            result = Empty;
            return false;
        }
        catch
        {
            result = Empty;
            return false;
        }
    }
}