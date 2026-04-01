using System.Collections.Immutable;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content.TableOfContents;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// A content service that aggregates pages, TOC entries, and cross-references
/// from all non-default locale content services registered in a <see cref="ILocalizedContentService{TFrontMatter}"/>.
/// This is registered as <see cref="IContentService"/> so that the TOC and static generation
/// pipelines discover non-default locale content automatically.
/// </summary>
internal class LocaleCompositeContentService<TFrontMatter> : IContentService
    where TFrontMatter : class, IFrontMatter, new()
{
    private readonly ILocalizedContentService<TFrontMatter> _localizedService;
    private readonly LocalizationOptions _localizationOptions;

    public LocaleCompositeContentService(
        ILocalizedContentService<TFrontMatter> localizedService,
        LocalizationOptions localizationOptions)
    {
        _localizedService = localizedService;
        _localizationOptions = localizationOptions;
    }

    public int SearchPriority => 10;

    public async Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync()
    {
        var pages = ImmutableList<PageToGenerate>.Empty;

        foreach (var locale in _localizationOptions.Locales.Keys)
        {
            if (string.Equals(locale, _localizationOptions.DefaultLocale, StringComparison.OrdinalIgnoreCase))
                continue;

            var service = _localizedService.GetServiceForLocale(locale);
            if (service is IContentService contentService)
            {
                pages = pages.AddRange(await contentService.GetPagesToGenerateAsync());
            }
        }

        return pages;
    }

    public async Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync()
    {
        var entries = ImmutableList<ContentTocItem>.Empty;

        foreach (var locale in _localizationOptions.Locales.Keys)
        {
            if (string.Equals(locale, _localizationOptions.DefaultLocale, StringComparison.OrdinalIgnoreCase))
                continue;

            var service = _localizedService.GetServiceForLocale(locale);
            if (service is IContentService contentService)
            {
                entries = entries.AddRange(await contentService.GetContentTocEntriesAsync());
            }
        }

        return entries;
    }

    public async Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync()
    {
        var content = ImmutableList<ContentToCopy>.Empty;

        foreach (var locale in _localizationOptions.Locales.Keys)
        {
            if (string.Equals(locale, _localizationOptions.DefaultLocale, StringComparison.OrdinalIgnoreCase))
                continue;

            var service = _localizedService.GetServiceForLocale(locale);
            if (service is IContentService contentService)
            {
                content = content.AddRange(await contentService.GetContentToCopyAsync());
            }
        }

        return content;
    }

    public async Task<ImmutableList<CrossReference>> GetCrossReferencesAsync()
    {
        var refs = ImmutableList<CrossReference>.Empty;

        foreach (var locale in _localizationOptions.Locales.Keys)
        {
            if (string.Equals(locale, _localizationOptions.DefaultLocale, StringComparison.OrdinalIgnoreCase))
                continue;

            var service = _localizedService.GetServiceForLocale(locale);
            if (service is IContentService contentService)
            {
                refs = refs.AddRange(await contentService.GetCrossReferencesAsync());
            }
        }

        return refs;
    }
}

/// <summary>
/// A no-op content service that returns empty results.
/// Used as a fallback when localization is not configured.
/// </summary>
internal class NoOpContentService : IContentService
{
    public int SearchPriority => 0;

    public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync() =>
        Task.FromResult(ImmutableList<PageToGenerate>.Empty);

    public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() =>
        Task.FromResult(ImmutableList<ContentTocItem>.Empty);

    public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() =>
        Task.FromResult(ImmutableList<ContentToCopy>.Empty);

    public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() =>
        Task.FromResult(ImmutableList<CrossReference>.Empty);
}
