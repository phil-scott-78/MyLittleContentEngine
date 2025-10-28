using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// Service for highlighting code and generating HTML output.
/// </summary>
public interface ICodeHighlighter
{
    /// <summary>
    /// Highlights the specified code and returns the complete HTML output wrapped in appropriate divs.
    /// </summary>
    /// <param name="code">The source code to highlight.</param>
    /// <param name="language">The programming language identifier (e.g., "csharp", "python", "javascript").</param>
    /// <param name="options">Optional CSS customization options. Uses default options if not provided.</param>
    /// <param name="isInTabGroup">Whether this code block is part of a tabbed code group. Affects wrapper CSS classes.</param>
    /// <returns>A <see cref="CodeHighlightResult"/> containing the highlighted HTML and metadata.</returns>
    CodeHighlightResult Highlight(
        string code,
        string language,
        CodeHighlightRenderOptions? options = null,
        bool isInTabGroup = false);
}

/// <summary>
/// Result of code highlighting operation.
/// </summary>
/// <param name="Html">The complete HTML output including wrapper divs and highlighted code.</param>
/// <param name="PlainText">The original plain text code.</param>
/// <param name="Language">The language identifier used for highlighting.</param>
/// <param name="Success">Whether the highlighting operation succeeded.</param>
public record CodeHighlightResult(
    string Html,
    string PlainText,
    string Language,
    bool Success = true);
