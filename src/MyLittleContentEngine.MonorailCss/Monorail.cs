using System.Collections.Immutable;
using MonorailCss;
using MonorailCss.Css;
using MonorailCss.Plugins;
using MonorailCss.Plugins.Prose;

namespace MyLittleContentEngine.MonorailCss;

public class MonorailCssOptions
{
    public Func<int> PrimaryHue { get; init; } = () => 250;

    public Func<string> BaseColorName { get; init; } = () => ColorNames.Gray;

    public Func<int, (int, int, int)> ColorSchemeGenerator { get; init; } =
        primary => (primary + 180, primary + 90, primary - 90);

    public Func<CssFrameworkSettings, CssFrameworkSettings> CustomCssFrameworkSettings { get; init; } =
        settings => settings;
}

public class MonorailCssService(MonorailCssOptions options, CssClassCollector cssClassCollector)
{
    private readonly MonorailCssOptions _options = options;

    public string GetStyleSheet()
    {
        // we are only scanning razor files, not the generated files. if you use
        // code like bg-{color}-400 in the razor as a variable, that's not going to be detected.
        var cssClassValues = cssClassCollector.GetClasses();
        var styleSheet = GetCssFramework().Process(cssClassValues);

        // add 
        var docsearchOverride = """
                                .DocSearch {
                                    --docsearch-primary-color: var(--monorail-color-primary-900);
                                    --docsearch-text-color: var(--monorail-color-base-800);
                                    --docsearch-spacing: 12px;
                                    --docsearch-icon-stroke-width: 1.4;
                                    --docsearch-highlight-color: var(--monorail-color-primary-600);
                                    --docsearch-muted-color: var(--monorail-color-base-700);
                                    --docsearch-container-background: var(--monorail-color-base-200);
                                    --docsearch-modal-width: 560px;
                                    --docsearch-modal-height: 600px;
                                    --docsearch-modal-background: var(--monorail-color-base-100);
                                    --docsearch-modal-shadow: 0 1px 3px 0 rgb(0 0 0 / 0.1), 0 1px 2px -1px rgb(0 0 0 / 0.1);
                                    --docsearch-searchbox-height: 56px;
                                    --docsearch-searchbox-background: var(--monorail-color-base-200);
                                    --docsearch-searchbox-focus-background: var(--monorail-color-base-100);
                                    --docsearch-searchbox-shadow: inset 0 0 0 1px var(--monorail-color-base-400);
                                    --docsearch-hit-height: 56px;
                                    --docsearch-hit-color: var(--monorail-color-base-600);
                                    --docsearch-hit-active-color: var(--monorail-color-base-100);
                                    --docsearch-hit-background: var(--monorail-color-base-100);
                                    --docsearch-hit-shadow: 0 1px 3px 0 #d4d9e1;
                                    --docsearch-key-gradient: none;
                                    --docsearch-key-shadow: none;
                                    --docsearch-key-pressed-shadow: none;
                                    --docsearch-footer-height: 44px;
                                    --docsearch-footer-background: #var(--monorail-color-base-200);
                                    --docsearch-footer-shadow: 0 -1px 0 0 var(--monorail-color-base-300);
                                    --docsearch-icon-color: var(--monorail-color-base-500);
                                }


                                html[data-theme=dark] .DocSearch {
                                    --docsearch-text-color: var(--monorail-color-base-500);
                                    --docsearch-container-background: var(--monorail-color-base-800);
                                    --docsearch-modal-background: var(--monorail-color-base-900);
                                    --docsearch-highlight-color: var(--monorail-color-primary-700);
                                    --docsearch-searchbox-background:var(--monorail-color-base-800);
                                    --docsearch-searchbox-focus-background: var(--monorail-color-base-800);
                                    --docsearch-searchbox-shadow: inset 0 0 0 1px var(--monorail-color-base-500);

                                    --docsearch-hit-color: #bec3c9;
                                    --docsearch-hit-shadow: none;
                                    --docsearch-hit-background: #090a11;
                                    --docsearch-key-gradient: none;
                                    --docsearch-key-shadow: none;
                                    --docsearch-key-pressed-shadow: none;
                                    --docsearch-footer-background: #var(--monorail-color-base-800);
                                    --docsearch-footer-shadow: 0 -1px 0 0 var(--monorail-color-base-800);
                                    --docsearch-muted-color: var(--monorail-color-base-400);
                                }
                                """;

        return $"""
                {styleSheet}

                 {docsearchOverride}
                """;
    }

    private CssFramework GetCssFramework()
    {
        var proseSettings = GetCustomProseSettings();


        var primaryHue = _options.PrimaryHue;

        var hueValue = primaryHue.Invoke();
        var primary = ColorPaletteGenerator.GenerateFromHue(hueValue);

        var (accentHue, tertiaryOneHue, tertiaryTwoHue) = _options.ColorSchemeGenerator(hueValue);
        var accent = ColorPaletteGenerator.GenerateFromHue(accentHue);

        var tertiaryOne = ColorPaletteGenerator.GenerateFromHue(tertiaryOneHue);
        var tertiaryTwo = ColorPaletteGenerator.GenerateFromHue(tertiaryTwoHue);

        const string alertFormatString =
            "fill-{0}-700 dark:fill-{0}-500 bg-{0}-100/75  border-{0}-500/20 dark:border-{0}-500/30 dark:bg-{0}-900/25 text-{0}-800 dark:text-{0}-200";

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
                        { "base", DesignSystem.Default.Colors[_options.BaseColorName()] }
                    }),
            },
            PluginSettings = ImmutableList.Create<ISettings>(proseSettings),
            Applies = ImmutableDictionary.Create<string, string>().AddRange(new Dictionary<string, string>()
                {
                    {
                        ".tab-container",
                        "flex flex-col bg-base-100 border border-base-300/75 shadow-xs rounded rounded-xl overflow-x-auto dark:bg-base-950/25 dark:border-base-700/50"
                    },
                    { ".tab-list", "flex flex-row flex-wrap px-4 pt-1 bg-base-200/90 gap-x-2 lg:gap-x-3 dark:bg-base-800/50" },
                    {
                        ".tab-button",
                        "whitespace-nowrap border-b border-transparent py-2 text-xs text-base-900/90 font-medium transition-colors hover:text-accent-500 disabled:pointer-events-none disabled:opacity-50 aria-selected:text-accent-700 aria-selected:border-accent-700 dark:text-base-100/90 dark:hover:text-accent-300 dark:aria-selected:text-accent-400 dark:aria-selected:border-accent-400"
                    },
                    { ".tab-panel", "hidden aria-selected:block py-3 px-2 md:px-4" },
                    {
                        ".code-highlight-wrapper .standalone-code-container",
                        "bg-base-100 border border-base-300/75 shadow-xs rounded rounded-xl overflow-x-auto dark:bg-base-950/25 dark:border-base-700/50"
                    },
                    {
                        ".code-highlight-wrapper pre",
                        "p-1 overflow-x-auto font-mono text-xs md:text-sm font-light leading-relaxed w-full dark:scheme-dark"
                    },
                    {
                        ".code-highlight-wrapper .standalone-code-highlight pre",
                        "text-base-900/90 py-2 px-2 md:px-4 dark:text-base-100/90"
                    },

                    // Markdig Alert Styles
                    { ".markdown-alert", "my-6 p-4 flex flex-row gap-2.5 rounded-2xl border text-sm" },
                    { ".markdown-alert-note", string.Format(alertFormatString, "emerald") },
                    { ".markdown-alert-tip", string.Format(alertFormatString, "blue") },
                    { ".markdown-alert-caution", string.Format(alertFormatString, "amber") },
                    { ".markdown-alert-warning", string.Format(alertFormatString, "rose") },
                    { ".markdown-alert-important", string.Format(alertFormatString, "sky") },
                    { ".markdown-alert-title span", "hidden" },
                    { ".markdown-alert svg", "h-4 w-4" },
                })
                .AddRange(HljsApplies())
                .AddRange(DocSearchApplies())
        };

        return new CssFramework(_options.CustomCssFrameworkSettings(cssFrameworkSettings));
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
                                new CssDeclaration(CssProperties.FontSize, ".80em"),
                                new CssDeclaration(CssProperties.Padding, "3px 8px"),
                                new CssDeclaration(CssProperties.BoxShadow,
                                    "inset 0 0 0 1px oklch(87.1% .006 286.286)"),
                                new CssDeclaration(CssProperties.BorderRadius, "0.4rem"),

                                new CssDeclaration(CssProperties.BackgroundColor,
                                    designSystem.Colors["base"][ColorLevels._200].AsStringWithOpacity(".50")),
                                new CssDeclaration(CssProperties.Color,
                                    designSystem.Colors["base"][ColorLevels._700].AsString()),
                            ]),

                            new CssRuleSet("table",
                            [
                                new CssDeclaration(CssProperties.FontSize, ".80em"),
                            ]),
                        ]
                    }
                },
                {
                    "base", new CssSettings
                    {
                        ChildRules =
                        [
                            new CssRuleSet(":not(pre) > code", [new CssDeclaration(CssProperties.FontSize, "0.77em")]),
                            new CssRuleSet("pre code", [new CssDeclaration(CssProperties.FontSize, "1em")]),
                            new CssRuleSet("table",
                            [
                                new CssDeclaration(CssProperties.FontSize, ".80em"),
                            ]),
                        ]
                    }
                },
                {
                    "sm", new CssSettings
                    {
                        ChildRules =
                        [
                            new CssRuleSet("pre code", [new CssDeclaration(CssProperties.FontSize, "1em")]),
                        ]
                    }
                },
                {
                    "lg", new CssSettings
                    {
                        ChildRules =
                        [
                            new CssRuleSet("code", [new CssDeclaration(CssProperties.FontSize, "0.8em")]),
                            new CssRuleSet("table",
                            [
                                new CssDeclaration(CssProperties.FontSize, ".80em"),
                            ]),
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

    private static ImmutableDictionary<string, string> DocSearchApplies()
    {
        return ImmutableDictionary.Create<string, string>()
            .AddRange(new Dictionary<string, string>
            {
                // DocSearch Styles. we need to throw the body tag on there to bump our specificity
                { ".DocSearch .cls-1, body .DocSearch .cls-2", "fill-base-500 dark:fill-base-300" },
                { ".DocSearch-Container", "backdrop-blur bg-base-200/75 dark:backdrop-blur dark:bg-base-800/75" },
                { ".DocSearch.DocSearch-Button", "w-full m-0 max-w-lg h-8" },
                { ".DocSearch.DocSearch-Button .DocSearch-Search-Icon", "h-4 w-4" },
                { ".DocSearch .DocSearch-Button-Keys", "hidden" },
                { ".DocSearch .DocSearch-Button-Placeholder", "text-base-500 text-sm font-normal" },
            });
    }


    private static ImmutableDictionary<string, string> HljsApplies() => ImmutableDictionary.Create<string, string>()
        .AddRange(new Dictionary<string, string>
        {
            // Base highlight.js styles
            { ".hljs", "text-base-900 dark:text-base-200" },

            // Comments
            { ".hljs-comment", "text-base-800/50 italic dark:text-base-300/50" },
            { ".hljs-quote", "text-base-800/50 italic dark:text-base-300/50" },

            // Keywords and control flow
            { ".hljs-keyword", "text-primary-700 dark:text-primary-300" },
            { ".hljs-selector-tag", "text-primary-700 dark:text-primary-300" },
            { ".hljs-literal", "text-primary-700 dark:text-primary-300" },
            { ".hljs-type", "text-base-700 dark:text-base-300" },

            // Strings and characters
            { ".hljs-string", "text-tertiary-one-700 dark:text-tertiary-one-300" },
            { ".hljs-number", "text-tertiary-one-700 dark:text-tertiary-one-300" },
            { ".hljs-regexp", "text-tertiary-one-700 dark:text-tertiary-one-300" },

            // Functions and methods
            { ".hljs-function", "text-accent-700 dark:text-accent-300" },
            { ".hljs-title", "text-accent-700 dark:text-accent-300" },
            { ".hljs-params", "text-accent-700 dark:text-accent-300" },

            // Variables and identifiers
            { ".hljs-variable", "text-tertiary-two-700 dark:text-tertiary-two-300" },
            { ".hljs-name", "text-tertiary-two-700 dark:text-tertiary-two-300" },
            { ".hljs-attr", "text-tertiary-two-700 dark:text-tertiary-two-300" },
            { ".hljs-symbol", "text-tertiary-two-700 dark:text-tertiary-two-300" },

            // Operators and punctuation
            { ".hljs-operator", "text-base-800 dark:text-base-300" },
            { ".hljs-punctuation", "text-base-800 dark:text-base-300" },

            // Special elements
            { ".hljs-built_in", "text-accent-700 dark:text-accent-300" },
            { ".hljs-class", "text-primary-700 dark:text-primary-300" },
            { ".hljs-meta", "text-base-700 dark:text-base-300" },
            { ".hljs-tag", "text-primary-700 dark:text-primary-300" },
            { ".hljs-attribute", "text-tertiary-two-700 dark:text-tertiary-two-300" },
            { ".hljs-addition", "text-green-700 dark:text-green-300" },
            { ".hljs-deletion", "text-red-700 dark:text-red-300" },
            { ".hljs-link", "text-blue-700 dark:text-blue-300" },
        });
}

static class ImmutableDictionaryExtensions
{
    public static ImmutableDictionary<string, T> AddRange<T>(this ImmutableDictionary<string, T> dictionary,
        IDictionary<string, T> items)
    {
        foreach (var item in items)
        {
            dictionary = dictionary.Add(item.Key, item.Value);
        }

        return dictionary;
    }
}