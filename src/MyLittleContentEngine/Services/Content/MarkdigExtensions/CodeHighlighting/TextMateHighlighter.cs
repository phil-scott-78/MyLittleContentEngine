using System.Net;
using System.Text;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;

internal static class TextMateHighlighter
{
    private static readonly Registry Registry;
    private static readonly RegistryOptions RegistryOptions;
    private static readonly List<Tuple<string, string>> ScopeMappings;
    private static readonly TimeSpan TokenizeTimeLimit = TimeSpan.FromSeconds(5); // Default time limit for tokenization
    private static readonly Lock RegistryAccessLock = new Lock(); // Added for thread safety

    static TextMateHighlighter()
    {
        // Initialize the TextMate registry with grammars.
        RegistryOptions = new RegistryOptions(ThemeName.DarkPlus);
        Registry = new Registry(RegistryOptions);

        // Initialize the mapping from TextMate scopes to highlight.js CSS classes.
        ScopeMappings =
        [
            Tuple.Create("comment.line.double-slash", "hljs-comment"), // C#, Java, JS comments
            Tuple.Create("comment.block.documentation", "hljs-comment"), // Documentation comments
            Tuple.Create("comment.block", "hljs-comment"),
            Tuple.Create("comment", "hljs-comment"),
            Tuple.Create("punctuation.definition.comment", "hljs-comment"),

            // Entities (functions, types, tags, attributes)
            Tuple.Create("entity.name.function", "hljs-title"), // functions
            Tuple.Create("entity.name.type", "hljs-type"), // types (classes, structs, interfaces)
            Tuple.Create("entity.name.class", "hljs-type"),
            Tuple.Create("entity.name.interface", "hljs-type"),
            Tuple.Create("entity.name.struct", "hljs-type"),
            Tuple.Create("entity.name.enum", "hljs-type"),
            Tuple.Create("entity.name.tag", "hljs-tag"), // HTML/XML tags
            Tuple.Create("entity.other.attribute-name", "hljs-attr"), // HTML/XML attributes
            Tuple.Create("entity.other.inherited-class", "hljs-type"),
            Tuple.Create("meta.attribute.cs", "hljs-meta"), // C# attributes

            // Keywords
            Tuple.Create("keyword.control", "hljs-keyword"),
            Tuple.Create("keyword.operator.new", "hljs-keyword"), // `new` operator
            Tuple.Create("keyword.operator", "hljs-operator"), // other operators
            Tuple.Create("keyword", "hljs-keyword"), // general keywords

            // Storage (type keywords, modifiers)
            Tuple.Create("storage.type", "hljs-keyword"), // `int`, `string`, `var`
            Tuple.Create("storage.modifier", "hljs-keyword"), // `public`, `static`, `async`

            // Constants and Literals
            Tuple.Create("constant.numeric", "hljs-number"), // numbers
            Tuple.Create("constant.language", "hljs-literal"), // `true`, `false`, `null`
            Tuple.Create("constant.character.escape", "hljs-regexp"), // character escapes (e.g. \\n, \\t)
            Tuple.Create("constant.other", "hljs-literal"),

            // Strings
            Tuple.Create("string.quoted.interpolated", "hljs-string"), // interpolated string content
            Tuple.Create("string.regexp", "hljs-regexp"), // regular expressions
            Tuple.Create("string", "hljs-string"), // general strings
            Tuple.Create("punctuation.definition.string", "hljs-string"), // quotes for strings

            // Punctuation
            Tuple.Create("punctuation.definition.tag", "hljs-tag"), // <, > in HTML/XML
            Tuple.Create("punctuation.separator", "hljs-punctuation"), // commas, colons
            Tuple.Create("punctuation.terminator", "hljs-punctuation"), // semicolons
            Tuple.Create("punctuation.accessor", "hljs-punctuation"), // . in member access
            Tuple.Create("punctuation.section.embedded.begin", "hljs-punctuation"), // e.g. { in C# interpolated string
            Tuple.Create("punctuation.section.embedded.end", "hljs-punctuation"), // e.g. } in C# interpolated string
            Tuple.Create("punctuation", "hljs-punctuation"), // General punctuation

            // Variables
            Tuple.Create("variable.parameter", "hljs-variable"),
            Tuple.Create("variable.language", "hljs-variable"), // `this`, `self`
            Tuple.Create("variable.other.member", "hljs-attr"), // instance variables/members
            Tuple.Create("variable.other.object.property", "hljs-attr"), // object properties
            Tuple.Create("variable.other.constant", "hljs-literal"), // constants defined as variables
            Tuple.Create("variable.other.enummember", "hljs-attr"), // enum members
            Tuple.Create("variable", "hljs-variable"), // general variables

            // Support (built-in functions, classes, constants)
            Tuple.Create("support.function", "hljs-built_in"),
            Tuple.Create("support.class", "hljs-type"),
            Tuple.Create("support.type", "hljs-keyword"), // primitive types if not `storage.type`
            Tuple.Create("support.constant", "hljs-literal"),

            // Markup
            Tuple.Create("markup.inserted", "hljs-addition"),
            Tuple.Create("markup.deleted", "hljs-deletion"),

            // Meta scopes
            Tuple.Create("meta.selector", "hljs-selector-tag"), // CSS selectors
            Tuple.Create("meta.tag", "hljs-tag"),
            Tuple.Create("meta.definition.method", "hljs-function"), // For method definitions
            Tuple.Create("meta.definition.type", "hljs-type"), // For class/struct/interface definitions

            // Fallback
            Tuple.Create("entity", "hljs-name")
        ];
    }

    private static string? GetHljsClassForScopes(List<string> scopes)
    {
        if (scopes.Count == 0)
            return null;

        for (var i = scopes.Count - 1; i >= 0; i--)
        {
            var currentScope = scopes[i];
            foreach (var mapping in ScopeMappings.Where(mapping => currentScope.StartsWith(mapping.Item1)))
            {
                return mapping.Item2;
            }
        }

        return null; // No mapping found
    }

    public static string Highlight(string text, string language)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        IGrammar? grammar = null;

        // the registry doesn't seem to be thread-safe, so we need to lock access to it
        lock (RegistryAccessLock)
        {
            var scopeName = RegistryOptions.GetScopeByLanguageId(language.ToLowerInvariant());

            if (!string.IsNullOrEmpty(scopeName))
            {
                grammar = Registry.LoadGrammar(scopeName);
            }

            // Attempt a broader search if a specific scopeName not found or language ID is an alias
            if (grammar == null)
            {
                // Try common conventions like "source.{lang}" or just the language name if it's a known short form
                // TextMateSharp's grammars often use "source.{language_name}"
                var potentialScopeNames = new List<string>
                {
                    $"source.{language.ToLowerInvariant()}",
                    language.ToLowerInvariant() // Some grammars might be registered with short names
                };

                foreach (var potentialScope in potentialScopeNames)
                {
                    try
                    {
                        grammar = Registry.LoadGrammar(potentialScope);
                        if (grammar != null) break;
                    }
                    catch
                    {
                        // Ignore exceptions if a potential scope name is invalid for lookup
                    }
                }
            }

            // If no grammar was found, return the text as a plain code block
            if (grammar == null)
            {
                var escapedCode = WebUtility.HtmlEncode(text);
                return $"<pre><code class=\"language-{language} code\">{escapedCode}</code></pre>";
            }

            var sb = new StringBuilder();
            sb.Append("<pre><code>");


            var lines = text.SplitNewLines();
            IStateStack? ruleStack = null; // IRawStack can be null initially

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                // Pass the time limit to TokenizeLine
                var result = grammar.TokenizeLine(line, ruleStack, TokenizeTimeLimit);
                ruleStack = result.RuleStack;

                var currentIndex = 0;
                foreach (var token in result.Tokens)
                {
                    if (token.StartIndex > currentIndex)
                    {
                        sb.Append(WebUtility.HtmlEncode(line.Substring(currentIndex, token.StartIndex - currentIndex)));
                    }

                    // Ensure token.Length does not exceed the line boundary
                    var length = token.Length;
                    if (token.StartIndex + length > line.Length)
                    {
                        length = line.Length - token.StartIndex;
                    }

                    var tokenText = line.Substring(token.StartIndex, length);
                    var escapedTokenText = WebUtility.HtmlEncode(tokenText);
                    var hljsClass = GetHljsClassForScopes(token.Scopes);

                    if (!string.IsNullOrEmpty(hljsClass))
                    {
                        sb.Append($"<span class=\"{hljsClass}\">{escapedTokenText}</span>");
                    }
                    else
                    {
                        sb.Append(escapedTokenText);
                    }

                    currentIndex = token.StartIndex + length;
                }

                if (i < lines.Length - 1)
                {
                    sb.AppendLine();
                }
            }

            sb.Append("</code></pre>");
            return sb.ToString();
        }
    }
}