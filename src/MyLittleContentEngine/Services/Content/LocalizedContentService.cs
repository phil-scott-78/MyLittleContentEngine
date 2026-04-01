using System.Collections.Immutable;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content.TableOfContents;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// Coordinates content resolution across locale-specific content services.
/// Works as a pass-through for single-locale sites (the degenerate case)
/// and provides fallback + language switching for multi-locale sites.
/// </summary>
/// <typeparam name="TFrontMatter">The front matter type.</typeparam>
internal class LocalizedContentService<TFrontMatter> : ILocalizedContentService<TFrontMatter>
    where TFrontMatter : class, IFrontMatter, new()
{
    private readonly ImmutableDictionary<string, IMarkdownContentService<TFrontMatter>> _localeServices;
    private readonly LocalizationOptions _localizationOptions;

    public LocalizedContentService(
        IDictionary<string, IMarkdownContentService<TFrontMatter>> localeServices,
        LocalizationOptions localizationOptions)
    {
        _localeServices = localeServices.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
        _localizationOptions = localizationOptions;
    }

    /// <inheritdoc />
    public string DefaultLocale => _localizationOptions.DefaultLocale;

    /// <inheritdoc />
    public bool IsMultiLocale => _localizationOptions.Locales.Count > 1;

    /// <inheritdoc />
    public ImmutableList<string> SupportedLocales =>
        _localizationOptions.Locales.Keys.ToImmutableList();

    /// <inheritdoc />
    public LocaleInfo? GetLocaleInfo(string locale) =>
        _localizationOptions.Locales.GetValueOrDefault(locale);

    /// <inheritdoc />
    public IMarkdownContentService<TFrontMatter>? GetServiceForLocale(string locale) =>
        _localeServices.GetValueOrDefault(locale);

    /// <inheritdoc />
    public async Task<LocalizedContentResult<TFrontMatter>?> GetContentByUrl(string url)
    {
        var locale = GetLocaleFromUrl(url);
        var contentPath = StripLocalePrefix(url, locale);

        // Try the requested locale first
        if (_localeServices.TryGetValue(locale, out var localeService))
        {
            var result = await localeService.GetRenderedContentPageByUrlOrDefault(contentPath);
            if (result != null)
            {
                return new LocalizedContentResult<TFrontMatter>
                {
                    Page = result.Value.Page,
                    HtmlContent = result.Value.HtmlContent,
                    Locale = locale,
                };
            }
        }

        // Fallback to default locale if different from requested
        if (!string.Equals(locale, _localizationOptions.DefaultLocale, StringComparison.OrdinalIgnoreCase)
            && _localeServices.TryGetValue(_localizationOptions.DefaultLocale, out var defaultService))
        {
            var result = await defaultService.GetRenderedContentPageByUrlOrDefault(contentPath);
            if (result != null)
            {
                return new LocalizedContentResult<TFrontMatter>
                {
                    Page = result.Value.Page,
                    HtmlContent = result.Value.HtmlContent,
                    Locale = _localizationOptions.DefaultLocale,
                    IsFallback = true,
                    RequestedLocale = locale,
                };
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<ImmutableList<AlternateLanguagePage>> GetAlternateLanguages(string url)
    {
        // Single-locale sites don't need alternate languages
        if (!IsMultiLocale)
            return ImmutableList<AlternateLanguagePage>.Empty;

        var locale = GetLocaleFromUrl(url);
        var contentPath = StripLocalePrefix(url, locale);

        var results = ImmutableList<AlternateLanguagePage>.Empty;

        foreach (var (localeKey, service) in _localeServices)
        {
            var pages = await service.GetAllContentPagesAsync();
            var exists = pages.Any(p => NavigationUrlComparer.AreEqual(
                StripLocalePrefix(p.Url, localeKey), contentPath));

            if (!exists) continue;

            var localeInfo = _localizationOptions.Locales.GetValueOrDefault(localeKey);
            var localeUrl = BuildLocaleUrl(contentPath, localeKey);

            results = results.Add(new AlternateLanguagePage
            {
                Locale = localeKey,
                DisplayName = localeInfo?.DisplayName ?? localeKey,
                Url = localeUrl,
                IsCurrentLocale = string.Equals(localeKey, locale, StringComparison.OrdinalIgnoreCase),
            });
        }

        return results;
    }

    /// <inheritdoc />
    public string GetLocaleFromUrl(string url)
    {
        // Single-locale: always return default, no URL parsing needed
        if (!IsMultiLocale)
            return _localizationOptions.DefaultLocale;

        var trimmed = url.TrimStart('/');
        var firstSlash = trimmed.IndexOf('/');
        var firstSegment = firstSlash >= 0 ? trimmed[..firstSlash] : trimmed;

        // Check if the first segment is a known non-default locale
        if (!string.IsNullOrEmpty(firstSegment)
            && _localizationOptions.Locales.ContainsKey(firstSegment)
            && !string.Equals(firstSegment, _localizationOptions.DefaultLocale, StringComparison.OrdinalIgnoreCase))
        {
            return firstSegment;
        }

        return _localizationOptions.DefaultLocale;
    }

    /// <summary>
    /// Strips the locale prefix from a URL, returning the content-relative path.
    /// For the default locale (no prefix), returns the URL unchanged.
    /// </summary>
    internal string StripLocalePrefix(string url, string locale)
    {
        if (string.Equals(locale, _localizationOptions.DefaultLocale, StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        var trimmed = url.TrimStart('/');
        var prefix = locale + "/";

        if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return trimmed[prefix.Length..];
        }

        // URL matches the locale exactly (no trailing path)
        if (string.Equals(trimmed, locale, StringComparison.OrdinalIgnoreCase))
        {
            return "index";
        }

        return url;
    }

    /// <summary>
    /// Builds a full URL for a content path in a specific locale.
    /// </summary>
    private string BuildLocaleUrl(string contentPath, string locale)
    {
        var path = contentPath.TrimStart('/');

        if (string.Equals(locale, _localizationOptions.DefaultLocale, StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrEmpty(path) ? "/" : $"/{path}";
        }

        return string.IsNullOrEmpty(path) ? $"/{locale}" : $"/{locale}/{path}";
    }
}
