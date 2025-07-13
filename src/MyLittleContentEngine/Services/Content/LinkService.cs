namespace MyLittleContentEngine.Services.Content;

public class LinkService
{
    private readonly ContentEngineOptions _options;

    public LinkService(ContentEngineOptions options)
    {
        _options = options;
    }

    public string GetLink(string url)
    {
        return LinkRewriter.RewriteUrl(url, string.Empty, _options.BaseUrl);
    }

    public string GetCanonicalUrl(string url)
    {
        // Check if this is an external URL or special protocol
        var isExternalUrl = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                           url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                           url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                           url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
                           url.StartsWith("ftp:", StringComparison.OrdinalIgnoreCase);
        
        // External URLs and special protocols should be returned unchanged
        if (isExternalUrl)
        {
            return url;
        }
        
        // For relative URLs, we need to provide a base path to resolve against
        // Use "/" as the relativeToUrl so LinkRewriter properly combines with BaseUrl
        var relativeToUrl = url.StartsWith('/') ? string.Empty : "/";
        var rewrittenUrl = LinkRewriter.RewriteUrl(url, relativeToUrl, _options.BaseUrl);
        
        if (string.IsNullOrWhiteSpace(_options.CanonicalBaseUrl))
        {
            return rewrittenUrl;
        }

        // Ensure canonical base URL doesn't end with slash
        var canonicalBase = _options.CanonicalBaseUrl.TrimEnd('/');
        
        // Ensure rewritten URL starts with slash
        if (!rewrittenUrl.StartsWith('/'))
        {
            rewrittenUrl = "/" + rewrittenUrl;
        }

        return canonicalBase + rewrittenUrl;
    }
}