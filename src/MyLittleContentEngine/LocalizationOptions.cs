using System.Collections.Immutable;

namespace MyLittleContentEngine;

/// <summary>
/// Configuration options for multi-language content support.
/// When configured on <see cref="ContentEngineOptions"/>, enables folder-based
/// locale routing similar to Astro Starlight's internationalization system.
/// </summary>
public record LocalizationOptions
{
    /// <summary>
    /// Gets the default locale code (e.g., "en"). Content in the default locale
    /// is served without a URL prefix.
    /// </summary>
    public required string DefaultLocale { get; init; }

    /// <summary>
    /// Gets the dictionary of supported locales keyed by locale code (e.g., "en", "fr", "es").
    /// Must include the <see cref="DefaultLocale"/>.
    /// </summary>
    public required ImmutableDictionary<string, LocaleInfo> Locales { get; init; }
}

/// <summary>
/// Describes a supported locale with display metadata.
/// </summary>
/// <param name="DisplayName">Human-readable name shown in language switchers (e.g., "Francais").</param>
/// <param name="Direction">Text direction, either "ltr" or "rtl". Defaults to "ltr".</param>
/// <param name="HtmlLang">Optional BCP 47 language tag for the HTML lang attribute (e.g., "fr-FR"). Falls back to the locale key if not set.</param>
public record LocaleInfo(string DisplayName, string? Direction = "ltr", string? HtmlLang = null);
