using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.Tabs;

/// <summary>
/// HTML renderer for the tabbed code block
/// </summary>
internal class TabbedCodeBlockRenderer(Func<TabbedCodeBlockRenderOptions> optionsFactory) : HtmlObjectRenderer<TabbedCodeBlock>
{
    // Add a static counter to generate unique tab group names (thread-safe)
    private static int _groupId;

    protected override void Write(HtmlRenderer renderer, TabbedCodeBlock obj)
    {
        var options = optionsFactory();

        var codeRenderer = renderer.ObjectRenderers.FindExact<CodeHighlightRenderer>() ??
                           throw new InvalidOperationException(
                               "CodeHighlightRendered should be added to ObjectRenderers");

        // Generate a unique group name using Interlocked for thread safety
        var groupName = $"tabs-{Interlocked.Increment(ref _groupId)}";

        // Container
        renderer.WriteLine($"<div class=\"{options.OuterWrapperCss}\">");
        renderer.WriteLine($"<div class=\"{options.ContainerCss}\">");

        // Tab buttons
        renderer.WriteLine($"""
                            <div role="tablist" id="tablist{groupName}" aria-orientation="horizontal" class="{options.TabListCss}">
                            """);

        var tabs = obj.OfType<FencedCodeBlock>().ToList();

        // Create buttons for each code block
        foreach (var (codeBlock, index) in tabs.Select((t, i) => (t, i)))
        {
            var arguments = codeBlock.GetArgumentPairs();
            if (!arguments.TryGetValue("title", out var title))
            {
                title = LanguageNormalizer.GetLanguageName(codeBlock.Info);
            }

            var selected = index == 0 ? "true" : "false";
            var active = index == 0 ? "active" : "inactive";

            renderer.WriteLine($"""
                                <button type="button" role="tab" aria-selected="{selected}" aria-controls="tab-content{groupName}-{index}" data-state="{active}" id="tabButton{groupName}-{index}" class="{options.TabButtonCss}" tabindex="-1" data-orientation="horizontal">{title}</button>
                                """);
        }

        renderer.WriteLine("</div>");

        // Tab content panels
        foreach (var (codeBlock, index) in tabs.Select((t, i) => (t, i)))
        {
            // Content panel with aria attributes
            renderer.WriteLine($"""
                                <div aria-labelledby="tabButton{groupName}-{index}" id="tab-content{groupName}-{index}" class="{options.TabPanelCss}">
                                """);

            codeRenderer.Write(renderer, codeBlock);
            // renderer.WriteChildren(codeBlock);
            renderer.WriteLine("</div>");
        }

        renderer.WriteLine("</div>");
        renderer.WriteLine("</div>");
    }
}