using MyLittleContentEngine.Models;

namespace Spectre.Console;

/// <summary>
/// Front matter for blog posts in the Spectre.Console documentation
/// </summary>
public class BlogFrontMatter : IFrontMatter
{
    public string Title { get; init; } = "Empty title";
    public string Author { get; init; } = "Spectre.Console Team";
    public string Description { get; init; } = string.Empty;
    public DateTime Date { get; init; } = DateTime.Now;
    public bool IsDraft { get; init; } = false;
    public string[] Tags { get; init; } = [];
    public string Series { get; init; } = string.Empty;
    public string? RedirectUrl { get; init; }
    public string? Section { get; init; }
    public string? Uid { get; init; } = null;
    public string? Repository { get; init; }
    
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