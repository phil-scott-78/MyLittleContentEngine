namespace MyLittleContentEngine.Models;
/// <summary>
/// Represents a content tag in MyLittleContentEngine with both display and URL-friendly versions of the tag name.
/// </summary>
public class Tag
{
    /// <summary>
    /// The original tag name as specified in the FrontMatter.
    /// This is the display version that may contain any characters.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The URL to use for navigation. Includes the leading slash and base page URL.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// The URL-safe version of the tag name.
    /// Used in URLs and file names where special characters might cause issues.
    /// </summary>
    public required string EncodedName { get; init; }
}