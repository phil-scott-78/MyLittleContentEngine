using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting
{
    internal class ColorCodingHighlighter(
        ICodeHighlighter highlighter,
        Func<CodeHighlightRenderOptions>? options) : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is not TextRendererBase<HtmlRenderer> htmlRenderer)
            {
                return;
            }

            var codeBlockRenderer = htmlRenderer.ObjectRenderers.FindExact<CodeBlockRenderer>();

            if (codeBlockRenderer is not null)
            {
                htmlRenderer.ObjectRenderers.Remove(codeBlockRenderer);
            }

            htmlRenderer.ObjectRenderers.AddIfNotAlready(
                new CodeHighlightRenderer(highlighter, options)
            );
        }
    }
}