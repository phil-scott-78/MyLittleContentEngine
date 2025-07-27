using MyLittleContentEngine.Models;

namespace MultipleContentSourceExample;

public class ContentFrontMatter : IFrontMatter
{
    public string Title { get; init; } = "Untitled";
    public int Order { get; init; }
    public string[] Tags { get; init; } = [];
    public bool IsDraft { get; init; }
    public string? Uid { get; init; }
    public string? RedirectUrl { get; init; }
    
    public Metadata AsMetadata()
    {
        return new Metadata()
        {
            Title = Title,
            Description = string.Empty,
            LastMod = DateTime.MaxValue,
            Order = Order,
            RssItem = false
        };
    }
}