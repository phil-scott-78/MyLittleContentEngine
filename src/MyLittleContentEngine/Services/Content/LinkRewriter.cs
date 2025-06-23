using MyLittleContentEngine.Services.Infrastructure;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// Service for handling URL rewriting in Markdown content when converted to HTML.
/// Provides special handling for different link types and path normalization.
/// </summary>
internal static class LinkRewriter
{
    /// <summary>
    /// Determines if the given path is an external URL
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>True if the path is an external URL</returns>
    private static bool IsExternalUrl(string path) =>
        path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("ftp:", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines if the given path is an anchor link
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>True if the path is an anchor link</returns>
    private static bool IsAnchorLink(string path) => path.StartsWith('#');

    /// <summary>
    /// Determines if the given path contains a query string or fragment
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>True if the path contains a query or fragment</returns>
    private static bool ContainsQueryOrFragment(string path) =>
        !IsExternalUrl(path) && (path.Contains('?') || path.Contains('#'));

    /// <summary>
    /// Rewrites a URL based on special rules for different link types
    /// </summary>
    /// <param name="url">URL to rewrite</param>
    /// <param name="relativeToUrl"></param>
    /// <returns>The rewritten URL</returns>
    public static string RewriteUrl(string url, string relativeToUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeToUrl))
        {
            return url.StartsWith('/') 
                ? url[1..] : // Remove leading slash if no base URL is provided
                url;
        }

        // Skip rewriting certain types of URLs
        if (IsExternalUrl(url) || IsAnchorLink(url))
        {
            return url;
        }

        // Handle URLs with query strings or fragments
        if (!ContainsQueryOrFragment(url))
        {
            return GetAbsolutePath(url, relativeToUrl);
        }

        // For URLs with fragments/queries, we need to handle only the path part
        var specialCharPos = url.IndexOfAny(['?', '#']);
        if (specialCharPos <= 0)
        {
            return GetAbsolutePath(url, relativeToUrl);
        }

        var path = url[..specialCharPos];
        var rest = url[specialCharPos..];

        // Only rewrite the path portion
        var newPath = GetAbsolutePath(path, relativeToUrl);
        return newPath + rest;
    }

    /// <summary>
    /// Converts a relative path to an absolute path
    /// </summary>
    /// <param name="relativePath">The relative path to convert</param>
    /// <param name="baseUrl"></param>
    /// <returns>The absolute path</returns>
    private static string GetAbsolutePath(string relativePath, string baseUrl)
    {
        // If the path is already absolute, return it as is
        if (relativePath.StartsWith('/'))
        {
            return relativePath;
        }

        // Handle relative paths that start with "../"
        if (!relativePath.StartsWith("../"))
        {
            // Regular relative path, combine with base URL
            return FileSystemUtilities.CombineUrl(baseUrl, relativePath);
        }

        var baseSegments = baseUrl.Split('/')
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        var relativeSegments = relativePath.Split('/').ToList();

        // Count how many levels to go up
        var levelsUp = 0;
        while (relativeSegments.Count > 0 && relativeSegments[0] == "..")
        {
            levelsUp++;
            relativeSegments.RemoveAt(0);
        }

        // Remove the appropriate number of segments from the base URL
        baseSegments = baseSegments.Take(Math.Max(0, baseSegments.Count - levelsUp)).ToList();

        // Combine the remaining base segments with the relative segments
        var resultSegments = baseSegments.Concat(relativeSegments);
        return baseUrl.StartsWith('/') 
            ? "/" + string.Join("/", resultSegments)
            : string.Join("/", resultSegments);
    }
}