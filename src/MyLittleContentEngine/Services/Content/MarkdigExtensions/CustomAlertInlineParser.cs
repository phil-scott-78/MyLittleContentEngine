using Markdig.Extensions.Alerts;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions;

// copied and pasted from https://raw.githubusercontent.com/xoofx/markdig/refs/heads/main/src/Markdig/Extensions/Alerts/AlertInlineParser.cs

/// <summary>
/// Parses GitHub-style alert blocks (e.g. [!NOTE]) within Markdown quote blocks.
/// </summary>
public class AlertInlineParser : InlineParser
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlertInlineParser"/> class.
    /// </summary>
    public AlertInlineParser()
    {
        OpeningCharacters = ['['];
    }

    /// <summary>
    /// Attempts to match the parser at the current position.
    /// </summary>
    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        if (slice.PeekChar() != '!')
        {
            return false;
        }

        // We expect the alert to be the first child of a quote block. Example:
        // > [!NOTE]
        // > This is a note
        if (processor.Block is not ParagraphBlock paragraphBlock ||
            paragraphBlock.Parent is not QuoteBlock quoteBlock ||
            paragraphBlock.Inline?.FirstChild != null ||
            quoteBlock is AlertBlock)
        {
            return false;
        }

        StringSlice saved = slice;

        slice.SkipChar(); // Skip [
        char c = slice.NextChar(); // Skip !

        int start = slice.Start;
        int end = start;
        while (c.IsAlpha())
        {
            end = slice.Start;
            c = slice.NextChar();
        }

        // We need at least one character
        if (c != ']' || start == end)
        {
            slice = saved;
            return false;
        }

        var alertType = new StringSlice(slice.Text, start, end);
        c = slice.NextChar(); // Skip ]

        start = slice.Start;
        while (true)
        {
            if (c == '\0' || c == '\n' || c == '\r')
            {
                end = slice.Start;
                if (c == '\r')
                {
                    c = slice.NextChar(); // Skip \r
                    if (c == '\0' || c == '\n')
                    {
                        end = slice.Start;
                        if (c == '\n')
                        {
                            slice.SkipChar(); // Skip \n
                        }
                    }
                }
                else if (c == '\n')
                {
                    slice.SkipChar(); // Skip \n
                }
                break;
            }
            else if (!c.IsSpaceOrTab())
            {
                slice = saved;
                return false;
            }

            c = slice.NextChar();
        }

        var alertBlock = new AlertBlock(alertType)
        {
            Span = quoteBlock.Span,
            TriviaSpaceAfterKind = new StringSlice(slice.Text, start, end),
            Line = quoteBlock.Line,
            Column = quoteBlock.Column,
        };

        var attributes = alertBlock.GetAttributes();
        attributes.AddClass("markdown-alert");
        attributes.AddClass(GetAlertType(alertType.AsSpan()));

        quoteBlock.ReplaceBy(alertBlock);
        processor.ReplaceParentContainer(quoteBlock, alertBlock);

        return true;
    }

    private static string GetAlertType(ReadOnlySpan<char> asSpan)
    {
        return $"markdown-alert-{asSpan.ToString().ToLowerInvariant()}";
    }
}