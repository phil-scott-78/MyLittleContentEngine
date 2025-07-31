using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.Tabs
{
    /// <summary>
    /// Extension for Markdig that transforms consecutive code blocks into tabbed containers
    /// </summary>
    internal class TabbedCodeBlocksExtension : IMarkdownExtension
    {
        private readonly Func<TabbedCodeBlockRenderOptions> _options;

        /// <summary>
        /// Extension for Markdig that transforms consecutive code blocks into tabbed containers
        /// </summary>
        public TabbedCodeBlocksExtension(Func<TabbedCodeBlockRenderOptions>? options)
        {
            _options = options ?? (() => TabbedCodeBlockRenderOptions.Default);
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            // Register document processor to find consecutive code blocks
            pipeline.DocumentProcessed += DocumentProcessed;
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                // Register a renderer for our custom block
                htmlRenderer.ObjectRenderers.AddIfNotAlready(new TabbedCodeBlockRenderer(_options));
            }
        }

        private static void DocumentProcessed(MarkdownDocument document)
        {
            var allBlocks = document.ToList();
            document.Clear();

            for (var i = 0; i < allBlocks.Count; i++)
            {
                if (allBlocks[i] is FencedCodeBlock codeBlock)
                {
                    var attributes = codeBlock.GetArgumentPairs();
                    if (!attributes.TryGetValue("tabs", out var tabs) || tabs != "true")
                    {
                        // If the code block is not marked for tabs, add it directly
                        document.Add(allBlocks[i]);
                        continue;
                    }

                    // Look ahead to find consecutive code blocks
                    var consecutiveCodeBlocks = new List<FencedCodeBlock> { codeBlock };
                    var j = i + 1;

                    // Keep looking ahead until we find a non-code block or reach the end
                    while (j < allBlocks.Count && allBlocks[j] is FencedCodeBlock nextCodeBlock)
                    {
                        consecutiveCodeBlocks.Add(nextCodeBlock);
                        j++;
                    }

                    // If we found multiple consecutive code blocks
                    if (consecutiveCodeBlocks.Count > 1)
                    {
                        // Create a tabbed container with all consecutive code blocks
                        var tabbedBlock = new TabbedCodeBlock();
                        foreach (var block in consecutiveCodeBlocks)
                        {
                            tabbedBlock.Add(block);
                        }

                        // Add the tabbed container to the document
                        document.Add(tabbedBlock);

                        // Skip ahead past the blocks we've processed
                        i = j - 1; // -1 because the loop will increment i
                    }
                    else
                    {
                        // Single code block, add it directly
                        document.Add(codeBlock);
                    }
                }
                else
                {
                    // Not a code block, add it directly
                    document.Add(allBlocks[i]);
                }
            }
        }
    }
}