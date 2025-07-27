using MyLittleContentEngine.Models;

namespace MultipleContentSourceExample;


internal class DocsFrontMatter : IFrontMatter
{
    /// <summary>Title of the blog post.</summary>
    public string Title { get; init; } = "Empty title";

    public string Description { get; init; } = string.Empty;

    /// <inheritdoc />
    public bool IsDraft { get; init; } = false;

    public string[] Tags { get; init; } = [];
    public string? RedirectUrl { get; init; }

    public int Order { get; init; } = int.MaxValue;

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
