using System.Diagnostics.CodeAnalysis;

namespace MyLittleContentEngine.Services;

/// <summary>
/// Represents a URL path segment with automatic normalization and validation.
/// Handles leading/trailing slashes consistently and provides safe path combination.
/// </summary>
public readonly struct UrlPath : IEquatable<UrlPath>
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
    /// Gets a value indicating whether this is an absolute path (starts with /).
    /// </summary>
    public bool IsAbsolute => _value?.StartsWith('/') ?? false;

    /// <summary>
    /// Gets a value indicating whether this is a relative path (doesn't start with /).
    /// </summary>
    public bool IsRelative => !IsAbsolute;

    /// <summary>
    /// Gets the empty UrlPath instance.
    /// </summary>
    public static UrlPath Empty { get; } = new(string.Empty);

    /// <summary>
    /// Gets the root UrlPath instance ("/").
    /// </summary>
    public static UrlPath Root { get; } = new("/");

    /// <summary>
    /// Initializes a new instance of the UrlPath struct.
    /// </summary>
    /// <param name="path">The path to normalize and store.</param>
    public UrlPath(string? path)
    {
        _value = NormalizePath(path);
    }

    /// <summary>
    /// Creates a UrlPath from a string.
    /// </summary>
    public static implicit operator UrlPath(string? path) => new(path);

    /// <summary>
    /// Converts a UrlPath to its string representation.
    /// </summary>
    public static implicit operator string(UrlPath path) => path.Value;

    /// <summary>
    /// Combines two URL paths, handling slashes correctly.
    /// </summary>
    public static UrlPath operator /(UrlPath left, UrlPath right)
    {
        return Combine(left, right);
    }

    /// <summary>
    /// Combines multiple URL path segments into a single path.
    /// </summary>
    public static UrlPath Combine(params UrlPath[] paths)
    {
        if (paths.Length == 0)
            return Empty;

        var segments = new List<string>();
        bool isAbsolute = false;

        for (int i = 0; i < paths.Length; i++)
        {
            var path = paths[i];
            if (path.IsEmpty)
                continue;

            // First non-empty path determines if result is absolute
            if (segments.Count == 0 && path.IsAbsolute)
            {
                isAbsolute = true;
            }

            // Split and add non-empty segments
            segments.AddRange(path.GetSegments());
        }

        if (segments.Count == 0)
            return isAbsolute ? Root : Empty;

        var result = string.Join('/', segments);
        return new UrlPath(isAbsolute ? $"/{result}" : result);
    }

    /// <summary>
    /// Ensures the path starts with a forward slash.
    /// </summary>
    public UrlPath EnsureLeadingSlash()
    {
        if (IsEmpty)
            return Root;
        
        if (_value == "/")
            return this;
        
        return IsAbsolute ? this : new UrlPath($"/{_value}");
    }

    /// <summary>
    /// Ensures the path doesn't start with a forward slash.
    /// </summary>
    public UrlPath RemoveLeadingSlash()
    {
        if (IsEmpty || IsRelative)
            return this;
        
        // Special case: "/" becomes "/" (can't remove the only slash)
        if (_value == "/")
            return this;
        
        return new UrlPath(_value.TrimStart('/'));
    }

    /// <summary>
    /// Ensures the path ends with a forward slash.
    /// </summary>
    public UrlPath EnsureTrailingSlash()
    {
        if (IsEmpty)
            return Root;
        
        if (_value == "/")
            return this;
        
        return _value.EndsWith('/') ? this : new UrlPath($"{_value}/");
    }

    /// <summary>
    /// Ensures the path doesn't end with a forward slash.
    /// </summary>
    public UrlPath RemoveTrailingSlash()
    {
        if (IsEmpty || !_value.EndsWith('/'))
            return this;
        
        // Special case: "/" becomes "" (for use in RequestPath which doesn't accept "/")
        if (_value == "/")
            return Empty;
        
        var trimmed = _value.TrimEnd('/');
        return string.IsNullOrEmpty(trimmed) ? Empty : new UrlPath(trimmed);
    }

    /// <summary>
    /// Appends a query string or fragment to the path.
    /// </summary>
    public UrlPath AppendQueryOrFragment(string queryOrFragment)
    {
        if (string.IsNullOrWhiteSpace(queryOrFragment))
            return this;

        if (!queryOrFragment.StartsWith('?') && !queryOrFragment.StartsWith('#'))
            throw new ArgumentException("Query or fragment must start with '?' or '#'", nameof(queryOrFragment));

        return new UrlPath($"{_value}{queryOrFragment}");
    }

    /// <summary>
    /// Gets the parent path (one level up).
    /// </summary>
    public UrlPath GetParent()
    {
        if (IsEmpty)
            return Empty;

        // Special case: root path has no parent
        if (_value == "/")
            return Root;

        var lastSlash = _value.LastIndexOf('/');
        if (lastSlash <= 0)
            return IsAbsolute ? Root : Empty;

        return new UrlPath(_value[..lastSlash]);
    }

    /// <summary>
    /// Gets the last segment of the path.
    /// </summary>
    public string GetLastSegment()
    {
        if (IsEmpty)
            return string.Empty;

        var segments = GetSegments();
        return segments.Length > 0 ? segments[^1] : string.Empty;
    }

    /// <summary>
    /// Gets the path segments as an array, excluding empty segments.
    /// </summary>
    public string[] GetSegments()
    {
        if (IsEmpty)
            return Array.Empty<string>();

        return _value.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Normalizes a path string by handling various edge cases.
    /// </summary>
    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        path = path.Trim();

        // Handle special cases
        if (path == "/" || path == ".")
            return "/";

        // Replace backslashes with forward slashes
        path = path.Replace('\\', '/');

        // Don't collapse double slashes after scheme (http://, https://, etc.)
        if (path.Contains("://"))
        {
            // Split on ://, process each part separately
            var schemeParts = path.Split("://", 2);
            if (schemeParts.Length == 2)
            {
                var scheme = schemeParts[0];
                var rest = schemeParts[1];
                
                // Remove duplicate slashes only in the path part
                while (rest.Contains("//"))
                {
                    rest = rest.Replace("//", "/");
                }
                
                path = $"{scheme}://{rest}";
            }
        }
        else
        {
            // Remove duplicate slashes for regular paths
            while (path.Contains("//"))
            {
                path = path.Replace("//", "/");
            }
        }

        return path;
    }

    public override string ToString() => Value;

    public override bool Equals(object? obj) => obj is UrlPath other && Equals(other);

    public bool Equals(UrlPath other) => string.Equals(_value, other._value, StringComparison.Ordinal);

    public override int GetHashCode() => _value?.GetHashCode() ?? 0;

    public static bool operator ==(UrlPath left, UrlPath right) => left.Equals(right);

    public static bool operator !=(UrlPath left, UrlPath right) => !left.Equals(right);

    /// <summary>
    /// Tries to parse a string as a UrlPath.
    /// </summary>
    public static bool TryParse(string? input, [NotNullWhen(true)] out UrlPath? result)
    {
        try
        {
            result = new UrlPath(input);
            return true;
        }
        catch
        {
            result = Empty;
            return false;
        }
    }
}