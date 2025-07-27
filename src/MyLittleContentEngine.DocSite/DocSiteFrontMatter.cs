using MyLittleContentEngine.Models;

namespace MyLittleContentEngine.DocSite;

/// <summary>
/// Default front matter class for documentation sites
/// </summary>
public class DocSiteFrontMatter : IFrontMatter
{
    /// <summary>Title of the blog post.</summary>
    public string Title { get; init; } = "Empty title";

    public string Description { get; init; } = string.Empty;

    /// <inheritdoc />
    public bool IsDraft { get; init; } = false;

    public string[] Tags { get; init; } = [];
    
    public int Order { get; init; } = int.MaxValue;
    
    public string? RedirectUrl { get; init; }
    
    // probably should make this optional
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

    public string? Uid { get; init; } = null;
}