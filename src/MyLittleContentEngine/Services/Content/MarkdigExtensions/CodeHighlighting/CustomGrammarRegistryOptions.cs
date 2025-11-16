using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;

/// <summary>
/// Custom RegistryOptions that supports loading grammars from JSON strings at runtime.
/// Wraps the built-in RegistryOptions and adds support for custom grammars.
/// </summary>
internal class CustomGrammarRegistryOptions : IRegistryOptions
{
    private readonly RegistryOptions _baseOptions;
    private readonly Dictionary<string, string> _customGrammars = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomGrammarRegistryOptions"/> class.
    /// </summary>
    /// <param name="defaultTheme">The default theme to use.</param>
    public CustomGrammarRegistryOptions(ThemeName defaultTheme)
    {
        _baseOptions = new RegistryOptions(defaultTheme);
    }

    /// <summary>
    /// Gets the internal RegistryOptions instance for accessing built-in functionality.
    /// </summary>
    internal RegistryOptions BaseOptions => _baseOptions;

    /// <summary>
    /// Adds a custom grammar from a JSON string.
    /// </summary>
    /// <param name="scopeName">The scope name for the grammar (e.g., "source.tape").</param>
    /// <param name="grammarJson">The grammar definition in JSON format.</param>
    public void AddCustomGrammar(string scopeName, string grammarJson)
    {
        _customGrammars[scopeName] = grammarJson;
    }

    /// <inheritdoc />
    public IRawGrammar? GetGrammar(string scopeName)
    {
        // Check if we have a custom grammar for this scope
        if (_customGrammars.TryGetValue(scopeName, out var grammarJson))
        {
            try
            {
                // Parse the JSON grammar
                // Convert string to byte array, then to MemoryStream, then to StreamReader
                var bytes = System.Text.Encoding.UTF8.GetBytes(grammarJson);
                using var stream = new MemoryStream(bytes);
                using var reader = new StreamReader(stream);
                return GrammarReader.ReadGrammarSync(reader);
            }
            catch
            {
                // If parsing fails, fall through to base implementation
            }
        }

        // Fall back to built-in grammars
        return _baseOptions.GetGrammar(scopeName);
    }

    /// <inheritdoc />
    public IRawTheme GetTheme(string scopeName)
    {
        return _baseOptions.GetTheme(scopeName);
    }

    /// <inheritdoc />
    public ICollection<string> GetInjections(string scopeName)
    {
        return _baseOptions.GetInjections(scopeName);
    }

    /// <inheritdoc />
    public IRawTheme GetDefaultTheme()
    {
        return _baseOptions.GetDefaultTheme();
    }
}
