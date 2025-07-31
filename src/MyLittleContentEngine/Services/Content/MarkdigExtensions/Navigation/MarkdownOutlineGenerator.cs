using System.Text;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MyLittleContentEngine.Models;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.Navigation;

/// <summary>
/// Service for generating outlines from Markdown documents.
/// </summary>
internal static class MarkdownOutlineGenerator
{
    /// <summary>
    /// Generates an outline from a Markdown document
    /// </summary>
    /// <param name="document">The Markdown document to generate the outline from</param>
    /// <returns>An array of outline entries representing the document's headings</returns>
    public static OutlineEntry[] GenerateOutline(MarkdownDocument document)
    {
        var flatEntries = new List<(OutlineEntry Entry, int Level)>();

        // First pass: collect all headings in document order
        foreach (var node in document.Descendants())
        {
            if (node is not HeadingBlock headingBlock) continue;

            if (headingBlock.Inline == null)
            {
                continue;
            }

            // Extract title from the heading
            var title = GetPlainTextFromInline(headingBlock.Inline);

            // Skip empty headings
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            // Get the ID that will be used in the HTML output
            var id = headingBlock.TryGetAttributes()?.Id;

            // Skip headers without IDs
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var entry = new OutlineEntry(title, id, []);
            flatEntries.Add((entry, headingBlock.Level));
        }

        // Second pass: build hierarchy
        return BuildHierarchy(flatEntries);
    }

    /// <summary>
    /// Builds a hierarchical outline from a flat list of entries
    /// </summary>
    /// <param name="flatEntries">Flat list of entries with their levels</param>
    /// <returns>Hierarchical outline</returns>
    private static OutlineEntry[] BuildHierarchy(List<(OutlineEntry Entry, int Level)> flatEntries)
    {
        if (flatEntries.Count == 0) return [];

        var result = new List<OutlineEntry>();
        var stack = new List<(OutlineEntry Entry, int Level, List<OutlineEntry> Children)>();

        foreach (var (entry, level) in flatEntries)
        {
            var children = new List<OutlineEntry>();
            var newStackEntry = (entry, level, children);

            // Find the correct position in the stack
            while (stack.Count > 0 && stack[^1].Level >= level)
            {
                var lastEntry = stack[^1];
                stack.RemoveAt(stack.Count - 1);

                var completedEntry = lastEntry.Entry with { Children = lastEntry.Children.ToArray() };

                // Add to the appropriate parent
                if (stack.Count > 0)
                {
                    stack[^1].Children.Add(completedEntry);
                }
                else
                {
                    result.Add(completedEntry);
                }
            }

            // Add current entry to stack
            stack.Add(newStackEntry);
        }

        // Process remaining entries in stack
        while (stack.Count > 0)
        {
            var lastEntry = stack[^1];
            stack.RemoveAt(stack.Count - 1);

            var completedEntry = lastEntry.Entry with { Children = lastEntry.Children.ToArray() };

            if (stack.Count > 0)
            {
                stack[^1].Children.Add(completedEntry);
            }
            else
            {
                result.Add(completedEntry);
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Extracts plain text from a Markdown inline container
    /// </summary>
    /// <param name="container">The container inline to extract text from</param>
    /// <returns>Plain text content of the inline container</returns>
    private static string GetPlainTextFromInline(ContainerInline container)
    {
        var sb = new StringBuilder();

        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    sb.Append(literal.Content.ToString());
                    break;

                case EmphasisInline emphasis:
                    sb.Append(GetPlainTextFromInline(emphasis));
                    break;

                case CodeInline code:
                    sb.Append(code.Content);
                    break;

                case ContainerInline nestedContainer:
                    sb.Append(GetPlainTextFromInline(nestedContainer));
                    break;

                // For any other inline type, try to get content if it's a ContainerInline
                default:
                    if (inline is ContainerInline otherContainer)
                    {
                        sb.Append(GetPlainTextFromInline(otherContainer));
                    }
                    break;
            }
        }

        return sb.ToString();
    }
}