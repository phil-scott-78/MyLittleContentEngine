namespace MyLittleContentEngine.Services.Content;

public class LinkService(ContentEngineOptions options)
{
    public string GetLink(string url)
    {
        return LinkRewriter.RewriteUrl(url, options.BaseUrl);
    }
}