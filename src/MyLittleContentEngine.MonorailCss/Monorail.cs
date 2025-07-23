using System.Collections.Immutable;
using MonorailCss;
using MonorailCss.Css;
using MonorailCss.Plugins;
using MonorailCss.Plugins.Prose;

namespace MyLittleContentEngine.MonorailCss;

/// <summary>
/// Options for configuring the Monorail CSS framework integration.
/// </summary>
public class MonorailCssOptions
{
    /// <summary>
    /// Gets or sets the primary hue value used for generating the color palette.
    /// The default value is 250.
    /// </summary>
    public Func<int> PrimaryHue { get; init; } = () => 250;

    /// <summary>
    /// Gets or sets the name of the base color from the MonorailCSS color palette.
    /// The default value is "Gray".
    /// </summary>
    public Func<string> BaseColorName { get; init; } = () => ColorNames.Gray;

    /// <summary>
    /// Gets or sets the function that generates the color scheme.
    /// The function takes the primary hue as input and returns a tuple containing the accent, tertiary one, and tertiary two hues.
    /// </summary>
    public Func<int, (int, int, int)> ColorSchemeGenerator { get; init; } =
        primary => (primary + 180, primary + 90, primary - 90);

    /// <summary>
    /// Gets or sets a function to customize the CSS framework settings.
    /// This allows for advanced customization of the MonorailCSS framework.
    /// </summary>
    public Func<CssFrameworkSettings, CssFrameworkSettings> CustomCssFrameworkSettings { get; init; } =
        settings => settings;
    
    /// <summary>
    /// Gets or sets any extra CSS styles to be included in the generated stylesheet.
    /// </summary>
    public string ExtraStyles { get; init; } = string.Empty;
}

public class MonorailCssService(MonorailCssOptions options, CssClassCollector cssClassCollector)
{
    public string GetStyleSheet()
    {
        // we are only scanning razor files, not the generated files. if you use
        // code like bg-{color}-400 in the razor as a variable, that's not going to be detected.
        var cssClassValues = cssClassCollector.GetClasses();
        var styleSheet = GetCssFramework().Process(cssClassValues);

        return $"""
                {options.ExtraStyles}
                
                {styleSheet}
                """;
    }

    private CssFramework GetCssFramework()
    {
        var proseSettings = GetCustomProseSettings();


        var primaryHue = options.PrimaryHue;

        var hueValue = primaryHue.Invoke();
        var primary = ColorPaletteGenerator.GenerateFromHue(hueValue);

        var (accentHue, tertiaryOneHue, tertiaryTwoHue) = options.ColorSchemeGenerator(hueValue);
        var accent = ColorPaletteGenerator.GenerateFromHue(accentHue);

        var tertiaryOne = ColorPaletteGenerator.GenerateFromHue(tertiaryOneHue);
        var tertiaryTwo = ColorPaletteGenerator.GenerateFromHue(tertiaryTwoHue);


        var cssFrameworkSettings = new CssFrameworkSettings
        {
            OutputColorsAsVariables = true,
            DesignSystem = DesignSystem.Default with
            {
                Colors = DesignSystem.Default.Colors.AddRange(
                    new Dictionary<string, ImmutableDictionary<string, CssColor>>
                    {
                        { "primary", primary },
                        { "accent", accent },
                        { "tertiary-one", tertiaryOne }, // these two are only used for source highlighting
                        { "tertiary-two", tertiaryTwo },
                        { "base", DesignSystem.Default.Colors[options.BaseColorName()] }
                    }),
            },
            PluginSettings = ImmutableList.Create<ISettings>(proseSettings),
            Applies = ImmutableDictionary.Create<string, string>()
                .AddRange(CodeBlockApplies())
                .AddRange(TabApplies())
                .AddRange(MarkdownAlertApplies())
                .AddRange(HljsApplies())
                .AddRange(SearchModalApplies())
        };

        return new CssFramework(options.CustomCssFrameworkSettings(cssFrameworkSettings));
    }

    private static Prose.Settings GetCustomProseSettings()
    {
        var proseSettings = new Prose.Settings
        {
            CustomSettings = designSystem => new Dictionary<string, CssSettings>
            {
                {
                    "DEFAULT", new CssSettings
                    {
                        ChildRules =
                        [
                            new CssRuleSet("a",
                            [
                                new CssDeclaration(CssProperties.FontWeight, "inherit"),
                                new CssDeclaration(CssProperties.TextDecoration, "none"),]
                            ),
                            new CssRuleSet("a:not(:has(> code))",
                            [
                                new CssDeclaration(CssProperties.BorderBottomWidth, "1px"),
                                new CssDeclaration(CssProperties.BorderBottomColor,
                                    designSystem.Colors["primary"][ColorLevels._500].AsStringWithOpacity("75%"))
                            ]),

                            new CssRuleSet("blockquote",
                            [
                                new CssDeclaration(CssProperties.BorderLeftWidth, "4px"),
                                new CssDeclaration(CssProperties.PaddingLeft, "1rem"),
                                new CssDeclaration(CssProperties.BorderColor,
                                    designSystem.Colors["primary"][ColorLevels._700].AsString()),
                            ]),
                            new CssRuleSet("pre",
                            [
                                new CssDeclaration(CssProperties.BackgroundColor,
                                    designSystem.Colors["base"][ColorLevels._200].AsStringWithOpacity(".50")),
                                new CssDeclaration(CssProperties.BoxShadow,
                                    "inset 0 0 0 1px oklch(87.1% .006 286.286)"),
                                new CssDeclaration(CssProperties.BorderRadius, "0.4rem"),
                            ]),
                            new CssRuleSet("code", [
                                new CssDeclaration(CssProperties.FontWeight, "400"),
                            ]),
                            new CssRuleSet(":not(pre) > code",
                            [
                                new CssDeclaration(CssProperties.Padding, "3px 8px"),
                                new CssDeclaration(CssProperties.BoxShadow,
                                    "inset 0 0 0 1px oklch(87.1% .006 286.286)"),
                                new CssDeclaration(CssProperties.BorderRadius, "0.4rem"),

                                new CssDeclaration(CssProperties.BackgroundColor,
                                    designSystem.Colors["base"][ColorLevels._200].AsStringWithOpacity(".50")),
                                new CssDeclaration(CssProperties.Color,
                                    designSystem.Colors["base"][ColorLevels._700].AsString()),
                            ]),

                        ]
                    }
                },
                {
                    "base", new CssSettings
                    {
                        ChildRules =
                        [
                            new CssRuleSet(":not(pre) > code", [new CssDeclaration(CssProperties.FontSize, "0.8em")]),

                        ]
                    }
                },
                {
                    "sm", new CssSettings
                    {
                        ChildRules =
                        [
                            new CssRuleSet(":not(pre) > code", [new CssDeclaration(CssProperties.FontSize, "0.8em")]),

                        ]
                    }
                },
                {
                    // dark mode color overrides
                    "invert", new CssSettings
                    {
                        ChildRules =
                        [
                            new CssRuleSet("pre",
                            [
                                new CssDeclaration(CssProperties.FontWeight, "300"),
                                new CssDeclaration(CssProperties.BackgroundColor,
                                    designSystem.Colors["base"][ColorLevels._800].AsStringWithOpacity(".75")),
                                new CssDeclaration(CssProperties.BoxShadow,
                                    "inset 0 0 0 1px oklab(100% 0 5.96046e-8/.1)"),
                            ]),
                            new CssRuleSet(":not(pre) > code",
                            [
                                new CssDeclaration(CssProperties.BackgroundColor,
                                    designSystem.Colors["base"][ColorLevels._800].AsStringWithOpacity(".75")),
                                new CssDeclaration(CssProperties.Color,
                                    designSystem.Colors["base"][ColorLevels._200].AsString()),
                                new CssDeclaration(CssProperties.BoxShadow,
                                    "inset 0 0 0 1px oklab(100% 0 5.96046e-8/.1)"),
                            ])
                        ]
                    }
                },
            }.ToImmutableDictionary()
        };
        return proseSettings;
    }
    
    private static ImmutableDictionary<string, string> CodeBlockApplies()
    {
        return ImmutableDictionary.Create<string, string>()
            .AddRange(new Dictionary<string, string>
            {
                {
                    ".code-highlight-wrapper .standalone-code-container",
                    "bg-white/50 border border-base-300/75 shadow-xs rounded rounded-xl overflow-x-auto dark:bg-black/20 dark:border-base-700/50"
                },
                {
                    ".code-highlight-wrapper pre ",
                    "p-1 overflow-x-auto font-mono text-xs md:text-sm  dark:font-light leading-relaxed w-full dark:scheme-dark"
                },
                {
                    ".code-highlight-wrapper .standalone-code-highlight pre",
                    "text-base-900/90 py-2 px-2 md:px-4 dark:text-base-100/90"
                },
                {
                    ".code-highlight-wrapper pre code",
                    "font-mono"
                },
            });
    }
    
    private static ImmutableDictionary<string, string> TabApplies()
    {
        return ImmutableDictionary.Create<string, string>()
            .AddRange(new Dictionary<string, string>
            {
                {
                    ".tab-container",
                    "flex flex-col bg-base-100 border border-base-300/75 shadow-xs rounded rounded-xl overflow-x-auto dark:bg-base-950/25 dark:border-base-700/50"
                },
                { 
                    ".tab-list", 
                    "flex flex-row flex-wrap px-4 pt-1 bg-base-200/90 gap-x-2 lg:gap-x-3 dark:bg-base-800/50" },
                {
                    ".tab-button",
                    "whitespace-nowrap border-b border-transparent py-2 text-xs text-base-900/90 font-medium transition-colors hover:text-accent-500 disabled:pointer-events-none disabled:opacity-50 data-[selected=true]:text-accent-700 data-[selected=true]:border-accent-700 dark:text-base-100/90 dark:hover:text-accent-300 dark:data-[selected=true]:text-accent-400 dark:data-[selected=true]:border-accent-400"
                },
                {
                    ".tab-panel", 
                    "hidden data-[selected=true]:block py-3 px-2 md:px-4"
                },
            });
    }
    
    private static ImmutableDictionary<string, string> MarkdownAlertApplies()
    {
        const string alertFormatString =
            "fill-{0}-700 dark:fill-{0}-500 bg-{0}-100/75 border-{0}-500/20 dark:border-{0}-500/30 dark:bg-{0}-900/25 text-{0}-800 dark:text-{0}-200";

         
        
        return ImmutableDictionary.Create<string, string>()
            .AddRange(new Dictionary<string, string>
            {
                // Markdig Alert Styles
                { ".markdown-alert", "my-6 p-4 flex flex-row gap-2.5 rounded-2xl border text-sm" },
                { ".markdown-alert a", "underline" },
                { ".markdown-alert-note", string.Format(alertFormatString, "emerald") },
                { ".markdown-alert-tip", string.Format(alertFormatString, "blue") },
                { ".markdown-alert-caution", string.Format(alertFormatString, "amber") },
                { ".markdown-alert-warning", string.Format(alertFormatString, "rose") },
                { ".markdown-alert-important", string.Format(alertFormatString, "sky") },
                { ".markdown-alert-title span", "hidden" },
                { ".markdown-alert svg", "h-4 w-4 mt-0.5" },
            });
    }


    private static ImmutableDictionary<string, string> HljsApplies()
    {
        return ImmutableDictionary.Create<string, string>()
            .AddRange(new Dictionary<string, string>
            {
                // Base highlight.js styles
                { ".hljs", "text-base-900 dark:text-base-200" },

                // Comments
                { ".hljs-comment", "text-base-600 italic dark:text-base-400" },
                { ".hljs-quote", "text-base-800/50 italic dark:text-base-300" },

                // Keywords and control flow
                { ".hljs-keyword", "text-primary-800 dark:text-primary-300" },
                { ".hljs-selector-tag", "text-primary-700 dark:text-primary-300" },
                { ".hljs-literal", "text-primary-800 dark:text-primary-300" },
                { ".hljs-type", "text-base-700 dark:text-base-300" },

                // Strings and characters
                { ".hljs-string", "text-tertiary-one-800 dark:text-tertiary-one-300" },
                { ".hljs-number", "text-tertiary-one-800 dark:text-tertiary-one-300" },
                { ".hljs-regexp", "text-tertiary-one-800 dark:text-tertiary-one-300" },

                // Functions and methods
                { ".hljs-function", "text-accent-800 dark:text-accent-300" },
                { ".hljs-title", "text-accent-800 dark:text-accent-300" },
                { ".hljs-params", "text-accent-800 dark:text-accent-300" },

                // Variables and identifiers
                { ".hljs-variable", "text-tertiary-two-800 dark:text-tertiary-two-300" },
                { ".hljs-name", "text-tertiary-two-800 dark:text-tertiary-two-300" },
                { ".hljs-attr", "text-tertiary-two-800 dark:text-tertiary-two-300" },
                { ".hljs-symbol", "text-tertiary-two-800 dark:text-tertiary-two-300" },

                // Operators and punctuation
                { ".hljs-operator", "text-base-800 dark:text-base-300" },
                { ".hljs-punctuation", "text-base-800 dark:text-base-300" },

                // Special elements
                { ".hljs-built_in", "text-accent-700 dark:text-accent-300" },
                { ".hljs-class", "text-primary-800 dark:text-primary-300" },
                { ".hljs-meta", "text-base-800 dark:text-base-300" },
                { ".hljs-tag", "text-primary-800 dark:text-primary-300" },
                { ".hljs-attribute", "text-tertiary-two-800 dark:text-tertiary-two-300" },
                { ".hljs-addition", "text-green-800 dark:text-green-300" },
                { ".hljs-deletion", "text-red-800 dark:text-red-300" },
                { ".hljs-link", "text-blue-800 dark:text-blue-300" },
            });
    }

    private static ImmutableDictionary<string, string> SearchModalApplies()
    {
        return ImmutableDictionary.Create<string, string>()
            .AddRange(new Dictionary<string, string>
            {
                // Modal backdrop and container
                { ".search-modal-backdrop", "fixed inset-0 bg-base-950/50 backdrop-blur z-50 p-4 md:p-16" },
                { ".search-modal-content", " top-16 mx-auto w-full mt-8 max-w-2xl bg-base-100 dark:bg-base-900 rounded-lg shadow-xl border border-base-200 dark:border-base-700" },
                
                // Modal header and input
                { ".search-modal-header", "p-4 border-b border-base-200 dark:border-base-700" },
                { ".search-modal-input-container", "relative" },
                { ".search-modal-input", "w-full px-4 py-2 pl-10 bg-base-50 dark:bg-base-800 border border-base-300 dark:border-base-600 rounded-md text-base-900 dark:text-base-100 placeholder-base-500 dark:placeholder-base-400 focus:outline-none focus:ring-1 focus:ring-primary-500/50 focus:border-primary-500" },
                { ".search-modal-icon", "absolute left-3 top-2.5 h-4 w-4 text-base-400 dark:text-base-500" },
                
                // Results container
                { ".search-modal-results", "max-h-96 overflow-y-auto px-4 dark:scheme-dark"  },
                
                // Status messages
                { ".search-modal-placeholder", "text-center text-base-600 dark:text-base-400 py-4" },
                { ".search-modal-loading", "text-center text-base-600 dark:text-base-400 py-4" },
                { ".search-modal-no-results", "text-center text-base-600 dark:text-base-400 py-4" },
                { ".search-modal-error", "text-center text-red-600 dark:text-red-400 py-4" },
                
                // Search result items
                { ".search-result-item", "border-b border-base-200 dark:border-base-800 py-4 last:border-b-0" },
                { ".search-result-link", "block hover:bg-base-50 dark:hover:bg-base-800 rounded-md p-2 -m-2 transition-colors" },
                { ".search-result-header", "flex items-start justify-between mb-1" },
                { ".search-result-title", "text-sm font-medium text-primary-700 dark:text-primary-400 flex-1" },
                { ".search-result-score", "text-xs text-base-500 dark:text-base-500 ml-2" },
                { ".search-result-description", "text-sm text-base-600 dark:text-base-400 mb-2" },
                { ".search-result-snippet", "text-xs text-base-700 dark:text-base-500" },
                { ".search-result-url", "text-xs text-base-500 dark:text-base-500 mt-2" },
                
                // Search highlighting
                { ".search-result-title .search-highlight", "text-primary-500 dark:text-primary-100 bg-inherit" },
                { ".search-highlight", "text-base-500 dark:text-base-50 bg-inherit" },
            });
    }
}