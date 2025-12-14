using System.Diagnostics.CodeAnalysis;

namespace MyLittleContentEngine.Services;

/// <summary>
/// Represents a file system path with automatic normalization.
/// This is a pure value object - for path operations and I/O, use the FilePathOperations service.
/// </summary>
public readonly struct FilePath : IEquatable<FilePath>
{
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
            path = path == "~" ? home : Path.Combine(home, path[2..]);
        }

        return path;
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
            result = new FilePath(input);
            return true;
        }
        catch
        {
            result = Empty;
            return false;
        }
    }
}