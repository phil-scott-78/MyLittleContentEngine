using System.Text;
using System.Text.Encodings.Web;
using static MyLittleContentEngine.Services.AsyncHelpers;
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
1. Extract code block content and language identifier
2. Parse language ID to separate base language from modifiers (e.g., "csharp:xmldocid,bodyonly")
3. Process special modifiers if present:
   - xmldocid: Highlights C# symbols from documentation IDs
   - path: Loads and highlights code from file paths
4. Apply syntax highlighting based on language:
   - C#/VB: Uses Roslyn-based highlighting if available
   - GBNF/Shell: Uses custom highlighters
   - Others: Uses TextMate grammar highlighting
5. Apply post-processing transformations (e.g., word highlighting)
6. Wrap result in appropriate HTML structure
*/
internal sealed class CodeHighlightRenderer(
    ISyntaxHighlightingService? syntaxHighlighter,
    Func<CodeHighlightRenderOptions>? options)
    : HtmlObjectRenderer<CodeBlock>
{
    private readonly Func<CodeHighlightRenderOptions> _optionsFactory =
        options ?? (() => CodeHighlightRenderOptions.Default);

    protected override void Write(HtmlRenderer renderer, CodeBlock codeBlock)
    {
        var options = _optionsFactory();

        // Start wrapper structure
        WriteOpeningTags(renderer, codeBlock, options);

        // Process the code block content through pipeline
        if (TryExtractFencedCodeBlock(codeBlock, out var languageId, out var code))
        {
            var html = ProcessPipeline(codeBlock, languageId, code);
            renderer.Write(html);
        }

        // Close wrapper structure
        WriteClosingTags(renderer);
    }

    private string ProcessPipeline(CodeBlock codeBlock, string languageId, string code)
    {
        // Parse language and modifiers
        var (baseLanguage, modifier) = ParseLanguageId(languageId);
        
        // Process special modifiers if present
        if (modifier != null)
        {
            var modifierResult = ProcessModifier(codeBlock, baseLanguage, modifier, code);
            if (modifierResult != null)
                return modifierResult;
        }
        
        // Highlight the code
        var highlightedCode = HighlightCodeUnified(baseLanguage, code);
        
        // Apply transformations
        return ApplyTransformations(highlightedCode, baseLanguage);
    }

    private void WriteOpeningTags(HtmlRenderer renderer, CodeBlock codeBlock, CodeHighlightRenderOptions options)
    {
        var (containerCss, preCss) = GetCssClasses(codeBlock, options);

        renderer.WriteLine($"<div class=\"{options.OuterWrapperCss}\">");
        renderer.WriteLine($"<div class=\"{containerCss}\">");
        renderer.WriteLine($"<div class=\"{preCss}\">");
    }

    private static void WriteClosingTags(HtmlRenderer renderer)
    {
        renderer.WriteLine("</div>");
        renderer.WriteLine("</div>");
        renderer.WriteLine("</div>");
    }

    private static (string containerCss, string preCss) GetCssClasses(CodeBlock codeBlock,
        CodeHighlightRenderOptions options)
    {
        var preCss = options.PreBaseCss;
        var containerCss = "";

        if (codeBlock.Parent is not TabbedCodeBlock)
        {
            containerCss = options.StandaloneContainerCss;
            preCss = $"{preCss} {options.PreStandaloneCss}".Trim();
        }

        return (containerCss, preCss);
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


    private string? ProcessModifier(CodeBlock codeBlock, string baseLanguage, string modifier, string code)
    {
        return modifier switch
        {
            LanguageModifiers.XmlDocId => ProcessXmlDocIdModifier(baseLanguage, code, bodyOnly: false),
            $"{LanguageModifiers.XmlDocId},{LanguageModifiers.BodyOnly}" => ProcessXmlDocIdModifier(baseLanguage, code, bodyOnly: true),
            LanguageModifiers.Path => ProcessPathModifier(codeBlock, baseLanguage, code),
            _ => null
        };
    }

    private static (string baseLanguage, string? modifier) ParseLanguageId(string languageId)
    {
        var trimmed = languageId.Trim();
        const string xmlDocIdMarker = $":{LanguageModifiers.XmlDocId}";
        const string pathMarker = $":{LanguageModifiers.Path}";

        if (trimmed.Contains(xmlDocIdMarker))
        {
            var baseIndex = trimmed.IndexOf(xmlDocIdMarker, StringComparison.Ordinal);
            var baseLanguage = trimmed[..baseIndex];
            var modifierPart = trimmed.Contains($",{LanguageModifiers.BodyOnly}") 
                ? $"{LanguageModifiers.XmlDocId},{LanguageModifiers.BodyOnly}" 
                : LanguageModifiers.XmlDocId;
            return (baseLanguage, modifierPart);
        }

        if (trimmed.Contains(pathMarker))
        {
            var baseIndex = trimmed.IndexOf(pathMarker, StringComparison.Ordinal);
            return (trimmed[..baseIndex], LanguageModifiers.Path);
        }

        return (trimmed, null);
    }


    private string ProcessXmlDocIdModifier(string baseLanguage, string code, bool bodyOnly)
    {
        if (syntaxHighlighter == null)
            return AsPreCode(HtmlEncoder.Default.Encode(code));

        // For C#, use symbol highlighting, for other languages use execution
        if (baseLanguage is not (LanguageIds.CSharp or LanguageIds.CSharpShort or LanguageIds.CSharpAbbrev))
        {
            return AsPreCode($"Error: Only C# is supported for {LanguageModifiers.XmlDocId} modifier.");
        }
        
        var symbolResult = RunSync(async () => await syntaxHighlighter.HighlightSymbolAsync(code.Trim(), bodyOnly));
        return symbolResult.Success
            ? AsPreCode(symbolResult.Html)
            : AsPreCode(HtmlEncoder.Default.Encode(symbolResult.ErrorMessage ?? "Symbol not found"));

    }

    private string ProcessPathModifier(CodeBlock codeBlock, string baseLanguage, string code)
    {
        if (syntaxHighlighter == null)
            return AsPreCode(HtmlEncoder.Default.Encode(code));

        var fileResult = RunSync(async () => await syntaxHighlighter.HighlightFileAsync(code.Trim()));

        if (fileResult.Success)
        {
            // Re-highlight the file content
            return ProcessPipeline(codeBlock, baseLanguage, fileResult.PlainText);
        }
        else
        {
            return AsPreCode($"Error loading file '{code.Trim()}': {HtmlEncoder.Default.Encode(fileResult.ErrorMessage ?? "File not found")}");
        }
    }

    private string HighlightCodeUnified(string language, string code)
    {
        // Handle special language cases
        return language switch
        {
            LanguageIds.VisualBasic or LanguageIds.VisualBasicNet => 
                syntaxHighlighter != null 
                    ? HighlightWithService(code, Language.VisualBasic, syntaxHighlighter)
                    : TextMateHighlighter.Highlight(code, language),
                    
            LanguageIds.CSharp or LanguageIds.CSharpShort or LanguageIds.CSharpAbbrev => 
                syntaxHighlighter != null
                    ? HighlightWithService(code, Language.CSharp, syntaxHighlighter)
                    : TextMateHighlighter.Highlight(code, language),
                    
            LanguageIds.Gbnf => GbnfHighlighter.Highlight(code),
            LanguageIds.Bash or LanguageIds.Shell => ShellSyntaxHighlighter.Highlight(code),
            LanguageIds.Text or "" => AsPreCode(HtmlEncoder.Default.Encode(code)),
            _ => TextMateHighlighter.Highlight(code, language)
        };
    }

    private static string HighlightWithService(string code, Language language,
        ISyntaxHighlightingService syntaxHighlighter)
    {
        var result = RunSync(async () => await syntaxHighlighter.HighlightAsync(code, language));
        return result.Success
            ? AsPreCode(result.Html)
            : AsPreCode(HtmlEncoder.Default.Encode(code));
    }


    private static string ApplyTransformations(string highlightedCode, string language)
    {
        // Skip transformation for markdown
        return language is LanguageIds.Markdown or LanguageIds.MarkdownShort
            ? highlightedCode 
            : CodeTransformer.Transform(highlightedCode);
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

    private static string AsPreCode(string s) => $"<pre><code>{s}</code></pre>";
}