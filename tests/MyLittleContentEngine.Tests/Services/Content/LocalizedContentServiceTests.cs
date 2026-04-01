using System.Collections.Immutable;
using Moq;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;
using MyLittleContentEngine.Tests.TestHelpers;
using Shouldly;

namespace MyLittleContentEngine.Tests.Services.Content;

public class LocalizedContentServiceTests
{
    private static readonly LocalizationOptions MultiLocaleOptions = new()
    {
        DefaultLocale = "en",
        Locales = ImmutableDictionary<string, LocaleInfo>.Empty
            .Add("en", new LocaleInfo("English"))
            .Add("fr", new LocaleInfo("Français"))
            .Add("es", new LocaleInfo("Español"))
    };

    private static readonly LocalizationOptions SingleLocaleOptions = new()
    {
        DefaultLocale = "en",
        Locales = ImmutableDictionary<string, LocaleInfo>.Empty
            .Add("en", new LocaleInfo("English"))
    };

    private static Mock<IMarkdownContentService<TestFrontMatter>> CreateMockService(
        params (string url, string title)[] pages)
    {
        var mock = new Mock<IMarkdownContentService<TestFrontMatter>>();
        var contentPages = pages.Select(p => new MarkdownContentPage<TestFrontMatter>
        {
            FrontMatter = new TestFrontMatter { Title = p.title },
            Url = p.url,
            MarkdownContent = $"# {p.title}",
            Outline = [],
            Tags = [],
        }).ToImmutableList();

        mock.Setup(x => x.GetAllContentPagesAsync()).ReturnsAsync(contentPages);

        foreach (var page in contentPages)
        {
            var p = page; // capture
            mock.Setup(x => x.GetRenderedContentPageByUrlOrDefault(
                    It.Is<string>(u => NavigationUrlComparer.AreEqual(u, p.Url))))
                .ReturnsAsync((p, $"<h1>{p.FrontMatter.Title}</h1>"));
        }

        return mock;
    }

    #region GetLocaleFromUrl

    [Theory]
    [InlineData("/getting-started", "en")]
    [InlineData("getting-started", "en")]
    [InlineData("/fr/getting-started", "fr")]
    [InlineData("fr/getting-started", "fr")]
    [InlineData("/es/about", "es")]
    [InlineData("index", "en")]
    [InlineData("/fr", "fr")]
    public void GetLocaleFromUrl_DetectsCorrectLocale(string url, string expectedLocale)
    {
        var enService = CreateMockService();
        var frService = CreateMockService();
        var esService = CreateMockService();

        var service = new LocalizedContentService<TestFrontMatter>(
            new Dictionary<string, IMarkdownContentService<TestFrontMatter>>
            {
                ["en"] = enService.Object,
                ["fr"] = frService.Object,
                ["es"] = esService.Object,
            },
            MultiLocaleOptions);

        service.GetLocaleFromUrl(url).ShouldBe(expectedLocale);
    }

    [Fact]
    public void GetLocaleFromUrl_SingleLocale_AlwaysReturnsDefault()
    {
        var enService = CreateMockService();
        var service = new LocalizedContentService<TestFrontMatter>(
            new Dictionary<string, IMarkdownContentService<TestFrontMatter>>
            {
                ["en"] = enService.Object,
            },
            SingleLocaleOptions);

        service.GetLocaleFromUrl("/anything/here").ShouldBe("en");
        service.GetLocaleFromUrl("fr/page").ShouldBe("en"); // "fr" is not a known locale
    }

    #endregion

    #region GetContentByUrl

    [Fact]
    public async Task GetContentByUrl_DefaultLocale_ReturnsContent()
    {
        var enService = CreateMockService(("getting-started", "Getting Started"));
        var frService = CreateMockService();

        var service = new LocalizedContentService<TestFrontMatter>(
            new Dictionary<string, IMarkdownContentService<TestFrontMatter>>
            {
                ["en"] = enService.Object,
                ["fr"] = frService.Object,
            },
            MultiLocaleOptions);

        var result = await service.GetContentByUrl("getting-started");

        result.ShouldNotBeNull();
        result.Locale.ShouldBe("en");
        result.IsFallback.ShouldBeFalse();
        result.Page.FrontMatter.Title.ShouldBe("Getting Started");
    }

    [Fact]
    public async Task GetContentByUrl_NonDefaultLocale_ReturnsLocaleContent()
    {
        var enService = CreateMockService(("getting-started", "Getting Started"));
        var frService = CreateMockService(("getting-started", "Premiers pas"));

        var service = new LocalizedContentService<TestFrontMatter>(
            new Dictionary<string, IMarkdownContentService<TestFrontMatter>>
            {
                ["en"] = enService.Object,
                ["fr"] = frService.Object,
            },
            MultiLocaleOptions);

        var result = await service.GetContentByUrl("fr/getting-started");

        result.ShouldNotBeNull();
        result.Locale.ShouldBe("fr");
        result.IsFallback.ShouldBeFalse();
        result.Page.FrontMatter.Title.ShouldBe("Premiers pas");
    }

    [Fact]
    public async Task GetContentByUrl_MissingTranslation_FallsBackToDefault()
    {
        var enService = CreateMockService(("advanced-topics", "Advanced Topics"));
        var frService = CreateMockService(); // no French translation

        var service = new LocalizedContentService<TestFrontMatter>(
            new Dictionary<string, IMarkdownContentService<TestFrontMatter>>
            {
                ["en"] = enService.Object,
                ["fr"] = frService.Object,
            },
            MultiLocaleOptions);

        var result = await service.GetContentByUrl("fr/advanced-topics");

        result.ShouldNotBeNull();
        result.Locale.ShouldBe("en");
        result.IsFallback.ShouldBeTrue();
        result.RequestedLocale.ShouldBe("fr");
        result.Page.FrontMatter.Title.ShouldBe("Advanced Topics");
    }

    [Fact]
    public async Task GetContentByUrl_NotFoundInAnyLocale_ReturnsNull()
    {
        var enService = CreateMockService();
        var frService = CreateMockService();

        var service = new LocalizedContentService<TestFrontMatter>(
            new Dictionary<string, IMarkdownContentService<TestFrontMatter>>
            {
                ["en"] = enService.Object,
                ["fr"] = frService.Object,
            },
            MultiLocaleOptions);

        var result = await service.GetContentByUrl("fr/nonexistent");
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetContentByUrl_SingleLocale_PassThrough()
    {
        var enService = CreateMockService(("about", "About"));

        var service = new LocalizedContentService<TestFrontMatter>(
            new Dictionary<string, IMarkdownContentService<TestFrontMatter>>
            {
                ["en"] = enService.Object,
            },
            SingleLocaleOptions);

        var result = await service.GetContentByUrl("about");

        result.ShouldNotBeNull();
        result.Locale.ShouldBe("en");
        result.IsFallback.ShouldBeFalse();
    }

    #endregion

    #region GetAlternateLanguages

    [Fact]
    public async Task GetAlternateLanguages_ReturnsAllLocalesWithPage()
    {
        var enService = CreateMockService(("getting-started", "Getting Started"));
        var frService = CreateMockService(("getting-started", "Premiers pas"));
        var esService = CreateMockService(); // no Spanish translation

        var service = new LocalizedContentService<TestFrontMatter>(
            new Dictionary<string, IMarkdownContentService<TestFrontMatter>>
            {
                ["en"] = enService.Object,
                ["fr"] = frService.Object,
                ["es"] = esService.Object,
            },
            MultiLocaleOptions);

        var alternates = await service.GetAlternateLanguages("getting-started");

        alternates.Count.ShouldBe(2); // en + fr, not es
        alternates.ShouldContain(a => a.Locale == "en" && a.Url == "/getting-started");
        alternates.ShouldContain(a => a.Locale == "fr" && a.Url == "/fr/getting-started");
    }

    [Fact]
    public async Task GetAlternateLanguages_MarksCurrentLocale()
    {
        var enService = CreateMockService(("about", "About"));
        var frService = CreateMockService(("about", "A propos"));

        var service = new LocalizedContentService<TestFrontMatter>(
            new Dictionary<string, IMarkdownContentService<TestFrontMatter>>
            {
                ["en"] = enService.Object,
                ["fr"] = frService.Object,
            },
            MultiLocaleOptions);

        var alternates = await service.GetAlternateLanguages("fr/about");

        var frAlt = alternates.First(a => a.Locale == "fr");
        frAlt.IsCurrentLocale.ShouldBeTrue();

        var enAlt = alternates.First(a => a.Locale == "en");
        enAlt.IsCurrentLocale.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAlternateLanguages_SingleLocale_ReturnsEmpty()
    {
        var enService = CreateMockService(("about", "About"));

        var service = new LocalizedContentService<TestFrontMatter>(
            new Dictionary<string, IMarkdownContentService<TestFrontMatter>>
            {
                ["en"] = enService.Object,
            },
            SingleLocaleOptions);

        var alternates = await service.GetAlternateLanguages("about");
        alternates.ShouldBeEmpty();
    }

    #endregion

    #region Properties

    [Fact]
    public void IsMultiLocale_MultipleLocales_ReturnsTrue()
    {
        var service = new LocalizedContentService<TestFrontMatter>(
            new Dictionary<string, IMarkdownContentService<TestFrontMatter>>
            {
                ["en"] = CreateMockService().Object,
                ["fr"] = CreateMockService().Object,
            },
            MultiLocaleOptions);

        service.IsMultiLocale.ShouldBeTrue();
    }

    [Fact]
    public void IsMultiLocale_SingleLocale_ReturnsFalse()
    {
        var service = new LocalizedContentService<TestFrontMatter>(
            new Dictionary<string, IMarkdownContentService<TestFrontMatter>>
            {
                ["en"] = CreateMockService().Object,
            },
            SingleLocaleOptions);

        service.IsMultiLocale.ShouldBeFalse();
    }

    [Fact]
    public void SupportedLocales_ReturnsAllConfigured()
    {
        var service = new LocalizedContentService<TestFrontMatter>(
            new Dictionary<string, IMarkdownContentService<TestFrontMatter>>
            {
                ["en"] = CreateMockService().Object,
                ["fr"] = CreateMockService().Object,
            },
            MultiLocaleOptions);

        service.SupportedLocales.ShouldContain("en");
        service.SupportedLocales.ShouldContain("fr");
        service.SupportedLocales.ShouldContain("es");
    }

    [Fact]
    public void GetLocaleInfo_ReturnsCorrectInfo()
    {
        var service = new LocalizedContentService<TestFrontMatter>(
            new Dictionary<string, IMarkdownContentService<TestFrontMatter>>
            {
                ["en"] = CreateMockService().Object,
            },
            MultiLocaleOptions);

        var info = service.GetLocaleInfo("fr");
        info.ShouldNotBeNull();
        info.DisplayName.ShouldBe("Français");
    }

    [Fact]
    public void GetServiceForLocale_ReturnsRegisteredService()
    {
        var enService = CreateMockService().Object;
        var service = new LocalizedContentService<TestFrontMatter>(
            new Dictionary<string, IMarkdownContentService<TestFrontMatter>>
            {
                ["en"] = enService,
            },
            SingleLocaleOptions);

        service.GetServiceForLocale("en").ShouldBe(enService);
        service.GetServiceForLocale("unknown").ShouldBeNull();
    }

    #endregion

    #region StripLocalePrefix (internal)

    [Theory]
    [InlineData("fr/getting-started", "fr", "getting-started")]
    [InlineData("/fr/getting-started", "fr", "getting-started")]
    [InlineData("getting-started", "en", "getting-started")]
    [InlineData("/getting-started", "en", "/getting-started")]
    [InlineData("fr", "fr", "index")]
    public void StripLocalePrefix_CorrectlyStrips(string url, string locale, string expected)
    {
        var service = new LocalizedContentService<TestFrontMatter>(
            new Dictionary<string, IMarkdownContentService<TestFrontMatter>>
            {
                ["en"] = CreateMockService().Object,
                ["fr"] = CreateMockService().Object,
            },
            MultiLocaleOptions);

        service.StripLocalePrefix(url, locale).ShouldBe(expected);
    }

    #endregion
}
