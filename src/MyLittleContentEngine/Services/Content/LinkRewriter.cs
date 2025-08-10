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
        path.StartsWith("ftp:", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("xref:", StringComparison.OrdinalIgnoreCase);


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
        return RewriteUrl(url, relativeToUrl, string.Empty);
    }

    /// <summary>
    /// Rewrites a URL based on special rules for different link types, with BaseUrl support
    /// </summary>
    /// <param name="url">URL to rewrite</param>
    /// <param name="relativeToUrl">The URL to resolve relative paths against</param>
    /// <param name="baseUrl">Base URL to prepend to absolute paths (e.g., "/MyLittleContentEngine/")</param>
    /// <returns>The rewritten URL</returns>
    public static string RewriteUrl(string url, string relativeToUrl, string baseUrl)
    {
        // Skip rewriting certain types of URLs
        if (IsExternalUrl(url) || IsAnchorLink(url))
        {
            return url;
        }

        // Handle URLs with query strings or fragments
        if (!ContainsQueryOrFragment(url))
        {
            return GetAbsolutePathWithBaseUrl(url, relativeToUrl, baseUrl);
        }

        // For URLs with fragments/queries, we need to handle only the path part
        var specialCharPos = url.IndexOfAny(['?', '#']);
        if (specialCharPos <= 0)
        {
            return GetAbsolutePathWithBaseUrl(url, relativeToUrl, baseUrl);
        }

        var path = url[..specialCharPos];
        var rest = url[specialCharPos..];

        // Only rewrite the path portion
        var newPath = GetAbsolutePathWithBaseUrl(path, relativeToUrl, baseUrl);
        return newPath + rest;
    }

    /// <summary>
    /// Converts a relative path to an absolute path with BaseUrl support
    /// </summary>
    /// <param name="relativePath">The relative path to convert</param>
    /// <param name="relativeToUrl">The URL to resolve relative paths against</param>
    /// <param name="baseUrl">Base URL to prepend to absolute paths (e.g., "/MyLittleContentEngine")</param>
    /// <returns>The absolute path with BaseUrl prepended if applicable</returns>
    private static string GetAbsolutePathWithBaseUrl(string relativePath, string relativeToUrl, string baseUrl)
    {
        // Handle case where no relative base is provided
        if (string.IsNullOrWhiteSpace(relativeToUrl))
        {
            // If it's an absolute path, prepend BaseUrl
            if (relativePath.StartsWith('/'))
            {
                return PrependBaseUrl(relativePath, baseUrl);
            }

            // If it's a relative path with no base, return as-is (or remove leading slash for legacy behavior)
            return relativePath;
        }

        // If the path is already absolute, prepend BaseUrl
        if (relativePath.StartsWith('/'))
        {
            return PrependBaseUrl(relativePath, baseUrl);
        }

        // Handle relative paths that start with "../"
        if (!relativePath.StartsWith("../"))
        {
            // Regular relative path, combine with base URL then prepend BaseUrl
            var combinedPath = FileSystemUtilities.CombineUrl(relativeToUrl, relativePath);
            return PrependBaseUrl(combinedPath, baseUrl);
        }

        var baseSegments = relativeToUrl.Split('/')
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
        var resultPath = relativeToUrl.StartsWith('/')
            ? "/" + string.Join("/", resultSegments)
            : string.Join("/", resultSegments);

        return PrependBaseUrl(resultPath, baseUrl);
    }

    /// <summary>
    /// Prepends the BaseUrl to an absolute path if BaseUrl is provided
    /// </summary>
    /// <param name="path">The absolute path to prepend BaseUrl to</param>
    /// <param name="baseUrl">The BaseUrl to prepend (e.g., "/MyLittleContentEngine")</param>
    /// <returns>The path with BaseUrl prepended if applicable</returns>
    private static string PrependBaseUrl(string path, string baseUrl)
    {
        // If no BaseUrl is provided, return the path as-is
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return path;
        }

        // Ensure path starts with /
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        // Normalize BaseUrl: ensure it starts with / but doesn't end with /
        var normalizedBaseUrl = baseUrl.StartsWith('/') ? baseUrl : "/" + baseUrl;
        if (normalizedBaseUrl.EndsWith('/') && normalizedBaseUrl.Length > 1)
        {
            normalizedBaseUrl = normalizedBaseUrl[..^1];
        }

        // Special case: if BaseUrl is just "/" (root), don't prepend anything
        if (normalizedBaseUrl == "/")
        {
            return path;
        }

        // If path is just "/", return BaseUrl with trailing slash
        if (path == "/")
        {
            return normalizedBaseUrl + "/";
        }

        // Combine BaseUrl with path
        return normalizedBaseUrl + path;
    }
}