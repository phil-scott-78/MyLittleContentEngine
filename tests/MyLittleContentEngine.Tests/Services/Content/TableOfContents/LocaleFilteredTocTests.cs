using System.Collections.Immutable;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;
using MyLittleContentEngine.Tests.TestHelpers;
using Shouldly;

namespace MyLittleContentEngine.Tests.Services.Content.TableOfContents;

public class LocaleFilteredTocTests
{
    // Two distinct types so TableOfContentService can store both (it keys by GetType())
    private class EnglishContentService(params (string title, string url, int order)[] pages) : LocaleContentServiceBase("en", pages);
    private class FrenchContentService(params (string title, string url, int order)[] pages) : LocaleContentServiceBase("fr", pages);

    private class LocaleContentServiceBase : IContentService
    {
        private readonly string _locale;
        private readonly ImmutableList<PageToGenerate> _pages;

        protected LocaleContentServiceBase(string locale, params (string title, string url, int order)[] pages)
        {
            _locale = locale;
            _pages = pages.Select(p => new PageToGenerate(
                p.url, p.url, new Metadata { Title = p.title, Order = p.order })).ToImmutableList();
        }

        public int SearchPriority => 0;
        public string Locale => _locale;

        public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync() => Task.FromResult(_pages);

        public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() =>
            Task.FromResult(_pages
                .Where(p => p.Metadata?.Title != null)
                .Select(p => new ContentTocItem(
                    p.Metadata!.Title!, p.Url, p.Metadata.Order, p.Url.GetSegments(), null, _locale))
                .ToImmutableList());

        public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() =>
            Task.FromResult(ImmutableList<ContentToCopy>.Empty);

        public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() =>
            Task.FromResult(ImmutableList<CrossReference>.Empty);
    }

    [Fact]
    public async Task GetNavigationTocForLocaleAsync_FiltersToSpecificLocale()
    {
        var enService = new EnglishContentService(
            ("Getting Started", "getting-started", 1),
            ("About", "about", 2));

        var frService = new FrenchContentService(
            ("Premiers pas", "fr/getting-started", 1),
            ("A propos", "fr/about", 2));

        var tocService = ServiceMockFactory.CreateTableOfContentService(enService, frService);

        var result = await tocService.GetNavigationTocForLocaleAsync("/getting-started", "en");

        result.Count.ShouldBe(2);
        result.ShouldContain(item => item.Name == "Getting Started");
        result.ShouldContain(item => item.Name == "About");
        result.ShouldNotContain(item => item.Name == "Premiers pas");
    }

    [Fact]
    public async Task GetNavigationTocForLocaleAsync_NullLocale_ReturnsAll()
    {
        var enService = new EnglishContentService(
            ("Getting Started", "getting-started", 1));

        var frService = new FrenchContentService(
            ("Premiers pas", "fr/getting-started", 1));

        var tocService = ServiceMockFactory.CreateTableOfContentService(enService, frService);

        var result = await tocService.GetNavigationTocForLocaleAsync("/getting-started", null);

        // Should include entries from both locales
        result.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetNavigationTocForLocaleAsync_UnknownLocale_ReturnsEmpty()
    {
        var enService = new EnglishContentService(
            ("Getting Started", "getting-started", 1));

        var tocService = ServiceMockFactory.CreateTableOfContentService(enService);

        var result = await tocService.GetNavigationTocForLocaleAsync("/getting-started", "de");

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetNavigationTocForLocaleAsync_NonDefaultLocale_FlatHierarchy()
    {
        var frService = new FrenchContentService(
            ("Premiers pas", "fr/getting-started", 1),
            ("A propos", "fr/about", 2));

        var tocService = ServiceMockFactory.CreateTableOfContentService(frService);

        var result = await tocService.GetNavigationTocForLocaleAsync("/fr/getting-started", "fr");

        // Should be flat like the English TOC, NOT nested under an "fr" folder
        result.Count.ShouldBe(2);
        result.ShouldContain(item => item.Name == "Premiers pas");
        result.ShouldContain(item => item.Name == "A propos");
    }

    [Fact]
    public async Task GetNavigationTocForLocaleAsync_FiltersToNonDefaultLocale_FlatStructure()
    {
        var enService = new EnglishContentService(
            ("Getting Started", "getting-started", 1),
            ("About", "about", 2));

        var frService = new FrenchContentService(
            ("Premiers pas", "fr/getting-started", 1),
            ("A propos", "fr/about", 2));

        var tocService = ServiceMockFactory.CreateTableOfContentService(enService, frService);

        var result = await tocService.GetNavigationTocForLocaleAsync("/fr/getting-started", "fr");

        result.Count.ShouldBe(2);
        result.ShouldContain(item => item.Name == "Premiers pas");
        result.ShouldContain(item => item.Name == "A propos");
        // Verify URLs are preserved with locale prefix
        result.ShouldContain(item => item.Href == "/fr/getting-started");
        result.ShouldContain(item => item.Href == "/fr/about");
    }

    [Fact]
    public void ContentTocItem_WithLocale_PreservesLocaleInRecord()
    {
        var item = new ContentTocItem("Test", "/test", 1, ["test"], null, "fr");

        item.Locale.ShouldBe("fr");
        item.Section.ShouldBeNull();
    }

    [Fact]
    public void ContentTocItem_WithoutLocale_DefaultsToNull()
    {
        var item = new ContentTocItem("Test", "/test", 1, ["test"]);

        item.Locale.ShouldBeNull();
        item.Section.ShouldBeNull();
    }
}
