// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Markdig;
using Markdig.Extensions.Alerts;
using Markdig.Helpers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.AlertBlock;

/// <summary>
/// Extension for adding alerts to a Markdown pipeline.
/// </summary>
internal class CustomAlertExtension : IMarkdownExtension
{
    /// <summary>
    /// Gets or sets the delegate to render the kind of the alert.
    /// </summary>
    public Action<HtmlRenderer, StringSlice>? RenderKind { get; set; }

    /// <inheritdoc />
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        var inlineParser = pipeline.InlineParsers.Find<AlertInlineParser>();
        if (inlineParser == null)
        {
            pipeline.InlineParsers.InsertBefore<LinkInlineParser>(new AlertInlineParser());
        }
    }

    /// <inheritdoc />
    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        var originalRenderer = renderer.ObjectRenderers.FindExact<AlertBlockRenderer>();
        if (originalRenderer != null)
        {
            // Remove the original AlertBlockRenderer if it exists
            renderer.ObjectRenderers.Replace<AlertBlockRenderer>(new CustomAlertBlockRenderer()
            {
                RenderKind = RenderKind ?? CustomAlertBlockRenderer.DefaultRenderKind
            });
        }
        else
        {
            var blockRenderer = renderer.ObjectRenderers.FindExact<CustomAlertBlockRenderer>();
            if (blockRenderer == null)
            {
                renderer.ObjectRenderers.InsertBefore<QuoteBlockRenderer>(new CustomAlertBlockRenderer()
                {
                    RenderKind = RenderKind ?? CustomAlertBlockRenderer.DefaultRenderKind
                });
            }
        }

    }
}