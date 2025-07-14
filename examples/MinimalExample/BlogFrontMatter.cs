using MyLittleContentEngine.Models;

namespace MinimalExample;

public class BlogFrontMatter : IFrontMatter
{
    public string Title { get; init; } = "Empty title";
    public string Description { get; init; } = string.Empty;
    public string? Uid { get; init; } = null;

    public DateTime Date { get; init; } = DateTime.Now;
    public bool IsDraft { get; init; } = false;
    public string[] Tags { get; init; } = [];
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
}