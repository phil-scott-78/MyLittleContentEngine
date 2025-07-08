using System.Collections.Immutable;

namespace MyLittleContentEngine.Models;

/// <summary>
/// A page generated from Markdown, along with its FrontMatter and Table of Contents.
/// </summary>
/// <typeparam name="TFrontMatter">The front matter type that must implement IFrontMatter.</typeparam>
public class MarkdownContentPage<TFrontMatter> where TFrontMatter : class, IFrontMatter
{
    /// <summary>
    ///     The metadata of the page defined in the front matter section.
    /// </summary>
    public required TFrontMatter FrontMatter { get; init; }

    /// <summary>
    ///     The URL path where the page will be published.
    ///     Derived from the file path (e.g., Content/Blog/subfolder/post-in-subfolder.md => blog/subfolder/post-in-subfolder).
    ///     Used as a route parameter, e.g., "blog/{Url}".
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// The URL to use for navigation. Includes the BaseUrl of the static content section.
    /// </summary>
    public required string NavigateUrl { get; init; }

    /// <summary>
    ///     The Markdown content of the page, rendered from Markdown, excluding the front matter section.
    /// </summary>
    public required string MarkdownContent { get; init; }

    /// <summary>
    /// The table of contents for the page, pulled from the Markdown using Header tags.
    /// </summary>
    public required OutlineEntry[] Outline { get; init; }

    /// <summary>
    ///     The tags associated with this page.
    ///     Only populated when the TFrontMatter type implements IFrontMatterWithTags.
    /// </summary>
    public ImmutableList<Tag> Tags { get; init; } = [];
}

/// <summary>
/// A Table of Contents entry.
/// </summary>
/// <param name="Title">The title.</param>
/// <param name="Id">The id of the header.</param>
/// <param name="Children">Any children of the entry</param>
public record OutlineEntry(string Title, string Id, OutlineEntry[] Children);