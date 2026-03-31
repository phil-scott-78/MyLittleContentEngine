using MyLittleContentEngine.Models;

namespace MyLittleContentEngine.BlogSite;

/// <summary>
/// Default front matter class for blog sites
/// </summary>
public class BlogSiteFrontMatter : IFrontMatter
{
    /// <inheritdoc />
    public string Title { get; init; } = "Empty title";

    /// <summary>Gets the post author name.</summary>
    public string Author { get; init; } = "";

    /// <summary>Gets the post description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the associated repository URL.</summary>
    public string Repository { get; init; } = string.Empty;

    /// <summary>Gets the publication date.</summary>
    public DateTime Date { get; init; } = DateTime.Now;

    /// <inheritdoc />
    public bool IsDraft { get; init; } = false;

    /// <inheritdoc />
    public string[] Tags { get; init; } = [];

    /// <summary>Gets the series name this post belongs to.</summary>
    public string Series { get; init; } = string.Empty;

    /// <inheritdoc />
    public string? RedirectUrl { get; init; }

    /// <inheritdoc />
    public string? Section { get; init; }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public string? Uid { get; init; } = null;
}