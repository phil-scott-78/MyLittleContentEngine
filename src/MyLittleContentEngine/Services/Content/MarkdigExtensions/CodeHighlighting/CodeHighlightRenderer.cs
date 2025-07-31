using System.Text;
using System.Text.Encodings.Web;
using static MyLittleContentEngine.Services.AsyncHelpers;
using Markdig.Parsers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.Tabs;
using MyLittleContentEngine.Services.Content.Roslyn;
using HtmlRenderer = Markdig.Renderers.HtmlRenderer;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;

internal sealed class CodeHighlightRenderer(
    IRoslynHighlighterService? roslynHighlighter,
    Func<CodeHighlightRenderOptions>? options)
    : HtmlObjectRenderer<CodeBlock>
{
    private readonly Func<CodeHighlightRenderOptions> _optionsFactory = options ?? (() => CodeHighlightRenderOptions.Default);

    protected override void Write(HtmlRenderer renderer, CodeBlock codeBlock)
    {
        var options1 = _optionsFactory();

        var preCss = options1.PreBaseCss;
        var containerCss = "";

        if (codeBlock.Parent is not TabbedCodeBlock)
        {
            // if we aren't in a tab block, then let's create ourselves a container
            containerCss = options1.StandaloneContainerCss;
            preCss += $" {options1.PreStandaloneCss} ";
        }

        renderer.WriteLine($"<div class=\"{options1.OuterWrapperCss}\">");
        renderer.WriteLine($"<div class=\"{containerCss}\">");
        renderer.WriteLine($"<div class=\"{preCss}\">");

        if (codeBlock is FencedCodeBlock fencedCodeBlock &&
            codeBlock.Parser is FencedCodeBlockParser fencedCodeBlockParser &&
            fencedCodeBlock.Info != null &&
            fencedCodeBlockParser.InfoPrefix != null)
        {
            var languageId = fencedCodeBlock.Info.Replace(fencedCodeBlockParser.InfoPrefix, string.Empty);
            var code = ExtractCode(codeBlock);

            if (roslynHighlighter != null)
            {
                WriteCode(renderer, codeBlock, languageId, code, roslynHighlighter);
            }
            else
            {
                WriteCodeWithoutRoslyn(renderer, languageId, code);
            }
        }

        // Common closing tags for all paths
        renderer.WriteLine("</div>");
        renderer.WriteLine("</div>");
        renderer.WriteLine("</div>");
    }

    private static void WriteCode(HtmlRenderer renderer, CodeBlock codeBlock, string languageId, string code, IRoslynHighlighterService roslynHighlighter)
    {
        string highlightedCode;

        var languageTrimmed = languageId.Trim();
        switch (languageTrimmed)
        {
            case "vb" or "vbnet":
                highlightedCode = roslynHighlighter.Highlight(code, Language.VisualBasic);
                break;
            case "csharp" or "c#" or "cs":
                highlightedCode = roslynHighlighter.Highlight(code);
                break;
            case "csharp:xmldocid,bodyonly":
                highlightedCode = RunSync(async () => await roslynHighlighter.HighlightExampleAsync(code, true));
                break;
            case "csharp:xmldocid":
                highlightedCode = RunSync(async () => await roslynHighlighter.HighlightExampleAsync(code, false));
                break;
            case "gbnf":
                highlightedCode = GbnfHighlighter.Highlight(code);
                break;
            case "bash" or "shell":
                highlightedCode = ShellSyntaxHighlighter.Highlight(code);
                break;
            case "text" or "":
                highlightedCode = "<pre><code>" + HtmlEncoder.Default.Encode(code) + "</code></pre>";
                break;
            default:
            {
                if (languageId.Contains(":xmldocid"))
                {
                    var newLanguage = languageId[..languageId.IndexOf(":xmldocid", StringComparison.Ordinal)];
                    if (codeBlock is not FencedCodeBlock fencedCodeBlock ||
                        !fencedCodeBlock.GetArgumentPairs().TryGetValue("data", out var arg))
                    {
                        arg = string.Empty;
                    }

                    var newCode = RunSync(async () => await roslynHighlighter.GetCodeOutputAsync(code, arg));
                    WriteCode(renderer, codeBlock, newLanguage, newCode,  roslynHighlighter);
                    return;
                }
                else if (languageId.Contains(":path"))
                {
                    var newLanguage = languageId[..languageId.IndexOf(":path", StringComparison.Ordinal)];
                    try
                    {
                        var fileContent = RunSync(async () => await roslynHighlighter.GetFileContentAsync(code.Trim()));
                        WriteCode(renderer, codeBlock, newLanguage, fileContent, roslynHighlighter);
                        return;
                    }
                    catch (Exception ex)
                    {
                        highlightedCode = $"<pre><code>Error loading file '{code.Trim()}': {ex.Message}</code></pre>";
                    }
                }
                else
                {
                    highlightedCode = TextMateHighlighter.Highlight(code, languageId);
                }

                break;
            }
        }

        // Apply code transformations for notation comments
        if (languageTrimmed != "markdown" && languageTrimmed != "md")
        {
            highlightedCode = CodeTransformer.Transform(highlightedCode);
        }

        renderer.Write(highlightedCode);
    }

    private static void WriteCodeWithoutRoslyn(HtmlRenderer renderer, string languageId, string code)
    {
        string highlightedCode;

        switch (languageId.Trim())
        {
            case "gbnf":
                highlightedCode = GbnfHighlighter.Highlight(code);
                break;
            case "bash" or "shell":
                highlightedCode = ShellSyntaxHighlighter.Highlight(code);
                break;
            case "text" or "":
                highlightedCode = "<pre><code>" + code + "</code></pre>";
                break;
            default:
            {
                highlightedCode = TextMateHighlighter.Highlight(code, languageId);
                break;
            }
        }

        // Apply code transformations for notation comments
        var transformedCode = CodeTransformer.Transform(highlightedCode);
        renderer.Write(transformedCode);
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