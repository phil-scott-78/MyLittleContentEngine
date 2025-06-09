namespace MyLittleContentEngine.Models;

/// <summary>
///     Interface for front matter. FrontMatter is the metadata of a Markdown content page.
/// </summary>
public interface IFrontMatter
{
    /// <summary>
    /// If true, the content page will not be generated.
    /// </summary>
    bool IsDraft => false;

    /// <summary>
    /// The title of the content page.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Converts the FrontMatter into structured metadata for RSS and SiteMap generation
    /// </summary>
    /// <returns></returns>
    public Metadata AsMetadata();
    
    /// <summary>
    /// Tags for the content.
    /// </summary>
    string[] Tags { get; init; }

    string? Uid { get; init; }
}