using System.Text;
using Markdig.Parsers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.Tabs;
using MyLittleContentEngine.Services.Content.CodeAnalysis.SyntaxHighlighting;
using HtmlRenderer = Markdig.Renderers.HtmlRenderer;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;

public static class Test
{
    public static string AsTitleCase()
    {
        return "my name is phil scott and this is my title".ToApaTitleCase();
    }
}

internal static class LanguageModifiers
{
    public const string XmlDocId = "xmldocid";
    public const string Path = "path";
    public const string BodyOnly = "bodyonly";
}

internal static class LanguageIds
{
    public const string CSharp = "csharp";
    public const string CSharpShort = "c#";
    public const string CSharpAbbrev = "cs";
    public const string VisualBasic = "vb";
    public const string VisualBasicNet = "vbnet";
    public const string Gbnf = "gbnf";
    public const string Bash = "bash";
    public const string Shell = "shell";
    public const string Text = "text";
    public const string Markdown = "markdown";
    public const string MarkdownShort = "md";
}

/*
Renders code blocks with syntax highlighting using a pipeline approach.

Processing Flow:
1. Extract code block content and language identifier from Markdig AST
2. Determine if code block is part of a tabbed code group
3. Delegate to CodeHighlighterService for highlighting and HTML generation
4. Write result to HTML renderer

The actual highlighting logic is handled by CodeHighlighterService which supports:
- C#/VB: Uses Roslyn-based highlighting if available
- GBNF/Shell: Uses custom highlighters
- Others: Uses TextMate grammar highlighting
- Special modifiers: xmldocid (C# symbol highlighting), path (file loading)
- Post-processing transformations (line highlighting, focus, diff, etc.)
*/
internal sealed class CodeHighlightRenderer(
    ISyntaxHighlightingService? syntaxHighlighter,
    Func<CodeHighlightRenderOptions>? options)
    : HtmlObjectRenderer<CodeBlock>
{
    private readonly Func<CodeHighlightRenderOptions> _optionsFactory =
        options ?? (() => CodeHighlightRenderOptions.Default);

    private readonly ICodeHighlighter _highlighter = new CodeHighlighterService(syntaxHighlighter);

    protected override void Write(HtmlRenderer renderer, CodeBlock codeBlock)
    {
        var options = _optionsFactory();

        // Extract code and language from Markdig AST
        if (!TryExtractFencedCodeBlock(codeBlock, out var languageId, out var code))
        {
            return;
        }

        // Determine if this code block is part of a tabbed code group
        var isInTabGroup = codeBlock.Parent is TabbedCodeBlock;

        // Delegate to highlighter service for processing and HTML generation
        var result = _highlighter.Highlight(code, languageId, options, isInTabGroup);

        // Write the complete HTML output to the renderer
        renderer.Write(result.Html);
    }

    private static bool TryExtractFencedCodeBlock(CodeBlock codeBlock, out string languageId, out string code)
    {
        languageId = "";
        code = "";

        if (codeBlock is not FencedCodeBlock fencedCodeBlock ||
            codeBlock.Parser is not FencedCodeBlockParser fencedCodeBlockParser ||
            fencedCodeBlock.Info == null ||
            fencedCodeBlockParser.InfoPrefix == null)
        {
            return false;
        }

        languageId = fencedCodeBlock.Info.Replace(fencedCodeBlockParser.InfoPrefix, string.Empty);
        code = ExtractCode(codeBlock);
        return true;
    }

    private static string ExtractCode(LeafBlock leafBlock)
    {
        var code = new StringBuilder();

        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        var lines = leafBlock.Lines.Lines ?? [];
        var totalLines = lines.Length;

        for (var index = 0; index < totalLines; index++)
        {
            var line = lines[index];
            var slice = line.Slice;

            if (slice.Text == null)
            {
                continue;
            }

            var lineText = slice.Text.Substring(slice.Start, slice.Length);

            if (index > 0)
            {
                code.AppendLine();
            }

            code.Append(lineText);
        }

        return code.ToString().Trim();
    }
}