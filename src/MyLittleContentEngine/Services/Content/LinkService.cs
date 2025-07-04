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

    public string GetLink(string url, string relativeToUrl)
    {
        return LinkRewriter.RewriteUrl(url, relativeToUrl, _options.BaseUrl);
    }
}