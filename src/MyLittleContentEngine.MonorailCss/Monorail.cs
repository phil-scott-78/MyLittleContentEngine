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
        return GetCssFramework().Process(cssClassValues);
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
                FontFamilies = DesignSystem.Default.FontFamilies.Add("display",
                    new FontFamilyDefinition("Lexend, sans-serif"))
            },
            PluginSettings = new List<ISettings> { proseSettings },
            Applies =
                new
                    Dictionary<string, string> /* these are for a custom highlight.js theme using tailwind colors */
                    {
                        // Base highlight.js styles
                        { ".hljs", "text-base-900 dark:text-base-200" },
                        
                        // Comments
                        { ".hljs-comment", "text-base-800/50 italic" },
                        { ".hljs-quote", "text-base-800/50 italic" },
                        
                        // Keywords and control flow
                        { ".hljs-keyword", "text-primary-700" },
                        { ".hljs-selector-tag", "text-primary-700" },
                        { ".hljs-literal", "text-primary-700" },
                        { ".hljs-type", "text-base-700" },
                        
                        // Strings and characters
                        { ".hljs-string", "text-tertiary-one-700" },
                        { ".hljs-number", "text-tertiary-one-700" },
                        { ".hljs-regexp", "text-tertiary-one-700" },
                        
                        // Functions and methods
                        { ".hljs-function", "text-accent-700" },
                        { ".hljs-title", "text-accent-700" },
                        { ".hljs-params", "text-accent-700" },
                        
                        // Variables and identifiers
                        { ".hljs-variable", "text-tertiary-two-700" },
                        { ".hljs-name", "text-tertiary-two-700" },
                        { ".hljs-attr", "text-tertiary-two-700" },
                        { ".hljs-symbol", "text-tertiary-two-700" },
                        
                        // Operators and punctuation
                        { ".hljs-operator", "text-base-800" },
                        { ".hljs-punctuation", "text-base-800" },
                        
                        // Special elements
                        { ".hljs-built_in", "text-accent-700" },
                        { ".hljs-class", "text-primary-700" },
                        { ".hljs-meta", "text-base-700" },
                        { ".hljs-tag", "text-primary-700" },
                        { ".hljs-attribute", "text-tertiary-two-700" },
                        { ".hljs-addition", "text-green-700" },
                        { ".hljs-deletion", "text-red-700" },
                        { ".hljs-link", "text-blue-700" },

                        // Dark mode styles
                        { ".dark .hljs", "text-base-200" },
                        { ".dark .hljs-comment", "text-base-300/50 italic" },
                        { ".dark .hljs-quote", "text-base-300/50 italic" },
                        { ".dark .hljs-keyword", "text-primary-300" },
                        { ".dark .hljs-selector-tag", "text-primary-300" },
                        { ".dark .hljs-literal", "text-primary-300" },
                        { ".dark .hljs-type", "text-base-300" },
                        { ".dark .hljs-string", "text-tertiary-one-300" },
                        { ".dark .hljs-number", "text-tertiary-one-300" },
                        { ".dark .hljs-regexp", "text-tertiary-one-300" },
                        { ".dark .hljs-function", "text-accent-300" },
                        { ".dark .hljs-title", "text-accent-300" },
                        { ".dark .hljs-params", "text-accent-300" },
                        { ".dark .hljs-variable", "text-tertiary-two-300" },
                        { ".dark .hljs-name", "text-tertiary-two-300" },
                        { ".dark .hljs-attr", "text-tertiary-two-300" },
                        { ".dark .hljs-symbol", "text-tertiary-two-300" },
                        { ".dark .hljs-operator", "text-base-300" },
                        { ".dark .hljs-punctuation", "text-base-300" },
                        { ".dark .hljs-built_in", "text-accent-300" },
                        { ".dark .hljs-class", "text-primary-300" },
                        { ".dark .hljs-meta", "text-base-300" },
                        { ".dark .hljs-tag", "text-primary-300" },
                        { ".dark .hljs-attribute", "text-tertiary-two-300" },
                        { ".dark .hljs-addition", "text-green-300" },
                        { ".dark .hljs-deletion", "text-red-300" },
                        { ".dark .hljs-link", "text-blue-300" },
                    }
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
                                new CssDeclaration(CssProperties.TextDecoration, "none"),
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
                            new CssRuleSet("code",
                            [
                                new CssDeclaration(CssProperties.FontSize, ".80em"),
                                new CssDeclaration(CssProperties.FontWeight, "400"),
                                new CssDeclaration(CssProperties.Padding, "2px 5px"),
                                new CssDeclaration(CssProperties.BorderRadius, "4px"),
                                new CssDeclaration(CssProperties.BackgroundColor,
                                    designSystem.Colors["accent"][ColorLevels._200].AsStringWithOpacity(".50")),
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
                            new CssRuleSet("code",
                            [
                                new CssDeclaration(CssProperties.BackgroundColor,
                                    designSystem.Colors["base"][ColorLevels._800].AsStringWithOpacity(".75")),
                                new CssDeclaration(CssProperties.Color,
                                    designSystem.Colors["accent"][ColorLevels._400].AsString()),
                            ])
                        ]
                    }
                },
            }.ToImmutableDictionary()
        };
        return proseSettings;
    }
}