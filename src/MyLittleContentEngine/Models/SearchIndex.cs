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
    /// Headings extracted from the page as "level:text" format (e.g., "1:Introduction", "2:Getting Started")
    /// </summary>
    public List<string> Headings { get; set; } = [];

    /// <summary>
    /// The description or summary of the page
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The search priority from the content service (higher values = more important)
    /// </summary>
    public int SearchPriority { get; set; } = 1;
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