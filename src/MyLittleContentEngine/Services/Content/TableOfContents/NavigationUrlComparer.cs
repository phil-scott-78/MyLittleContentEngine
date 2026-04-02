namespace MyLittleContentEngine.Services.Content.TableOfContents;

internal static class NavigationUrlComparer
{
    public static bool AreEqual(string? url1, string? url2)
    {
        if (url1 == null && url2 == null) return true;
        if (url1 == null || url2 == null) return false;

        return NormalizeForNavigation(url1).Equals(NormalizeForNavigation(url2));
    }

    private static string NormalizeForNavigation(string url)
    {
        if (!url.StartsWith('/'))
        {
            url = $"/{url}";
        }

        url = url.TrimEnd('/');
        if (url.Length == 0) url = "/";

        // Treat /index as equivalent to root /
        if (url.Equals("/index", StringComparison.OrdinalIgnoreCase))
        {
            url = "/";
        }

        return url.ToLowerInvariant();
    }
}