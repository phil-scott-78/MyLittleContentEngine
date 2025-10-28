using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// Helper class for building HTML wrappers around highlighted code blocks.
/// Encapsulates the logic for generating consistent HTML structure with appropriate CSS classes.
/// </summary>
public static class CodeBlockHtmlBuilder
{
    /// <summary>
    /// Builds the complete HTML structure for a code block by wrapping the highlighted code in appropriate divs.
    /// </summary>
    /// <param name="highlightedHtml">The highlighted code HTML (typically pre/code tags with syntax highlighting).</param>
    /// <param name="options">CSS customization options for the wrapper elements.</param>
    /// <param name="isInTabGroup">Whether this code block is part of a tabbed code group. If true, omits standalone container classes.</param>
    /// <returns>The complete HTML structure with all wrapper divs.</returns>
    public static string BuildHtml(
        string highlightedHtml,
        CodeHighlightRenderOptions options,
        bool isInTabGroup = false)
    {
        var (containerCss, preCss) = GetCssClasses(options, isInTabGroup);

        var html = new System.Text.StringBuilder();
        html.AppendLine($"<div class=\"{options.OuterWrapperCss}\">");

        if (!string.IsNullOrEmpty(containerCss))
        {
            html.AppendLine($"<div class=\"{containerCss}\">");
        }

        html.AppendLine($"<div class=\"{preCss}\">");
        html.AppendLine(highlightedHtml);
        html.AppendLine("</div>");

        if (!string.IsNullOrEmpty(containerCss))
        {
            html.AppendLine("</div>");
        }

        html.AppendLine("</div>");

        return html.ToString();
    }

    /// <summary>
    /// Determines the CSS classes to apply based on whether the code block is standalone or in a tab group.
    /// </summary>
    /// <param name="options">CSS customization options.</param>
    /// <param name="isInTabGroup">Whether this code block is part of a tabbed code group.</param>
    /// <returns>A tuple of (containerCss, preCss) classes to apply.</returns>
    private static (string containerCss, string preCss) GetCssClasses(
        CodeHighlightRenderOptions options,
        bool isInTabGroup)
    {
        var preCss = options.PreBaseCss;
        var containerCss = "";

        if (!isInTabGroup)
        {
            containerCss = options.StandaloneContainerCss;
            preCss = $"{preCss} {options.PreStandaloneCss}".Trim();
        }

        return (containerCss, preCss);
    }
}
