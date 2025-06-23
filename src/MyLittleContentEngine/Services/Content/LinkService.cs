namespace MyLittleContentEngine.Services.Content;

public class LinkService
{
    public string GetLink(string url)
    {
        return LinkRewriter.RewriteUrl(url, string.Empty);
    }
}