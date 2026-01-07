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
    private readonly ITextMateHighlighter _textMateHighlighter;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeHighlighterService"/> class.
    /// </summary>
    /// <param name="textMateHighlighter">The TextMate highlighter for code syntax highlighting.</param>
    /// <param name="syntaxHighlighter">Optional Roslyn-based syntax highlighter for C# and VB code.</param>
    public CodeHighlighterService(
        ITextMateHighlighter textMateHighlighter,
        ISyntaxHighlightingService? syntaxHighlighter = null)
    {
        _textMateHighlighter = textMateHighlighter ?? throw new ArgumentNullException(nameof(textMateHighlighter));
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
        const string xmlDocIdDiffMarker = ":xmldocid-diff";
        const string xmlDocIdMarker = ":xmldocid";
        const string pathMarker = ":path";

        // Check for :xmldocid-diff first (before :xmldocid) to avoid false matches
        if (trimmed.Contains(xmlDocIdDiffMarker, StringComparison.OrdinalIgnoreCase))
        {
            var baseIndex = trimmed.IndexOf(xmlDocIdDiffMarker, StringComparison.OrdinalIgnoreCase);
            var baseLanguage = trimmed[..baseIndex];
            var modifierPart = trimmed.Contains(",bodyonly", StringComparison.OrdinalIgnoreCase)
                ? "xmldocid-diff,bodyonly"
                : "xmldocid-diff";
            return (baseLanguage, modifierPart);
        }

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
            "xmldocid-diff" => ProcessXmlDocIdDiffModifier(baseLanguage, code, bodyOnly: false),
            "xmldocid-diff,bodyonly" => ProcessXmlDocIdDiffModifier(baseLanguage, code, bodyOnly: true),
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
        var wrappedHtml = AsPreCode(combinedHtml);
        // Apply transformations (line highlighting, focus, diff, etc.)
        return ApplyTransformations(wrappedHtml, baseLanguage);
    }

    private string ProcessXmlDocIdDiffModifier(string baseLanguage, string code, bool bodyOnly)
    {
        if (_syntaxHighlighter == null)
            return AsPreCode(HtmlEncoder.Default.Encode(code));

        // Only C# is supported for XML doc ID highlighting
        if (baseLanguage is not ("csharp" or "c#" or "cs"))
        {
            return AsPreCode("Error: Only C# is supported for xmldocid-diff modifier.");
        }

        // Split code into lines and validate exactly 2 XmlDocIds
        var xmlDocIds = code.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (xmlDocIds.Length != 2)
        {
            return AsPreCode($"Error: xmldocid-diff requires exactly 2 XmlDocIds, but got {xmlDocIds.Length}.");
        }

        // Highlight both symbols
        var result1 = RunSync(async () =>
            await _syntaxHighlighter.HighlightSymbolAsync(xmlDocIds[0], bodyOnly));
        var result2 = RunSync(async () =>
            await _syntaxHighlighter.HighlightSymbolAsync(xmlDocIds[1], bodyOnly));

        // Handle errors
        if (!result1.Success || !result2.Success)
        {
            var errors = new List<string>();
            if (!result1.Success)
                errors.Add($"Error highlighting '{xmlDocIds[0]}': {result1.ErrorMessage ?? "Unknown error"}");
            if (!result2.Success)
                errors.Add($"Error highlighting '{xmlDocIds[1]}': {result2.ErrorMessage ?? "Unknown error"}");

            var errorHtml = string.Join("\n", errors.Select(e =>
                $"""<span class="comment">// {HtmlEncoder.Default.Encode(e)}</span>"""));
            return AsPreCode(errorHtml);
        }

        // Extract inner HTML and compute diff
        var html1 = ExtractInnerCodeHtml(result1.Html);
        var html2 = ExtractInnerCodeHtml(result2.Html);

        var diffResult = ComputeAndRenderDiff(html1, html2, result1.PlainText, result2.PlainText);

        // Wrap in pre/code with has-diff class if differences exist
        var preClass = diffResult.HasDifferences ? " class=\"has-diff\"" : "";
        var wrappedHtml = $"<pre{preClass}><code>{diffResult.Html}</code></pre>";

        // Skip CodeTransformer since we already structured the lines ourselves
        return wrappedHtml;
    }

    private static DiffRenderResult ComputeAndRenderDiff(
        string highlightedHtml1,
        string highlightedHtml2,
        string plainText1,
        string plainText2)
    {
        // Split both highlighted HTML and plain text into lines
        var htmlLines1 = highlightedHtml1.SplitNewLines();
        var htmlLines2 = highlightedHtml2.SplitNewLines();

        // Use DiffPlex to compute diff on plain text
        var differ = new DiffPlex.Differ();
        var diffResult = differ.CreateLineDiffs(plainText1, plainText2, ignoreWhitespace: true);

        var outputLines = new List<string>();
        var processedLinesA = 0; // Tracks how many lines from A we've processed
        var hasDifferences = diffResult.DiffBlocks.Count > 0;

        // Process each diff block
        foreach (var diffBlock in diffResult.DiffBlocks)
        {
            // Add unchanged lines before this diff block
            while (processedLinesA < diffBlock.DeleteStartA)
            {
                if (processedLinesA < htmlLines1.Length)
                {
                    outputLines.Add($"""<span class="line">{htmlLines1[processedLinesA]}</span>""");
                }
                processedLinesA++;
            }

            // Add deleted lines (from first snippet)
            for (var i = 0; i < diffBlock.DeleteCountA; i++)
            {
                var lineIndex = diffBlock.DeleteStartA + i;
                if (lineIndex < htmlLines1.Length)
                {
                    outputLines.Add($"""<span class="line diff-remove">{htmlLines1[lineIndex]}</span>""");
                }
            }

            // Add inserted lines (from second snippet)
            for (var i = 0; i < diffBlock.InsertCountB; i++)
            {
                var lineIndex = diffBlock.InsertStartB + i;
                if (lineIndex < htmlLines2.Length)
                {
                    outputLines.Add($"""<span class="line diff-add">{htmlLines2[lineIndex]}</span>""");
                }
            }

            // Move past the deleted lines in A
            processedLinesA += diffBlock.DeleteCountA;
        }

        // Add any remaining unchanged lines from the end
        while (processedLinesA < htmlLines1.Length)
        {
            outputLines.Add($"""<span class="line">{htmlLines1[processedLinesA]}</span>""");
            processedLinesA++;
        }

        return new DiffRenderResult(string.Join("\n", outputLines), hasDifferences);
    }

    private record DiffRenderResult(string Html, bool HasDifferences);

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
                    : _textMateHighlighter.Highlight(code, "vb"),  // Normalize to "vb" for TextMate

            "csharp" or "c#" or "cs" =>
                _syntaxHighlighter != null
                    ? HighlightWithService(code, Language.CSharp, _syntaxHighlighter)
                    : _textMateHighlighter.Highlight(code, "csharp"),  // Normalize to "csharp" for TextMate

            "gbnf" => GbnfHighlighter.Highlight(code),
            "bash" or "shell" => ShellSyntaxHighlighter.Highlight(code),
            "text" or "" => AsPreCode(HtmlEncoder.Default.Encode(code)),
            _ => _textMateHighlighter.Highlight(code, language)
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
