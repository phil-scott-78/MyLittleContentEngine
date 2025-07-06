namespace MyLittleContentEngine.Models;

/// <summary>
/// Represents a search index document for client-side searching with fuse.js
/// </summary>
public class SearchIndexDocument
{
    /// <summary>
    /// The URL of the page
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The title of the page
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The main content of the page
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Headings extracted from the page with their priorities
    /// </summary>
    public List<SearchIndexHeading> Headings { get; set; } = [];

    /// <summary>
    /// The description or summary of the page
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents a heading in the search index with priority weighting
/// </summary>
public class SearchIndexHeading
{
    /// <summary>
    /// The text content of the heading
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The heading level (1-6)
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// The priority weight for search ranking (higher values = more important)
    /// </summary>
    public int Priority { get; set; }
}

/// <summary>
/// The complete search index containing all documents
/// </summary>
public class SearchIndex
{
    /// <summary>
    /// All searchable documents
    /// </summary>
    public List<SearchIndexDocument> Documents { get; set; } = [];

    /// <summary>
    /// When the index was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}