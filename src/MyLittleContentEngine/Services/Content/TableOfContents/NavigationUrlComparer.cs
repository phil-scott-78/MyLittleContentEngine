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
        if (url == "/") return "/index";
        if (!url.StartsWith('/'))
        {
            url = $"/{url}";
        }

        if (url.EndsWith('/'))
        {
            url += "index";
        }

        return url.ToLowerInvariant();
    }
}