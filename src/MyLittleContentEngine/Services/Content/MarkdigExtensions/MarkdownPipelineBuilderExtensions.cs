using Markdig;
using Markdig.Extensions.Alerts;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.Tabs;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions;

/// <summary>
/// Extension method for adding custom syntax highlighting blocks to Markdig pipeline
/// </summary>
internal static class MarkdownPipelineBuilderExtensions
{
    /// <summary>
    ///     Adds syntax highlighting support to the Markdig pipeline using the specified code highlighter.
    /// </summary>
    /// <returns>The <see cref="MarkdownPipelineBuilder"/> configured with syntax highlighting.</returns>
    public static MarkdownPipelineBuilder UseSyntaxHighlighting(this MarkdownPipelineBuilder markdownPipelineBuilder,
        ICodeHighlighter highlighter, Func<CodeHighlightRenderOptions>? options)
    {
        markdownPipelineBuilder.Extensions.AddIfNotAlready(new ColorCodingHighlighter(highlighter, options));

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
    /// Adds custom alert block support to the Markdig pipeline, replacing the built-in alert inline parser
    /// with a custom implementation.
    /// </summary>
    /// <param name="builder">The <see cref="MarkdownPipelineBuilder"/> to configure.</param>
    /// <returns>The <paramref name="builder"/> configured with custom alert blocks.</returns>
    public static MarkdownPipelineBuilder UseCustomAlerts(this MarkdownPipelineBuilder builder)
    {
        builder.UseAlertBlocks();
        builder.Extensions.AddIfNotAlready(new CustomAlertsExtension());
        return builder;
    }

    private class CustomAlertsExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            var existing = pipeline.InlineParsers.Find<Markdig.Extensions.Alerts.AlertInlineParser>();
            if (existing is not null)
            {
                pipeline.InlineParsers.Remove(existing);
            }

            pipeline.InlineParsers.InsertBefore<LinkInlineParser>(new AlertInlineParser());

        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            var blockRenderer = renderer.ObjectRenderers.FindExact<AlertBlockRenderer>();
            if (blockRenderer == null)
            {
                renderer.ObjectRenderers.InsertBefore<QuoteBlockRenderer>(new AlertBlockRenderer()
                {
                    RenderKind = AlertBlockRenderer.DefaultRenderKind
                });
            }
        }
    }
}