using System.Text;
using static MyLittleContentEngine.Services.AsyncHelpers;
using Markdig.Parsers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.Tabs;
using MyLittleContentEngine.Services.Content.Roslyn;
using HtmlRenderer = Markdig.Renderers.HtmlRenderer;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;

internal sealed class CodeHighlightRenderer(
    RoslynHighlighterService? roslynHighlighter,
    Func<CodeHighlightRenderOptions>? options)
    : HtmlObjectRenderer<CodeBlock>
{
    private readonly Func<CodeHighlightRenderOptions> _optionsFactory = options ?? (() => CodeHighlightRenderOptions.MonorailMono);

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

    private static void WriteCode(HtmlRenderer renderer, CodeBlock codeBlock, string languageId, string code, RoslynHighlighterService roslynHighlighter)
    {
        switch (languageId.Trim())
        {
            case "vb" or "vbnet":
                renderer.Write(roslynHighlighter.Highlight(code, Language.VisualBasic));
                break;
            case "csharp" or "c#" or "cs":
                renderer.Write(roslynHighlighter.Highlight(code));
                break;
            case "csharp:xmldocid,bodyonly":
                var bodyOnlySample = RunSync(async () => await roslynHighlighter.HighlightExampleAsync(code, true));
                renderer.Write(bodyOnlySample);
                break;
            case "csharp:xmldocid":
                var fullSample = RunSync(async () => await roslynHighlighter.HighlightExampleAsync(code, false));
                renderer.Write(fullSample);
                break;
            case "gbnf":
                renderer.Write(GbnfHighlighter.Highlight(code));
                break;
            case "bash" or "shell":
                renderer.Write(ShellSyntaxHighlighter.Highlight(code));
                break;
            case "text" or "":
                renderer.Write("<pre><code>");
                renderer.Write(code);
                renderer.Write("</code></pre>");
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
                }
                else if (languageId.Contains(":path"))
                {
                    var newLanguage = languageId[..languageId.IndexOf(":path", StringComparison.Ordinal)];
                    try
                    {
                        var fileContent = RunSync(async () => await roslynHighlighter.GetFileContentAsync(code.Trim()));
                        WriteCode(renderer, codeBlock, newLanguage, fileContent, roslynHighlighter);
                    }
                    catch (Exception ex)
                    {
                        // If file reading fails, display an error message
                        renderer.Write($"<pre><code>Error loading file '{code.Trim()}': {ex.Message}</code></pre>");
                    }
                }
                else
                {
                    renderer.Write(TextMateHighlighter.Highlight(code, languageId));
                }

                break;
            }
        }
    }

    private static void WriteCodeWithoutRoslyn(HtmlRenderer renderer, string languageId, string code)
    {
        switch (languageId.Trim())
        {
            case "gbnf":
                renderer.Write(GbnfHighlighter.Highlight(code));
                break;
            case "bash" or "shell":
                renderer.Write(ShellSyntaxHighlighter.Highlight(code));
                break;
            case "text" or "":
                renderer.Write("<pre><code>");
                renderer.Write(code);
                renderer.Write("</code></pre>");
                break;
            default:
            {
                renderer.Write(TextMateHighlighter.Highlight(code, languageId));
                break;
            }
        }
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

        return code.ToString();
    }
}