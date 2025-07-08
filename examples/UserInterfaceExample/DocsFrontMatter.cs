using MyLittleContentEngine.Models;

namespace UserInterfaceExample;

public class DocsFrontMatter : IFrontMatter
{
    public string Title { get; init; } = "Empty title";
    public string Description { get; init; } = string.Empty;
    public bool IsDraft { get; init; } = false;
    public string[] Tags { get; init; } = [];
    public int Order { get; init; } = int.MaxValue;
    public string? Uid { get; init; } = null;
    
    public Metadata AsMetadata()
    {
        return new Metadata()
        {
            Title = Title,
            Description = Description,
            LastMod = DateTime.MinValue,
            RssItem = false,
            Order = Order
        };
    }
}