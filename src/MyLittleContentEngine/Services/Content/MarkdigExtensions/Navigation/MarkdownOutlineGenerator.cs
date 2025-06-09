using System.Text;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MyLittleContentEngine.Models;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.Navigation;

/// <summary>
/// Service for generating outlines from Markdown documents.
/// </summary>
public static class MarkdownOutlineGenerator
{
    /// <summary>
    /// Generates an outline from a Markdown document
    /// </summary>
    /// <param name="document">The Markdown document to generate the outline from</param>
    /// <returns>An array of outline entries representing the document's headings</returns>
    public static OutlineEntry[] GenerateOutline(MarkdownDocument document)
    {
        var outlineEntries = new List<OutlineEntry>();
        var headerStack = new Stack<(OutlineEntry Entry, int Level)>();

        // Traverse the document to find headings
        foreach (var node in document.Descendants())
        {
            if (node is not HeadingBlock headingBlock) continue;
            var level = headingBlock.Level;

            if (headingBlock.Inline == null)
            {
                continue;
            }

            // Extract title from the heading
            var title = GetPlainTextFromInline(headingBlock.Inline);

            // Get the ID that will be used in the HTML output
            var id = headingBlock.TryGetAttributes()?.Id;

            // Skip headers without IDs
            if (id == null)
            {
                continue;
            }

            var newEntry = new OutlineEntry(title, id, []);

            // Pop entries from the stack that are at the same or higher level
            while (headerStack.Count > 0 && headerStack.Peek().Level >= level)
            {
                headerStack.Pop();
            }

            if (headerStack.Count == 0)
            {
                // This is a top-level heading
                outlineEntries.Add(newEntry);
            }
            else
            {
                // Add as child-to-parent heading
                var (parentEntry, parentLevel) = headerStack.Peek(); // Store the parent level here
                var parentChildren = parentEntry.Children.ToList();
                parentChildren.Add(newEntry);

                // Create updated parent with new children
                var updatedParent = parentEntry with { Children = parentChildren.ToArray() };

                // Pop the old parent and push the updated one with the same level
                headerStack.Pop();
                headerStack.Push((updatedParent, parentLevel)); // Use the stored parent level

                // Update in the main list if it's a top-level entry
                if (headerStack.Count == 1)
                {
                    var index = outlineEntries.IndexOf(parentEntry);
                    if (index >= 0) // Make sure we found the parent
                    {
                        outlineEntries[index] = updatedParent;
                    }
                }
            }

            headerStack.Push((newEntry, level));
        }

        return outlineEntries.ToArray();
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