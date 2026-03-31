using MyLittleContentEngine.Models;

namespace MyLittleContentEngine.DocSite;

/// <summary>
/// Default front matter class for documentation sites
/// </summary>
public class DocSiteFrontMatter : IFrontMatter
{
    /// <summary>Title of the blog post.</summary>
    public string Title { get; init; } = "Empty title";

    /// <summary>Gets the page description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <inheritdoc />
    public bool IsDraft { get; init; } = false;

    /// <inheritdoc />
    public string[] Tags { get; init; } = [];

    /// <summary>Gets the sort order for navigation.</summary>
    public int Order { get; init; } = int.MaxValue;

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
            LastMod = DateTime.MinValue,
            RssItem = false,
            Order = Order
        };
    }

    /// <inheritdoc />
    public string? Uid { get; init; } = null;
}