namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;

/// <summary>
/// Options for customizing the CSS classes used in the code highlight renderer
/// </summary>
public record CodeHighlightRenderOptions
{
    /// <summary>
    /// Gets the default <see cref="CodeHighlightRenderOptions"/>.
    /// </summary>
    public static readonly CodeHighlightRenderOptions Default = new()
    {
        OuterWrapperCss = "",
        StandaloneContainerCss = "",
        PreBaseCss = "",
        PreStandaloneCss = ""
    };

    /// <summary>
    /// Gets the Monorail specific settings for <see cref="CodeHighlightRenderOptions"/>.
    /// </summary>
    public static readonly CodeHighlightRenderOptions MonorailColorful = new()
    {
        OuterWrapperCss = "not-prose",
        StandaloneContainerCss = "bg-base-100 dark:bg-primary-950/25  border border-base-300/75 dark:border-primary-700/50  shadow-xs rounded rounded-xl overflow-x-auto",
        PreBaseCss = "p-1 overflow-x-auto dark:scheme-dark font-mono text-xs md:text-sm  dark:font-light leading-relaxed w-full",
        PreStandaloneCss = "text-base-900/90 dark:text-base-100/90 py-2 px-2 md:px-4"
    };
    
    /// <summary>
    /// Gets the Monorail specific settings for <see cref="CodeHighlightRenderOptions"/>.
    /// </summary>
    public static readonly CodeHighlightRenderOptions MonorailMono = new()
    {
        OuterWrapperCss = "not-prose",
        StandaloneContainerCss = "bg-base-100 dark:bg-base-950/25  border border-base-300/75 dark:border-base-700/50  shadow-xs rounded rounded-xl overflow-x-auto",
        PreBaseCss = "p-1 overflow-x-auto dark:scheme-dark font-mono text-xs md:text-sm  dark:font-light leading-relaxed w-full",
        PreStandaloneCss = "text-base-900/90 dark:text-base-100/90 py-2 px-2 md:px-4"
    };

    /// <summary>
    /// CSS class for the outer wrapper element
    /// </summary>
    public required string OuterWrapperCss { get; init; }

    /// <summary>
    /// CSS classes for the container when not in a tabbed code block
    /// </summary>
    public required string StandaloneContainerCss { get; init; }

    /// <summary>
    /// CSS classes for the Pre element
    /// </summary>
    public required string PreBaseCss { get; init; }

    /// <summary>
    /// Additional CSS classes for the Pre element when not in a tabbed code block
    /// </summary>
    public required string PreStandaloneCss { get; init; }
}