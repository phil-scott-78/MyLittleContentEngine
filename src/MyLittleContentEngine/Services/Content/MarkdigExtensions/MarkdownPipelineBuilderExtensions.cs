using Markdig;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.AlertBlock;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.Tabs;
using MyLittleContentEngine.Services.Content.CodeAnalysis.SyntaxHighlighting;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions;

/// <summary>
/// Extension method for adding custom syntax highlighting blocks to Markdig pipeline
/// </summary>
internal static class MarkdownPipelineBuilderExtensions
{
    /// <summary>
    ///     Use Roslyn to colorize HTML generated from Markdown.
    /// </summary>
    /// <returns>The <see cref="MarkdownPipelineBuilder"/> configured with ColorCode.</returns>
    public static MarkdownPipelineBuilder UseSyntaxHighlighting(this MarkdownPipelineBuilder markdownPipelineBuilder,
        ISyntaxHighlightingService? syntaxHighlighter, Func<CodeHighlightRenderOptions>? options)
    {
        markdownPipelineBuilder.Extensions.AddIfNotAlready(new ColorCodingHighlighter(syntaxHighlighter, options));

        return markdownPipelineBuilder;
    }

    /// <summary>
    /// Adds support for tabbed code blocks to the specified Markdig pipeline.
    /// </summary>
    /// <param name="markdownPipelineBuilder">The <see cref="MarkdownPipelineBuilder"/> to which the tabbed code block extension will be added.</param>
    /// <param name="options">The options for rendering the tabbed code block.</param>
    /// <returns>The <paramref name="markdownPipelineBuilder"/> after the tabbed code block extension has been added.</returns>
    public static MarkdownPipelineBuilder UseTabbedCodeBlocks(this MarkdownPipelineBuilder markdownPipelineBuilder,
        Func<TabbedCodeBlockRenderOptions>? options)
    {
        markdownPipelineBuilder.Extensions.AddIfNotAlready(new TabbedCodeBlocksExtension(options));
        return markdownPipelineBuilder;
    }

    /// <summary>
    /// Adds support for custom alert blocks to the specified Markdig pipeline.
    /// </summary>
    /// <param name="markdownPipelineBuilder">The <see cref="MarkdownPipelineBuilder"/> to which the custom alert extension will be added.</param>
    /// <returns>The <paramref name="markdownPipelineBuilder"/> after the custom alert extension has been added.</returns>
    public static MarkdownPipelineBuilder UseCustomAlertBlocks(this MarkdownPipelineBuilder markdownPipelineBuilder)
    {
        markdownPipelineBuilder.Extensions.AddIfNotAlready(new CustomAlertExtension());
        return markdownPipelineBuilder;
    }
}