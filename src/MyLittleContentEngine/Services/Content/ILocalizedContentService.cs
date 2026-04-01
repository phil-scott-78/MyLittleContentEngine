using System.Collections.Immutable;
using MyLittleContentEngine.Models;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// Coordinates content resolution across multiple locales, providing fallback behavior
/// and alternate language discovery.
/// </summary>
/// <typeparam name="TFrontMatter">The front matter type.</typeparam>
public interface ILocalizedContentService<TFrontMatter>
    where TFrontMatter : class, IFrontMatter, new()
{
    /// <summary>
    /// Gets content for a URL, handling locale detection and fallback to the default locale.
    /// </summary>
    /// <param name="url">The URL path (e.g., "/fr/getting-started" or "getting-started").</param>
    /// <returns>The localized content result, or null if not found in any locale.</returns>
    Task<LocalizedContentResult<TFrontMatter>?> GetContentByUrl(string url);

    /// <summary>
    /// Gets alternate language versions of the page at the given URL.
    /// </summary>
    /// <param name="url">The URL of the current page.</param>
    /// <returns>A list of alternate language pages, including the current one.</returns>
    Task<ImmutableList<AlternateLanguagePage>> GetAlternateLanguages(string url);

    /// <summary>
    /// Extracts the locale code from a URL path.
    /// </summary>
    /// <param name="url">The URL path.</param>
    /// <returns>The locale code (e.g., "fr"), or the default locale if no prefix is found.</returns>
    string GetLocaleFromUrl(string url);

    /// <summary>
    /// Whether this site has more than one locale configured.
    /// When false, the service acts as a pass-through to the single content service.
    /// </summary>
    bool IsMultiLocale { get; }

    /// <summary>
    /// Gets all supported locale codes.
    /// </summary>
    ImmutableList<string> SupportedLocales { get; }

    /// <summary>
    /// Gets the default locale code.
    /// </summary>
    string DefaultLocale { get; }

    /// <summary>
    /// Gets the locale info for a specific locale code.
    /// </summary>
    LocaleInfo? GetLocaleInfo(string locale);

    /// <summary>
    /// Gets the underlying content service for a specific locale.
    /// </summary>
    /// <param name="locale">The locale code.</param>
    /// <returns>The content service, or null if the locale is not registered.</returns>
    IMarkdownContentService<TFrontMatter>? GetServiceForLocale(string locale);
}

/// <summary>
/// The result of a localized content lookup, including whether fallback was used.
/// </summary>
/// <typeparam name="TFrontMatter">The front matter type.</typeparam>
public record LocalizedContentResult<TFrontMatter>
    where TFrontMatter : class, IFrontMatter
{
    /// <summary>
    /// The content page.
    /// </summary>
    public required MarkdownContentPage<TFrontMatter> Page { get; init; }

    /// <summary>
    /// The rendered HTML content.
    /// </summary>
    public required string HtmlContent { get; init; }

    /// <summary>
    /// The locale this content was resolved from.
    /// </summary>
    public required string Locale { get; init; }

    /// <summary>
    /// True if the content was served from the default locale because the requested locale had no translation.
    /// </summary>
    public bool IsFallback { get; init; }

    /// <summary>
    /// The locale that was originally requested (differs from <see cref="Locale"/> when <see cref="IsFallback"/> is true).
    /// </summary>
    public string? RequestedLocale { get; init; }
}

/// <summary>
/// Represents an alternate language version of a page.
/// </summary>
public record AlternateLanguagePage
{
    /// <summary>
    /// The locale code (e.g., "fr").
    /// </summary>
    public required string Locale { get; init; }

    /// <summary>
    /// The human-readable display name (e.g., "Francais").
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// The full URL to this language version.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Whether this is the currently active locale.
    /// </summary>
    public bool IsCurrentLocale { get; init; }
}
