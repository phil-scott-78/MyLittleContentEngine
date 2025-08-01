namespace MyLittleContentEngine.Services.Content.CodeAnalysis.SyntaxHighlighting;

/// <summary>
/// Main service interface for syntax highlighting operations
/// </summary>
public interface ISyntaxHighlightingService
{
    /// <summary>
    /// Highlights code with the specified language
    /// </summary>
    /// <param name="code">The code to highlight</param>
    /// <param name="language">The programming language</param>
    /// <returns>The highlighted code in HTML format</returns>
    Task<HighlightedCode> HighlightAsync(string code, Language language = Language.CSharp);

    /// <summary>
    /// Highlights code for a symbol by its XML documentation ID
    /// </summary>
    /// <param name="xmlDocId">The XML documentation ID of the symbol</param>
    /// <param name="bodyOnly">Whether to show only the method body</param>
    /// <returns>The highlighted code</returns>
    Task<HighlightedCode> HighlightSymbolAsync(string xmlDocId, bool bodyOnly = false);

    /// <summary>
    /// Highlights code from a file
    /// </summary>
    /// <param name="relativePath">Path relative to the solution root</param>
    /// <returns>The highlighted file content</returns>
    Task<HighlightedCode> HighlightFileAsync(string relativePath);
}

/// <summary>
/// Result of syntax highlighting operation
/// </summary>
public record HighlightedCode
{
    /// <summary>
    /// The highlighted HTML content
    /// </summary>
    public required string Html { get; init; }

    /// <summary>
    /// The original code without highlighting
    /// </summary>
    public required string PlainText { get; init; }

    /// <summary>
    /// The language used for highlighting
    /// </summary>
    public required Language Language { get; init; }

    /// <summary>
    /// Whether the highlighting was successful
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Error message if highlighting failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Additional metadata about the highlighted code
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Creates a successful highlight result
    /// </summary>
    public static HighlightedCode CreateSuccess(string html, string plainText, Language language) => new()
    {
        Html = html,
        PlainText = plainText,
        Language = language,
        Success = true
    };

    /// <summary>
    /// Creates a failed highlight result
    /// </summary>
    public static HighlightedCode CreateFailure(string plainText, Language language, string errorMessage) => new()
    {
        Html = System.Web.HttpUtility.HtmlEncode(plainText),
        PlainText = plainText,
        Language = language,
        Success = false,
        ErrorMessage = errorMessage
    };
}