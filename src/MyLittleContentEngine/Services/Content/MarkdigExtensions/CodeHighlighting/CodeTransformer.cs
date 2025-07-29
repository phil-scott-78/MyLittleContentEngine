using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System.Text.RegularExpressions;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;

internal static partial class CodeTransformer
{
    [GeneratedRegex("""(?:\/\/|#|--|<!--|\*|%|'|REM\s+|;|\/\*)\s*\[!code\s+([^\]]+)\]\s*(?:-->|\*\/)?""", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CommentDirectiveRegex();

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

        // This is the key method that has been fixed.
        var lineElements = StructureCodeIntoLines(codeElement);
        if (lineElements.Count == 0) return highlightedHtml;

        var transformations = new List<LineTransformation>();

        for (var i = 0; i < lineElements.Count; i++)
        {
            var lineElement = lineElements[i];
            var lineText = lineElement.TextContent;
            var match = CommentDirectiveRegex().Match(lineText);

            if (match.Success)
            {
                var notation = match.Groups[1].Value.Trim();
                transformations.Add(new LineTransformation { LineNumber = i, Notation = notation });
                
                // For syntax-highlighted code, we need to handle the directive differently
                // because it might be split across multiple text nodes
                RemoveDirectiveFromLine(lineElement, match.Value, lineText);
            }
        }

        ApplyTransformationsToDom(lineElements, transformations);

        return preElement.OuterHtml;
    }

    /// <summary>
    /// Structures the raw content of a &lt;code&gt; element into lines and, crucially,
    /// preserves the newline characters between them in the DOM.
    /// </summary>
    private static List<IElement> StructureCodeIntoLines(IElement codeElement)
    {
        var document = codeElement.Owner;
        var lineElements = new List<IElement>();

        // Split the original content into lines.
        var lines = codeElement.InnerHtml.ReplaceLineEndings("\n").Split('\n');

        // Clear the <code> element to rebuild it with a proper structure.
        codeElement.InnerHtml = "";

        for (var i = 0; i < lines.Length; i++)
        {
            // Edge case: Don't process a final, empty "line" that results from a trailing newline.
            if (i == lines.Length - 1 && string.IsNullOrEmpty(lines[i])) continue;

            if (document == null) continue;

            var lineSpan = document.CreateElement("span");
            lineSpan.ClassName = "line";
            lineSpan.InnerHtml = string.IsNullOrWhiteSpace(lines[i]) 
                ? "  " 
                : lines[i];
            codeElement.AppendChild(lineSpan);
            lineElements.Add(lineSpan);

            if (i < lines.Length - 1)
            {
                codeElement.AppendChild(document.CreateTextNode("\n"));
            }
        }

        return lineElements;
    }

    private static void RemoveDirectiveFromLine(IElement lineElement, string directiveToRemove, string fullLineText)
    {
        // Check if we need to preserve the comment marker
        // This happens when we have something like "// [!code hl] more text" 
        var directiveIndex = fullLineText.IndexOf(directiveToRemove, StringComparison.Ordinal);
        var preserveCommentMarker = false;
        var commentMarkerToPreserve = "";
        
        if (directiveIndex != -1)
        {
            // Check if there's content after the directive in the full line
            var afterDirectiveInLine = fullLineText.Substring(directiveIndex + directiveToRemove.Length);
            // Don't trim - we want to know if there's ANY content after, including spaces
            if (afterDirectiveInLine.TrimEnd().Length > 0)
            {
                // Extract the comment marker from the directive
                var commentMatch = Regex.Match(directiveToRemove, @"^((?://|#|--|<!--|\*|%|'|REM\s*|;|/\*)\s*)\[!code", RegexOptions.IgnoreCase);
                if (commentMatch.Success)
                {
                    preserveCommentMarker = true;
                    // Ensure we have a space after the comment marker
                    var marker = commentMatch.Groups[1].Value.TrimEnd();
                    commentMarkerToPreserve = marker + (afterDirectiveInLine.StartsWith(" ") ? "" : " ");
                }
            }
        }
        
        // For syntax-highlighted code, the directive might be split across multiple text nodes
        // So we need a more sophisticated approach
        
        if (directiveIndex == -1)
        {
            // Fallback to simple text replacement
            var textNodes = lineElement.Descendants().OfType<IText>().ToList();
            foreach (var textNode in textNodes)
            {
                if (textNode.Text.Contains(directiveToRemove))
                {
                    // Check if we need to preserve comment marker in this text node
                    var nodeDirectiveIndex = textNode.Text.IndexOf(directiveToRemove, StringComparison.Ordinal);
                    var afterDirectiveInNode = textNode.Text.Substring(nodeDirectiveIndex + directiveToRemove.Length);
                    
                    // Special case: if the directive starts with a space and there's content after,
                    // we need to preserve a space when removing the directive
                    if (directiveToRemove.StartsWith(" ") && afterDirectiveInNode.TrimEnd().Length > 0)
                    {
                        textNode.TextContent = textNode.Text.Replace(directiveToRemove, " ").TrimEnd();
                        break;
                    }
                    
                    // If there's content after the directive, we might need to preserve the comment marker
                    if (afterDirectiveInNode.TrimEnd().Length > 0)
                    {
                        var commentMatch = Regex.Match(directiveToRemove, @"^((?://|#|--|<!--|\*|%|'|REM\s*|;|/\*)\s*)\[!code", RegexOptions.IgnoreCase);
                        if (commentMatch.Success)
                        {
                            var replacement = commentMatch.Groups[1].Value.TrimEnd() + " ";
                            textNode.TextContent = textNode.Text.Replace(directiveToRemove, replacement).TrimEnd();
                            break;
                        }
                    }
                    
                    var newText = textNode.Text.Replace(directiveToRemove, "").TrimEnd();
                    newText = Regex.Replace(newText, @"^\s*(//|#|--|<!--|\*|%|'|REM\s*|;|/\*|\*/)\s*$", "", RegexOptions.IgnoreCase).TrimEnd();
                    newText = Regex.Replace(newText, @"^\s*/\*\s*\*/\s*$", "", RegexOptions.IgnoreCase).TrimEnd();
                    textNode.TextContent = newText;
                    break;
                }
            }
        }
        else
        {
            // Track position as we iterate through text nodes
            var currentPosition = 0;
            var directiveEndIndex = directiveIndex + directiveToRemove.Length;
            var textNodes = lineElement.Descendants().OfType<IText>().ToList();
            var firstNodeInDirective = true;
            
            foreach (var textNode in textNodes)
            {
                var nodeStartPos = currentPosition;
                var nodeEndPos = currentPosition + textNode.Text.Length;
                
                // Check if this node contains any part of the directive
                if (nodeStartPos < directiveEndIndex && nodeEndPos > directiveIndex)
                {
                    var localStart = Math.Max(0, directiveIndex - nodeStartPos);
                    var localEnd = Math.Min(textNode.Text.Length, directiveEndIndex - nodeStartPos);
                    
                    // Remove the portion of the directive from this node
                    var beforeDirective = textNode.Text.Substring(0, localStart);
                    var afterDirective = textNode.Text.Substring(localEnd);
                    
                    // If we need to preserve the comment marker and this is the first node containing the directive
                    if (preserveCommentMarker && firstNodeInDirective)
                    {
                        // Insert the comment marker at the position where the directive starts
                        beforeDirective += commentMarkerToPreserve;
                        firstNodeInDirective = false;
                    }
                    
                    var newText = beforeDirective + afterDirective;
                    newText = newText.TrimEnd();
                    
                    // Only remove if it's just a comment marker with no content
                    if (!preserveCommentMarker && Regex.IsMatch(newText, @"^\s*(//|#|--|<!--|\*|%|'|REM\s*|;|/\*|\*/)\s*$", RegexOptions.IgnoreCase))
                    {
                        newText = "";
                    }
                    
                    textNode.TextContent = newText;
                }
                
                currentPosition = nodeEndPos;
            }
        }
        
        // Clean up empty spans and merge adjacent spans of the same class
        CleanupAndMergeSpans(lineElement);
    }
    
    private static void CleanupAndMergeSpans(IElement lineElement)
    {
        var spans = lineElement.QuerySelectorAll("span").ToList();
        
        // First pass: Remove empty spans (but keep spans that only contain whitespace between other content)
        foreach (var span in spans)
        {
            if (string.IsNullOrWhiteSpace(span.TextContent) &&
                span.ChildNodes.Length == 0)
            {
                // Check if this span is between other content
                var prevSibling = span.PreviousSibling;
                var nextSibling = span.NextSibling;
                
                // Only remove if it's not serving as spacing between elements
                if (prevSibling == null || nextSibling == null || 
                    (prevSibling is IText && string.IsNullOrWhiteSpace(prevSibling.TextContent)) ||
                    (nextSibling is IText && string.IsNullOrWhiteSpace(nextSibling.TextContent)))
                {
                    span.Remove();
                }
            }
        }
        
        // Second pass: Check for comment-only spans that should be removed
        spans = lineElement.QuerySelectorAll("span").ToList();
        foreach (var span in spans)
        {
            var content = span.TextContent.Trim();
            if (content != null && Regex.IsMatch(content, @"^(//|#|--|<!--|\*|%|'|REM|;|/\*|\*/|<!--|-->)$", RegexOptions.IgnoreCase))
            {
                // This span only contains a comment marker, check if the next span continues the comment
                var hasCommentContent = false;
                
                // Check if any following sibling has content (not just whitespace)
                var sibling = span.NextSibling;
                while (sibling != null)
                {
                    if ((sibling is IElement elem && elem.TextContent?.Trim().Length > 0) || (sibling is IText textNode && textNode.Text?.Trim().Length > 0))
                    {
                        hasCommentContent = true;
                        break;
                    }

                    sibling = sibling.NextSibling;
                }
                
                if (!hasCommentContent)
                {
                    // No continuation, remove this orphaned comment marker
                    span.Remove();
                }
            }
        }
        
        // Third pass: Merge adjacent spans with the same class
        spans = lineElement.QuerySelectorAll("span").ToList();
        for (int i = 0; i < spans.Count - 1; i++)
        {
            var currentSpan = spans[i];
            var nextSpan = spans[i + 1];
            
            // Check if they have the same class and are adjacent
            if (currentSpan.ClassName == nextSpan.ClassName && 
                currentSpan.NextElementSibling == nextSpan)
            {
                // Check if there's only whitespace text nodes between them
                var node = currentSpan.NextSibling;
                var canMerge = true;
                while (node != null && node != nextSpan)
                {
                    if (node is IText textNode && !string.IsNullOrWhiteSpace(textNode.Text))
                    {
                        canMerge = false;
                        break;
                    }
                    else if (node is IElement)
                    {
                        canMerge = false;
                        break;
                    }
                    node = node.NextSibling;
                }
                
                if (canMerge)
                {
                    // Merge the content - be careful with comment markers
                    var currentContent = currentSpan.TextContent;
                    var nextContent = nextSpan.TextContent;
                    
                    // Special handling for comment spans to preserve spacing
                    if (currentSpan.ClassList.Contains("hljs-comment") && 
                        Regex.IsMatch(currentContent, @"^(//|#|--|<!--|\*|%|'|REM|;|/\*)$") &&
                        nextContent.Length > 0 && !nextContent.StartsWith(" "))
                    {
                        currentSpan.InnerHtml += " " + nextSpan.InnerHtml;
                    }
                    else
                    {
                        currentSpan.InnerHtml += nextSpan.InnerHtml;
                    }
                    nextSpan.Remove();
                    spans.RemoveAt(i + 1);
                    i--; // Check this span again in case there are more to merge
                }
            }
        }
    }

    // This helper method and LineTransformation class remain unchanged.
    private static void ApplyTransformationsToDom(List<IElement> lineElements, List<LineTransformation> transformations)
    {
        if (transformations.Count == 0) return;

        var focusedLineNumbers = transformations
            .Where(t => t.Notation == "focus")
            .Select(t => t.LineNumber)
            .ToHashSet();

        var hasFocusedLines = focusedLineNumbers.Count != 0;

        foreach (var transform in transformations)
        {
            var lineElement = lineElements[transform.LineNumber];
            var cssClass = GetCssClassForNotation(transform.Notation);
            if (cssClass != null)
            {
                lineElement.ClassList.Add(cssClass);
            }
        }

        if (hasFocusedLines)
        {
            for (var i = 0; i < lineElements.Count; i++)
            {
                if (!focusedLineNumbers.Contains(i))
                {
                    lineElements[i].ClassList.Add("blurred");
                }
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