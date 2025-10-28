using System.Text.Encodings.Web;
using MyLittleContentEngine.Services.Content.CodeAnalysis.SyntaxHighlighting;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;
using static MyLittleContentEngine.Services.AsyncHelpers;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// Service for highlighting code in various programming languages and generating HTML output.
/// Supports multiple highlighting engines (TextMate, Roslyn, custom highlighters) and code transformations.
/// </summary>
public sealed class CodeHighlighterService : ICodeHighlighter
{
    private readonly ISyntaxHighlightingService? _syntaxHighlighter;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeHighlighterService"/> class.
    /// </summary>
    /// <param name="syntaxHighlighter">Optional Roslyn-based syntax highlighter for C# and VB code.</param>
    public CodeHighlighterService(ISyntaxHighlightingService? syntaxHighlighter = null)
    {
        _syntaxHighlighter = syntaxHighlighter;
    }

    /// <inheritdoc />
    public CodeHighlightResult Highlight(
        string code,
        string language,
        CodeHighlightRenderOptions? options = null,
        bool isInTabGroup = false)
    {
        var renderOptions = options ?? CodeHighlightRenderOptions.Default;

        try
        {
            var highlightedHtml = ProcessHighlightingPipeline(language, code);
            var wrappedHtml = CodeBlockHtmlBuilder.BuildHtml(highlightedHtml, renderOptions, isInTabGroup);

            return new CodeHighlightResult(
                Html: wrappedHtml,
                PlainText: code,
                Language: language,
                Success: true);
        }
        catch (Exception)
        {
            // On error, return encoded plain text
            var fallbackHtml = AsPreCode(HtmlEncoder.Default.Encode(code));
            var wrappedFallback = CodeBlockHtmlBuilder.BuildHtml(fallbackHtml, renderOptions, isInTabGroup);

            return new CodeHighlightResult(
                Html: wrappedFallback,
                PlainText: code,
                Language: language,
                Success: false);
        }
    }

    private string ProcessHighlightingPipeline(string languageId, string code)
    {
        // Parse language and modifiers (e.g., "csharp:xmldocid,bodyonly")
        var (baseLanguage, modifier) = ParseLanguageId(languageId);

        // Process special modifiers if present
        if (modifier != null)
        {
            var modifierResult = ProcessModifier(baseLanguage, modifier, code);
            if (modifierResult != null)
                return modifierResult;
        }

        // Highlight the code
        var highlightedCode = HighlightCode(baseLanguage, code);

        // Apply transformations (line highlighting, focus, diff, etc.)
        return ApplyTransformations(highlightedCode, baseLanguage);
    }

    private static (string baseLanguage, string? modifier) ParseLanguageId(string languageId)
    {
        var trimmed = languageId.Trim();
        const string xmlDocIdMarker = ":xmldocid";
        const string pathMarker = ":path";

        if (trimmed.Contains(xmlDocIdMarker, StringComparison.OrdinalIgnoreCase))
        {
            var baseIndex = trimmed.IndexOf(xmlDocIdMarker, StringComparison.OrdinalIgnoreCase);
            var baseLanguage = trimmed[..baseIndex];
            var modifierPart = trimmed.Contains(",bodyonly", StringComparison.OrdinalIgnoreCase)
                ? "xmldocid,bodyonly"
                : "xmldocid";
            return (baseLanguage, modifierPart);
        }

        if (trimmed.Contains(pathMarker, StringComparison.OrdinalIgnoreCase))
        {
            var baseIndex = trimmed.IndexOf(pathMarker, StringComparison.OrdinalIgnoreCase);
            return (trimmed[..baseIndex], "path");
        }

        return (trimmed, null);
    }

    private string? ProcessModifier(string baseLanguage, string modifier, string code)
    {
        return modifier.ToLowerInvariant() switch
        {
            "xmldocid" => ProcessXmlDocIdModifier(baseLanguage, code, bodyOnly: false),
            "xmldocid,bodyonly" => ProcessXmlDocIdModifier(baseLanguage, code, bodyOnly: true),
            "path" => ProcessPathModifier(baseLanguage, code),
            _ => null
        };
    }

    private string ProcessXmlDocIdModifier(string baseLanguage, string code, bool bodyOnly)
    {
        if (_syntaxHighlighter == null)
            return AsPreCode(HtmlEncoder.Default.Encode(code));

        // Only C# is supported for XML doc ID highlighting
        if (baseLanguage is not ("csharp" or "c#" or "cs"))
        {
            return AsPreCode("Error: Only C# is supported for xmldocid modifier.");
        }

        // Split code into lines and process each XML doc ID
        var xmlDocIds = code.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var htmlFragments = new List<string>();

        foreach (var xmlDocId in xmlDocIds)
        {
            var symbolResult = RunSync(async () =>
                await _syntaxHighlighter.HighlightSymbolAsync(xmlDocId, bodyOnly));

            if (symbolResult.Success)
            {
                // Extract inner HTML from the <pre><code>...</code></pre> wrapper
                var innerHtml = ExtractInnerCodeHtml(symbolResult.Html);
                htmlFragments.Add(innerHtml);
            }
            else
            {
                // Add error as a comment in the code block
                var errorMessage = symbolResult.ErrorMessage ?? "Symbol not found";
                var errorComment = $"""<span class="comment">// Error: {HtmlEncoder.Default.Encode(errorMessage)} for '{HtmlEncoder.Default.Encode(xmlDocId)}'</span>""";
                htmlFragments.Add(errorComment);
            }
        }

        // Combine all fragments with blank lines
        var combinedHtml = string.Join("\n\n", htmlFragments);
        return AsPreCode(combinedHtml);
    }

    private string ProcessPathModifier(string baseLanguage, string code)
    {
        if (_syntaxHighlighter == null)
            return AsPreCode(HtmlEncoder.Default.Encode(code));

        var fileResult = RunSync(async () =>
            await _syntaxHighlighter.HighlightFileAsync(code.Trim()));

        if (fileResult.Success)
        {
            // Re-highlight the file content through the pipeline
            return ProcessHighlightingPipeline(baseLanguage, fileResult.PlainText);
        }
        else
        {
            var errorMessage = fileResult.ErrorMessage ?? "File not found";
            return AsPreCode($"Error loading file '{code.Trim()}': {HtmlEncoder.Default.Encode(errorMessage)}");
        }
    }

    private string HighlightCode(string language, string code)
    {
        return language.ToLowerInvariant() switch
        {
            "vb" or "vbnet" =>
                _syntaxHighlighter != null
                    ? HighlightWithService(code, Language.VisualBasic, _syntaxHighlighter)
                    : TextMateHighlighter.Highlight(code, "vb"),  // Normalize to "vb" for TextMate

            "csharp" or "c#" or "cs" =>
                _syntaxHighlighter != null
                    ? HighlightWithService(code, Language.CSharp, _syntaxHighlighter)
                    : TextMateHighlighter.Highlight(code, "csharp"),  // Normalize to "csharp" for TextMate

            "gbnf" => GbnfHighlighter.Highlight(code),
            "bash" or "shell" => ShellSyntaxHighlighter.Highlight(code),
            "text" or "" => AsPreCode(HtmlEncoder.Default.Encode(code)),
            _ => TextMateHighlighter.Highlight(code, language)
        };
    }

    private static string HighlightWithService(
        string code,
        Language language,
        ISyntaxHighlightingService syntaxHighlighter)
    {
        var result = RunSync(async () => await syntaxHighlighter.HighlightAsync(code, language));
        return result.Success
            ? AsPreCode(result.Html)
            : AsPreCode(HtmlEncoder.Default.Encode(code));
    }

    private static string ApplyTransformations(string highlightedCode, string language)
    {
        // Skip transformation for markdown (to avoid processing markdown syntax)
        return language.ToLowerInvariant() is "markdown" or "md"
            ? highlightedCode
            : CodeTransformer.Transform(highlightedCode);
    }

    private static string AsPreCode(string content) => $"<pre><code>{content}</code></pre>";

    /// <summary>
    /// Extracts the inner HTML content from between &lt;pre&gt;&lt;code&gt; and &lt;/code&gt;&lt;/pre&gt; tags.
    /// </summary>
    /// <param name="html">The HTML string containing pre/code tags.</param>
    /// <returns>The inner content, or the original string if tags are not found.</returns>
    private static string ExtractInnerCodeHtml(string html)
    {
        const string openTag = "<pre><code>";
        const string closeTag = "</code></pre>";

        var startIndex = html.IndexOf(openTag, StringComparison.Ordinal);
        if (startIndex == -1)
            return html;

        startIndex += openTag.Length;
        var endIndex = html.IndexOf(closeTag, startIndex, StringComparison.Ordinal);
        if (endIndex == -1)
            return html;

        return html[startIndex..endIndex];
    }
}
