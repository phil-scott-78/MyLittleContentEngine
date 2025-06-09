namespace MyLittleContentEngine.Models;

/// <summary>
/// Represents a cross-reference in the content system, used in
/// Table of Contents (ToC) or and xref links.
/// </summary>
public class CrossReference
{
    /// <summary>
    /// Unique identifier for the cross-reference.
    /// </summary>
    public required string? Uid { get; init; }

    /// <summary>
    /// The title of the cross-reference, typically used for display purposes.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The URL of the cross-reference, which points to the related content.
    /// </summary>
    public required string Url { get; init; }
}