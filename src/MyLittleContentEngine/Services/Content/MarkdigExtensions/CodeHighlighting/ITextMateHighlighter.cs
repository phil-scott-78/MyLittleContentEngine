namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;

/// <summary>
/// Provides syntax highlighting for code blocks using TextMate grammars.
/// </summary>
public interface ITextMateHighlighter
{
    /// <summary>
    /// Highlights the specified code using TextMate grammar for the given language.
    /// </summary>
    /// <param name="text">The code to highlight.</param>
    /// <param name="language">The language identifier (e.g., "csharp", "javascript").</param>
    /// <returns>HTML-formatted code with syntax highlighting spans.</returns>
    string Highlight(string text, string language);
}
