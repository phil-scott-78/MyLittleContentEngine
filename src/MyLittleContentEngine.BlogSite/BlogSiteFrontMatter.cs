using MyLittleContentEngine.Models;

namespace MyLittleContentEngine.BlogSite;

/// <summary>
/// Default front matter class for blog sites
/// </summary>
public class BlogSiteFrontMatter : IFrontMatter
{
    
    public string Title { get; init; } = "Empty title";

    public string Author { get; init; } = "";

    public string Description { get; init; } = string.Empty;

    public string Repository { get; init; } = string.Empty;

    public DateTime Date { get; init; } = DateTime.Now;

    
    public bool IsDraft { get; init; } = false;

    public string[] Tags { get; init; } = [];

    public string Series { get; init; } = string.Empty;

    public string? RedirectUrl { get; init; }
    
    public Metadata AsMetadata()
    {
        return new Metadata()
        {
            Title = Title,
            Description = Description,
            LastMod = Date,
            RssItem = true
        };
    }

    public string? Uid { get; init; } = null;
}