namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.Tabs;

/// <summary>
/// Options for customizing the CSS classes used in the tabbed code block renderer
/// </summary>
public record TabbedCodeBlockRenderOptions
{
    /// <summary>
    /// Gets the default settings for <see cref="TabbedCodeBlockRenderOptions"/>.
    /// </summary>
    public static readonly TabbedCodeBlockRenderOptions Default = new()
    {
        OuterWrapperCss = "",
        ContainerCss = "tab-container",
        TabListCss = "tab-list",
        TabButtonCss = "tab-button",
        TabPanelCss = "tab-panel"
    };

    /// <summary>
    /// Gets the Monorail specific settings for <see cref="TabbedCodeBlockRenderOptions"/>.
    /// </summary>
    public static readonly TabbedCodeBlockRenderOptions MonorailColorful = new()
    {
        OuterWrapperCss = "not-prose",
        ContainerCss = "flex flex-col bg-base-100 dark:bg-primary-950/25 border border-base-300/75 dark:border-primary-700/50 shadow-xs rounded rounded-xl overflow-x-auto",
        TabListCss = "flex flex-row flex-wrap px-4 pt-1 bg-base-200/90 dark:bg-primary-900/25 space-x-4",
        TabButtonCss = "whitespace-nowrap border-b border-transparent py-2 text-xs text-base-900/90 dark:text-base-100/90 font-medium transition-colors hover:text-accent-500 dark:hover:text-accent-700 disabled:pointer-events-none disabled:opacity-50 aria-selected:text-accent-700 dark:aria-selected:text-accent-400 aria-selected:border-accent-700 dark:aria-selected:border-accent-400",
        TabPanelCss = "tab-panel hidden aria-selected:block py-3 px-2 md:px-4"
    };

    public static readonly TabbedCodeBlockRenderOptions MonorailMono = new()
    {
        OuterWrapperCss = "not-prose",
        ContainerCss = "flex flex-col bg-base-100 dark:bg-base-950/25 border border-base-300/75 dark:border-base-700/50 shadow-xs rounded rounded-xl overflow-x-auto",
        TabListCss = "flex flex-row flex-wrap px-4 pt-1 bg-base-200/90 dark:bg-primary-900/25 space-x-4",
        TabButtonCss = "whitespace-nowrap border-b border-transparent py-2 text-xs text-base-900/90 dark:text-base-100/90 font-medium transition-colors hover:text-accent-500 dark:hover:text-accent-700 disabled:pointer-events-none disabled:opacity-50 aria-selected:text-accent-700 dark:aria-selected:text-accent-400 aria-selected:border-accent-700 dark:aria-selected:border-accent-400",
        TabPanelCss = "tab-panel hidden aria-selected:block py-3 px-2 md:px-4"
    };
    
    /// <summary>
    /// CSS class for the outer wrapper element
    /// </summary>
    public required string OuterWrapperCss { get; init; }

    /// <summary>
    /// CSS classes for the container
    /// </summary>
    public required string ContainerCss { get; init; }

    /// <summary>
    /// CSS classes for the tab list
    /// </summary>
    public required string TabListCss { get; init; }

    /// <summary>
    /// CSS classes for the tab buttons
    /// </summary>
    public required string TabButtonCss { get; init; }

    /// <summary>
    /// CSS classes for the tab panels
    /// </summary>
    public required string TabPanelCss { get; init; }
}