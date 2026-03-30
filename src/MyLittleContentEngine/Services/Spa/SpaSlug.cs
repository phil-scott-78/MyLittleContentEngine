namespace MyLittleContentEngine.Services.Spa;

/// <summary>
/// Converts between SPA slugs and content URLs.
/// Slugs use "index" for the root page; URLs use "/".
/// </summary>
public static class SpaSlug
{
    /// <summary>Converts a slug to a content URL (e.g. "index" → "/", "docs/intro" → "/docs/intro").</summary>
    public static string ToUrl(string slug) => slug is "index" ? "/" : $"/{slug}";

    /// <summary>Converts a content URL to a slug (e.g. "/" → "index", "/docs/intro" → "docs/intro").</summary>
    public static string FromUrl(string url)
    {
        var trimmed = url.Trim('/');
        return string.IsNullOrEmpty(trimmed) ? "index" : trimmed;
    }
}
