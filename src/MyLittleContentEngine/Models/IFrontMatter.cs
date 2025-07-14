namespace MyLittleContentEngine.Models;

/// <summary>
///     Interface for front matter. FrontMatter is the metadata of a Markdown content page.
/// </summary>
public interface IFrontMatter
{
    /// <summary>
    /// The title of the content page.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Tags for the content.
    /// </summary>
    string[] Tags { get; init; }
    
    /// <summary>
    /// If true, the content page will not be generated.
    /// </summary>
    bool IsDraft { get; init; }

    /// <summary>
    /// Currently unused, but maybe one day we can support xref links.
    /// </summary>
    string? Uid { get; init; }
    
    string? RedirectUrl { get; init; }
    
    /// <summary>
    /// Converts the FrontMatter into structured metadata for RSS and SiteMap generation.
    /// </summary>
    public Metadata AsMetadata();
}