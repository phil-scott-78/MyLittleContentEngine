using MyLittleContentEngine.Services;

namespace MyLittleContentEngine.Models;

/// <summary>
/// Represents a broken link found during static site generation validation.
/// </summary>
/// <param name="SourcePage">The page containing the broken link</param>
/// <param name="BrokenUrl">The URL that could not be resolved</param>
/// <param name="LinkType">The type of link (href or src)</param>
/// <param name="ElementType">The HTML element containing the link (a, img, script, etc.)</param>
public record BrokenLink(
    UrlPath SourcePage,
    string BrokenUrl,
    LinkType LinkType,
    string ElementType);

/// <summary>
/// Categorizes the type of link attribute.
/// </summary>
public enum LinkType
{
    /// <summary>
    /// Link is from an href attribute (e.g., &lt;a href&gt;, &lt;link href&gt;)
    /// </summary>
    Href,

    /// <summary>
    /// Link is from a src attribute (e.g., &lt;img src&gt;, &lt;script src&gt;)
    /// </summary>
    Src
}
