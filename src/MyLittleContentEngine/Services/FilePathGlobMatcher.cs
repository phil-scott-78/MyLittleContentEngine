using System.Collections.Immutable;
using Microsoft.Extensions.FileSystemGlobbing;

namespace MyLittleContentEngine.Services;

/// <summary>
/// Provides glob pattern matching for file paths to determine if they should be ignored during content operations.
/// </summary>
/// <remarks>
/// <para>
/// This helper uses Microsoft.Extensions.FileSystemGlobbing to support glob patterns including:
/// </para>
/// <list type="bullet">
///     <item><description><c>*</c> - Matches any characters except directory separators</description></item>
///     <item><description><c>**</c> - Matches any directory depth</description></item>
///     <item><description><c>?</c> - Matches a single character</description></item>
/// </list>
/// <para>
/// Matching is case-insensitive by default, consistent with FilePath behavior on Windows.
/// Exact paths without wildcards are also supported for backward compatibility.
/// </para>
/// <para>
/// Examples:
/// </para>
/// <list type="bullet">
///     <item><description><c>*.md</c> - Matches all markdown files</description></item>
///     <item><description><c>**/*.razor</c> - Matches razor files at any depth</description></item>
///     <item><description><c>temp/*.txt</c> - Matches text files in temp directory</description></item>
///     <item><description><c>app.css</c> - Exact path match</description></item>
/// </list>
/// </remarks>
internal static class FilePathGlobMatcher
{
    /// <summary>
    /// Determines whether a file path should be ignored based on a collection of glob patterns.
    /// </summary>
    /// <param name="relativePath">The relative path to check (relative to content root)</param>
    /// <param name="patterns">Collection of glob patterns to match against</param>
    /// <returns>True if the path matches any ignore pattern; otherwise false</returns>
    /// <remarks>
    /// <para>
    /// This method performs in-memory pattern matching without any file system I/O.
    /// Matching is case-insensitive by default.
    /// </para>
    /// <para>
    /// If the patterns collection is empty, this method returns false (no paths are ignored).
    /// </para>
    /// </remarks>
    public static bool IsIgnored(string relativePath, ImmutableList<FilePath> patterns)
    {
        if (patterns.Count == 0)
        {
            return false;
        }

        // Normalize path separators to forward slashes for glob matching
        // Glob patterns typically use forward slashes
        var normalizedPath = relativePath.Replace('\\', '/');

        // Create matcher with case-insensitive matching (default behavior)
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);

        // Add all patterns as excludes - we want to know if any pattern matches this path
        // Note: We use Include + !Match pattern because Matcher is designed for "include these, exclude those"
        // but we want "does this path match any of these patterns"
        matcher.AddInclude("**/*"); // Include everything by default
        matcher.AddExcludePatterns(patterns.Select(p => p.Value));

        // Match returns whether the file is INCLUDED (not excluded)
        // So if it's NOT included, it means it matched an exclude pattern
        var result = matcher.Match(normalizedPath);

        return !result.HasMatches;
    }
}
