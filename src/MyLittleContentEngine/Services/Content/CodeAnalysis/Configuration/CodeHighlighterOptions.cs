using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.Tabs;

namespace MyLittleContentEngine.Services.Content.CodeAnalysis.Configuration;

/// <summary>
/// Configuration options for the Roslyn syntax highlighting service.
/// </summary>
/// <remarks>
/// This class provides configuration settings for the <see cref="CodeHighlightRenderer"/>
/// to specify the paths required for syntax highlighting of code blocks in Markdown content.
/// </remarks>
public record CodeHighlighterOptions
{
    /// <summary>Factory for creating code highlight render options.</summary>
    public Func<CodeHighlightRenderOptions> CodeHighlightRenderOptionsFactory { get; init; } = () => CodeHighlightRenderOptions.Default;

    /// <summary>Factory for creating tabbed code block render options.</summary>
    public Func<TabbedCodeBlockRenderOptions> TabbedCodeBlockRenderOptionsFactory { get; init; } = () => TabbedCodeBlockRenderOptions.Default;
}