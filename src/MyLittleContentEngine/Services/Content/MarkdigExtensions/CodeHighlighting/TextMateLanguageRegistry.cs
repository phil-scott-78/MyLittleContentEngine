using TextMateSharp.Grammars;
using TextMateSharp.Registry;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;

/// <summary>
/// Registry for managing TextMate language grammars and scope mappings.
/// Allows registration of custom languages in addition to built-in ones.
/// </summary>
public class TextMateLanguageRegistry
{
    private readonly Registry _registry;
    private readonly CustomGrammarRegistryOptions _registryOptions;
    private readonly Dictionary<string, string> _customScopeMappings = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TextMateLanguageRegistry"/> class.
    /// </summary>
    /// <param name="configure">Optional callback to configure custom languages.</param>
    public TextMateLanguageRegistry(Action<TextMateLanguageRegistry>? configure = null)
    {
        _registryOptions = new CustomGrammarRegistryOptions(ThemeName.DarkPlus);
        _registry = new Registry(_registryOptions);

        // Allow consumer to register custom languages
        configure?.Invoke(this);
    }

    /// <summary>
    /// Gets the internal TextMate registry instance.
    /// </summary>
    internal Registry Registry => _registry;

    /// <summary>
    /// Gets the registry options.
    /// </summary>
    internal CustomGrammarRegistryOptions RegistryOptions => _registryOptions;

    /// <summary>
    /// Adds a custom language-to-scope mapping.
    /// </summary>
    /// <param name="languageId">The language identifier (e.g., "mylang").</param>
    /// <param name="scopeName">The TextMate scope name (e.g., "source.mylang").</param>
    /// <returns>This instance for method chaining.</returns>
    public TextMateLanguageRegistry AddGrammar(string languageId, string scopeName)
    {
        if (string.IsNullOrWhiteSpace(languageId))
            throw new ArgumentException("Language ID cannot be null or whitespace.", nameof(languageId));
        if (string.IsNullOrWhiteSpace(scopeName))
            throw new ArgumentException("Scope name cannot be null or whitespace.", nameof(scopeName));

        _customScopeMappings[languageId.ToLowerInvariant()] = scopeName;
        return this;
    }

    /// <summary>
    /// Loads a grammar from a JSON string and associates it with a language identifier.
    /// </summary>
    /// <param name="languageId">The language identifier (e.g., "mylang").</param>
    /// <param name="grammarJson">The TextMate grammar in JSON format.</param>
    /// <returns>This instance for method chaining.</returns>
    public TextMateLanguageRegistry AddGrammarFromJson(string languageId, string grammarJson)
    {
        if (string.IsNullOrWhiteSpace(languageId))
            throw new ArgumentException("Language ID cannot be null or whitespace.", nameof(languageId));
        if (string.IsNullOrWhiteSpace(grammarJson))
            throw new ArgumentException("Grammar JSON cannot be null or whitespace.", nameof(grammarJson));

        // Parse the JSON to get the scope name from the grammar itself
        // If parsing fails or scopeName is not in the JSON, fall back to a default
        var scopeName = $"source.{languageId.ToLowerInvariant()}";

        try
        {
            // Try to extract scopeName from the JSON
            var jsonDoc = System.Text.Json.JsonDocument.Parse(grammarJson);
            if (jsonDoc.RootElement.TryGetProperty("scopeName", out var scopeNameElement))
            {
                var extractedScope = scopeNameElement.GetString();
                if (!string.IsNullOrEmpty(extractedScope))
                {
                    scopeName = extractedScope;
                }
            }
        }
        catch
        {
            // If parsing fails, use the default scope name
        }

        // Store the custom grammar in the registry options
        _registryOptions.AddCustomGrammar(scopeName, grammarJson);

        // Store the language ID to scope name mapping
        _customScopeMappings[languageId.ToLowerInvariant()] = scopeName;

        return this;
    }

    /// <summary>
    /// Attempts to get the scope name for a given language identifier.
    /// Checks custom mappings first, then falls back to built-in language IDs.
    /// </summary>
    /// <param name="languageId">The language identifier.</param>
    /// <returns>The scope name if found; otherwise, null.</returns>
    internal string? GetScopeNameForLanguage(string languageId)
    {
        var normalizedId = languageId.ToLowerInvariant();

        // Check custom mappings first
        if (_customScopeMappings.TryGetValue(normalizedId, out var scopeName))
        {
            return scopeName;
        }

        // Fall back to built-in language IDs via the base options
        return _registryOptions.BaseOptions.GetScopeByLanguageId(normalizedId);
    }
}
