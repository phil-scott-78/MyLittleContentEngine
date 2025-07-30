using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;

internal static class CodeTransformer
{
    // Comment markers that we support
    private static readonly string[] CommentMarkers = ["//", "#", "--", "<!--", "*", "%", "'", "REM", ";", "/*"];
    private static readonly string[] BlockCommentEndings = ["-->", "*/"];

    // Empty comment patterns
    private static readonly string[] EmptyCommentPatterns =
    [
        "//", "#", "--", "<!--", "*", "%", "'", "REM", ";", "/*", "*/", "-->"
    ];

    private record DirectiveMatch(string FullMatch, string Notation, int Index, int EndIndex);
    private record WordHighlightInfo(string Word, string? Message);

    public static string Transform(string highlightedHtml)
    {
        if (string.IsNullOrEmpty(highlightedHtml))
            return highlightedHtml;

        var parser = new HtmlParser();
        var document = parser.ParseDocument(highlightedHtml);

        var preElement = document.QuerySelector("pre");
        if (preElement == null) return highlightedHtml;

        var codeElement = preElement.QuerySelector("code");
        if (codeElement == null) return highlightedHtml;

        var lineElements = StructureCodeIntoLines(codeElement);
        if (lineElements.Count == 0) return highlightedHtml;

        var transformations = new List<LineTransformation>();

        for (var i = 0; i < lineElements.Count; i++)
        {
            var lineElement = lineElements[i];
            var lineText = lineElement.TextContent;
            var directive = FindDirective(lineText);

            if (directive != null)
            {
                // Handle word highlighting
                if (directive.Notation.StartsWith("word:", StringComparison.OrdinalIgnoreCase))
                {
                    var wordInfo = ParseWordHighlight(directive.Notation);
                    if (wordInfo != null)
                    {
                        transformations.Add(new LineTransformation { LineNumber = i, Notation = directive.Notation });
                        RemoveDirectiveFromLine(lineElement, directive, lineText);
                        ApplyWordHighlighting(lineElement, wordInfo);
                    }
                    else
                    {
                        // Invalid word directive, just remove it without adding transformation
                        RemoveDirectiveFromLine(lineElement, directive, lineText);
                    }
                }
                else
                {
                    // Other directive types
                    transformations.Add(new LineTransformation { LineNumber = i, Notation = directive.Notation });
                    RemoveDirectiveFromLine(lineElement, directive, lineText);
                }
            }
        }

        ApplyTransformationsToDom(preElement, lineElements, transformations);

        return preElement.OuterHtml;
    }

    /// <summary>
    /// Structures the raw content of a &lt;code&gt; element into lines and preserves newlines.
    /// </summary>
    private static List<IElement> StructureCodeIntoLines(IElement codeElement)
    {
        var document = codeElement.Owner;
        var lineElements = new List<IElement>();

        var lines = codeElement.InnerHtml.ReplaceLineEndings("\n").Split('\n');
        codeElement.InnerHtml = "";

        for (var i = 0; i < lines.Length; i++)
        {
            // Skip final empty line from trailing newline
            if (i == lines.Length - 1 && string.IsNullOrEmpty(lines[i])) continue;
            if (document == null) continue;

            var lineSpan = document.CreateElement("span");
            lineSpan.ClassName = "line";
            lineSpan.InnerHtml = string.IsNullOrWhiteSpace(lines[i]) ? "  " : lines[i];
            codeElement.AppendChild(lineSpan);
            lineElements.Add(lineSpan);

            if (i < lines.Length - 1)
            {
                codeElement.AppendChild(document.CreateTextNode("\n"));
            }
        }

        return lineElements;
    }

    private static WordHighlightInfo? ParseWordHighlight(string notation)
    {
        // notation format: "word:Hello" or "word:Hello|This is my message"
        if (!notation.StartsWith("word:", StringComparison.OrdinalIgnoreCase))
            return null;

        var content = notation.Substring(5); // Remove "word:" prefix
        var parts = content.Split('|', 2);
        
        if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
            return null;

        var word = parts[0].Trim();
        var message = parts.Length > 1 ? parts[1].Trim() : null;
        
        return new WordHighlightInfo(word, string.IsNullOrWhiteSpace(message) ? null : message);
    }

    private static void ApplyWordHighlighting(IElement lineElement, WordHighlightInfo wordInfo)
    {
        var document = lineElement.Owner;
        if (document == null) return;

        // Find and highlight the word in text nodes
        var textNodes = lineElement.Descendants().OfType<IText>().ToList();
        
        foreach (var textNode in textNodes)
        {
            var text = textNode.Text;
            var wordIndex = text.IndexOf(wordInfo.Word, StringComparison.Ordinal);
            
            if (wordIndex == -1) continue;

            // Create the highlighted word element
            var highlightSpan = document.CreateElement("span");
            highlightSpan.ClassName = wordInfo.Message != null ? "word-highlight-with-message" : "word-highlight";
            highlightSpan.TextContent = wordInfo.Word;

            // Split the text and insert the highlighted span
            var beforeText = text.Substring(0, wordIndex);
            var afterText = text.Substring(wordIndex + wordInfo.Word.Length);

            var parent = textNode.Parent;
            if (parent != null)
            {
                // Insert before text if it exists
                if (!string.IsNullOrEmpty(beforeText))
                {
                    var beforeNode = document.CreateTextNode(beforeText);
                    parent.InsertBefore(beforeNode, textNode);
                }

                // Insert the highlighted span
                parent.InsertBefore(highlightSpan, textNode);

                // Insert after text if it exists
                if (!string.IsNullOrEmpty(afterText))
                {
                    var afterNode = document.CreateTextNode(afterText);
                    parent.InsertBefore(afterNode, textNode);
                }

                // Remove the original text node
                textNode.Remove();
            }

            // If there's a message, add it as a permanent callout after the line
            if (wordInfo.Message != null)
            {
                AddMessageCallout(lineElement, wordInfo.Message, highlightSpan);
            }

            // Only highlight the first occurrence to avoid issues with multiple matches
            break;
        }
    }

    private static void AddMessageCallout(IElement lineElement, string message, IElement highlightSpan)
    {
        var document = lineElement.Owner;
        if (document == null) return;

        // Create a wrapper for the highlighted word and message
        var messageWrapper = document.CreateElement("span");
        messageWrapper.ClassName = "word-highlight-wrapper";

        // Move the highlight span into the wrapper
        var parent = highlightSpan.Parent;
        if (parent != null)
        {
            parent.InsertBefore(messageWrapper, highlightSpan);
            messageWrapper.AppendChild(highlightSpan);
        }

        // Create the message callout element with user-select: none to prevent text selection
        var messageElement = document.CreateElement("div");
        messageElement.ClassName = "word-highlight-message";
        messageElement.TextContent = message;

        // Add the callout arrow
        var arrowElement = document.CreateElement("div");
        arrowElement.ClassName = "word-highlight-arrow";
        messageElement.AppendChild(arrowElement);

        
        // Add the message to the wrapper
        messageWrapper.AppendChild(messageElement);
    }

    private static DirectiveMatch? FindDirective(string text)
    {
        var span = text.AsSpan();
        var codeIndex = span.IndexOf("[!code", StringComparison.OrdinalIgnoreCase);
        if (codeIndex == -1) return null;

        // Find the closing bracket
        var closeIndex = span[codeIndex..].IndexOf(']');
        if (closeIndex == -1) return null;
        closeIndex += codeIndex;

        // Extract notation
        var notationStart = codeIndex + 6; // "[!code".Length
        while (notationStart < closeIndex && char.IsWhiteSpace(span[notationStart]))
            notationStart++;

        if (notationStart >= closeIndex) return null;

        var notation = span.Slice(notationStart, closeIndex - notationStart).ToString().Trim();

        // Check if there's a comment marker before [!code
        var beforeDirective = span[..codeIndex];
        var commentMarkerFound = false;
        var directiveStart = 0;

        foreach (var marker in CommentMarkers)
        {
            var markerIndex = beforeDirective.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex == -1) continue;
            // Check if it's only whitespace between marker and [!code
            var between = beforeDirective[(markerIndex + marker.Length)..];
            if (IsOnlyWhitespace(between))
            {
                commentMarkerFound = true;
                directiveStart = markerIndex;
                break;
            }
        }

        if (!commentMarkerFound) return null;

        // Check for optional block comment endings
        var directiveEnd = closeIndex + 1;
        var afterBracket = span[directiveEnd..];

        foreach (var ending in BlockCommentEndings)
        {
            if (afterBracket.StartsWith(ending, StringComparison.OrdinalIgnoreCase))
            {
                directiveEnd += ending.Length;
                break;
            }
        }

        var fullMatch = span.Slice(directiveStart, directiveEnd - directiveStart).ToString();
        return new DirectiveMatch(fullMatch, notation, directiveStart, directiveEnd);
    }

    private static bool IsOnlyWhitespace(ReadOnlySpan<char> span)
    {
        foreach (var c in span)
        {
            if (!char.IsWhiteSpace(c)) return false;
        }
        return true;
    }

    private static void RemoveDirectiveFromLine(IElement lineElement, DirectiveMatch directive, string fullLineText)
    {
        var shouldPreserve = DetermineCommentPreservation(directive, fullLineText);
        var commentMarker = shouldPreserve ? ExtractCommentMarker(directive) : "";

        // Try a simple case first - directive is in a single text node
        if (TryRemoveFromSingleNode(lineElement, directive.FullMatch, commentMarker))
        {
            CleanupLineElement(lineElement);
            return;
        }

        // Complex case - directive spans multiple nodes
        RemoveDirectiveAcrossNodes(lineElement, directive, commentMarker);
        CleanupLineElement(lineElement);
    }

    private static bool DetermineCommentPreservation(DirectiveMatch directive, string fullLineText)
    {
        // Check if there's content after the directive
        if (directive.EndIndex >= fullLineText.Length) return false;
        var afterDirective = fullLineText.AsSpan()[directive.EndIndex..];
        return !IsOnlyWhitespace(afterDirective);
    }

    private static string ExtractCommentMarker(DirectiveMatch directive)
    {
        // Get the comment marker from the beginning of the directive
        var directiveSpan = directive.FullMatch.AsSpan();
        var codeIndex = directiveSpan.IndexOf("[!code", StringComparison.OrdinalIgnoreCase);
        if (codeIndex == -1) return "";

        var marker = directiveSpan[..codeIndex].ToString().TrimEnd();
        return string.IsNullOrEmpty(marker) ? "" : marker;
    }

    private static bool TryRemoveFromSingleNode(IElement lineElement, string directive, string commentMarker)
    {
        var textNodes = lineElement.Descendants().OfType<IText>().ToList();

        foreach (var node in textNodes)
        {
            if (!node.Text.Contains(directive)) continue;

            // If we have a comment marker and there's content after the directive,
            // we need to ensure proper spacing
            var replacement = "";
            if (!string.IsNullOrEmpty(commentMarker))
            {
                var directiveIndex = node.Text.IndexOf(directive, StringComparison.Ordinal);
                var afterDirective = node.Text.Substring(directiveIndex + directive.Length);
                replacement = !string.IsNullOrWhiteSpace(afterDirective) && !afterDirective.StartsWith(' ')
                    ? commentMarker + " "
                    : commentMarker;
            }

            var newText = node.Text.Replace(directive, replacement);

            // Remove orphaned comment markers
            if (string.IsNullOrEmpty(commentMarker))
            {
                newText = newText.TrimEnd();
                if (IsEmptyComment(newText))
                {
                    newText = "";
                }
            }

            node.TextContent = newText;
            return true;
        }

        return false;
    }

    private static bool IsEmptyComment(string text)
    {
        var trimmed = text.Trim();

        // Check exact matches
        if (EmptyCommentPatterns.Any(pattern => string.Equals(trimmed, pattern, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Check for empty block comments like "/*  */"
        if (trimmed.StartsWith("/*", StringComparison.OrdinalIgnoreCase) &&
            trimmed.EndsWith("*/", StringComparison.OrdinalIgnoreCase))
        {
            var inner = trimmed.Substring(2, trimmed.Length - 4);
            return string.IsNullOrWhiteSpace(inner);
        }

        // Check for empty HTML comments like "<!--  -->"
        if (trimmed.StartsWith("<!--", StringComparison.OrdinalIgnoreCase) &&
            trimmed.EndsWith("-->", StringComparison.OrdinalIgnoreCase))
        {
            var inner = trimmed.Substring(4, trimmed.Length - 7);
            return string.IsNullOrWhiteSpace(inner);
        }

        return false;
    }

    private static void RemoveDirectiveAcrossNodes(IElement lineElement, DirectiveMatch directive, string commentMarker)
    {
        var textNodes = lineElement.Descendants().OfType<IText>().ToList();
        var currentPosition = 0;
        var commentMarkerAdded = false;

        foreach (var node in textNodes)
        {
            var nodeStart = currentPosition;
            var nodeEnd = currentPosition + node.Text.Length;

            // Check if this node contains part of the directive
            if (nodeStart < directive.EndIndex && nodeEnd > directive.Index)
            {
                var localStart = Math.Max(0, directive.Index - nodeStart);
                var localEnd = Math.Min(node.Text.Length, directive.EndIndex - nodeStart);

                var before = node.Text[..localStart];
                var after = node.Text[localEnd..];

                // Add comment marker to first node if needed
                if (!string.IsNullOrEmpty(commentMarker) && !commentMarkerAdded)
                {
                    before += commentMarker;
                    commentMarkerAdded = true;
                }

                node.TextContent = (before + after).TrimEnd();
            }

            currentPosition = nodeEnd;
        }
    }

    private static void CleanupLineElement(IElement lineElement)
    {
        var spans = lineElement.QuerySelectorAll("span").ToList();
        var i = 0;

        while (i < spans.Count)
        {
            var span = spans[i];
            var shouldRemove = false;

            // Check if the span should be removed
            if (string.IsNullOrWhiteSpace(span.TextContent) && span.ChildNodes.Length == 0)
            {
                // Check if it's between content (preserve as spacing)
                var prev = span.PreviousSibling;
                var next = span.NextSibling;

                shouldRemove = prev == null || next == null ||
                    (prev is IText pt && string.IsNullOrWhiteSpace(pt.TextContent)) ||
                    (next is IText nt && string.IsNullOrWhiteSpace(nt.TextContent));
            }
            else if (IsOrphanedCommentMarker(span))
            {
                shouldRemove = true;
            }

            if (shouldRemove)
            {
                span.Remove();
                spans.RemoveAt(i);
                continue;
            }

            // Try to merge with the next span
            if (i < spans.Count - 1 && TryMergeSpans(span, spans[i + 1]))
            {
                spans.RemoveAt(i + 1);
                continue;
            }

            i++;
        }
    }

    private static bool IsOrphanedCommentMarker(IElement span)
    {
        var content = span.TextContent.Trim();
        if (!IsCommentMarkerOnly(content))
            return false;

        // Check if any following content exists
        var sibling = span.NextSibling;
        while (sibling != null)
        {
            var hasContent = sibling switch
            {
                IElement elem => !string.IsNullOrWhiteSpace(elem.TextContent),
                IText text => !string.IsNullOrWhiteSpace(text.Text),
                _ => false
            };

            if (hasContent) return false;
            sibling = sibling.NextSibling;
        }

        return true;
    }

    private static bool IsCommentMarkerOnly(string text)
    {
        return CommentMarkers.Any(marker =>
            string.Equals(text, marker, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryMergeSpans(IElement current, IElement next)
    {
        // Must have the same class and be adjacent
        if (current.ClassName != next.ClassName || current.NextElementSibling != next)
            return false;

        // Check for non-whitespace between them
        var node = current.NextSibling;
        while (node != null && node != next)
        {
            switch (node)
            {
                case IText text when !string.IsNullOrWhiteSpace(text.Text):
                case IElement:
                    return false;
                default:
                    node = node.NextSibling;
                    break;
            }
        }

        // Merge with special handling for comments
        var currentContent = current.TextContent;
        var nextContent = next.TextContent;

        if (current.ClassList.Contains("hljs-comment") &&
            IsCommentMarkerOnly(currentContent) &&
            !string.IsNullOrEmpty(nextContent) && !nextContent.StartsWith(" "))
        {
            current.InnerHtml += " " + next.InnerHtml;
        }
        else
        {
            current.InnerHtml += next.InnerHtml;
        }

        next.Remove();
        return true;
    }

    private static void ApplyTransformationsToDom(IElement preElement, List<IElement> lineElements, List<LineTransformation> transformations)
    {
        if (transformations.Count == 0) return;

        // Group transformations by type for easier processing
        var transformationsByType = transformations
            .GroupBy(t => t.Notation)
            .ToDictionary(g => g.Key, g => g.Select(t => t.LineNumber).ToHashSet());

        // Apply line-level classes
        foreach (var transform in transformations)
        {
            var lineElement = lineElements[transform.LineNumber];
            var cssClass = GetCssClassForNotation(transform.Notation);
            if (cssClass != null)
            {
                lineElement.ClassList.Add(cssClass);
            }
        }

        // Handle focused lines - add blurred class to non-focused lines
        if (transformationsByType.TryGetValue("focus", out var focusedLineNumbers))
        {
            // Add the has-focused class to the pre element
            preElement.ClassList.Add("has-focused");
            
            // Add blurred class to non-focused lines
            for (var i = 0; i < lineElements.Count; i++)
            {
                if (!focusedLineNumbers.Contains(i))
                {
                    lineElements[i].ClassList.Add("blurred");
                }
            }
        }

        // Add pre-level classes for other transformations as needed
        // This makes it easy to add new transformations in the future
        if (transformationsByType.ContainsKey("highlight") || transformationsByType.ContainsKey("hl"))
        {
            preElement.ClassList.Add("has-highlighted");
        }
        
        if (transformationsByType.ContainsKey("++") || transformationsByType.ContainsKey("--"))
        {
            preElement.ClassList.Add("has-diff");
        }
        
        if (transformationsByType.ContainsKey("error"))
        {
            preElement.ClassList.Add("has-errors");
        }
        
        if (transformationsByType.ContainsKey("warning"))
        {
            preElement.ClassList.Add("has-warnings");
        }
        
        // Check for word highlighting directives
        foreach (var notation in transformationsByType.Keys)
        {
            if (notation.StartsWith("word:", StringComparison.OrdinalIgnoreCase))
            {
                preElement.ClassList.Add("has-word-highlights");
                break;
            }
        }
    }

    private static string? GetCssClassForNotation(string notation) => notation switch
    {
        "highlight" or "hl" => "highlight",
        "++" => "diff-add",
        "--" => "diff-remove",
        "focus" => "focused",
        "error" => "error",
        "warning" => "warning",
        _ => null
    };

    private sealed class LineTransformation
    {
        public int LineNumber { get; init; }
        public string Notation { get; init; } = string.Empty;
    }
}