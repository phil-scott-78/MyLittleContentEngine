using System.Collections.Immutable;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;
using Shouldly;

namespace MyLittleContentEngine.Tests.Content;

public class FlatFileRedirectContentServiceTests
{
    private static IServiceProvider BuildServiceProvider(params IContentService[] contentServices)
    {
        var services = new ServiceCollection();
        foreach (var svc in contentServices)
            services.AddSingleton(svc);

        services.AddTransient<IContentService, FlatFileRedirectContentService>();
        return services.BuildServiceProvider();
    }

    private static string DecodeContent(ContentToCreate content) =>
        Encoding.UTF8.GetString(content.Bytes);

    // ── Core redirect generation ──────────────────────────────────────────────

    [Fact]
    public async Task Generates_redirect_for_folder_based_page()
    {
        var stub = new StubContentService(
        [
            new PageToGenerate("/about", "about/index.html"),
        ]);

        var sp = BuildServiceProvider(stub);
        var sut = new FlatFileRedirectContentService(sp);

        var result = await sut.GetContentToCreateAsync();

        result.Count.ShouldBe(1);
        result[0].TargetPath.Value.ShouldBe("about.html");
        DecodeContent(result[0]).ShouldContain("about/");
    }

    [Fact]
    public async Task Redirect_html_matches_RedirectHelper_format()
    {
        var stub = new StubContentService(
        [
            new PageToGenerate("/about", "about/index.html"),
        ]);

        var sp = BuildServiceProvider(stub);
        var sut = new FlatFileRedirectContentService(sp);

        var result = await sut.GetContentToCreateAsync();

        var expected = RedirectHelper.GetRedirectHtml("about/");
        DecodeContent(result[0]).ShouldBe(expected);
    }

    [Fact]
    public async Task Skips_root_index_html()
    {
        var stub = new StubContentService(
        [
            new PageToGenerate("/", "index.html"),
        ]);

        var sp = BuildServiceProvider(stub);
        var sut = new FlatFileRedirectContentService(sp);

        var result = await sut.GetContentToCreateAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Skips_flat_file_pages()
    {
        var stub = new StubContentService(
        [
            new PageToGenerate("/about", "about.html"),
        ]);

        var sp = BuildServiceProvider(stub);
        var sut = new FlatFileRedirectContentService(sp);

        var result = await sut.GetContentToCreateAsync();

        result.ShouldBeEmpty();
    }

    // ── Nested and tag paths ──────────────────────────────────────────────────

    [Fact]
    public async Task Handles_nested_folder_paths()
    {
        var stub = new StubContentService(
        [
            new PageToGenerate("/docs/intro", "docs/intro/index.html"),
        ]);

        var sp = BuildServiceProvider(stub);
        var sut = new FlatFileRedirectContentService(sp);

        var result = await sut.GetContentToCreateAsync();

        result.Count.ShouldBe(1);
        result[0].TargetPath.Value.ShouldBe("docs/intro.html");

        var expected = RedirectHelper.GetRedirectHtml("intro/");
        DecodeContent(result[0]).ShouldBe(expected);
    }

    [Fact]
    public async Task Handles_tag_pages()
    {
        var stub = new StubContentService(
        [
            new PageToGenerate("/tags/csharp/", "tags/csharp/index.html"),
        ]);

        var sp = BuildServiceProvider(stub);
        var sut = new FlatFileRedirectContentService(sp);

        var result = await sut.GetContentToCreateAsync();

        result.Count.ShouldBe(1);
        result[0].TargetPath.Value.ShouldBe("tags/csharp.html");

        var expected = RedirectHelper.GetRedirectHtml("csharp/");
        DecodeContent(result[0]).ShouldBe(expected);
    }

    [Fact]
    public async Task Handles_deeply_nested_paths()
    {
        var stub = new StubContentService(
        [
            new PageToGenerate("/a/b/c", "a/b/c/index.html"),
        ]);

        var sp = BuildServiceProvider(stub);
        var sut = new FlatFileRedirectContentService(sp);

        var result = await sut.GetContentToCreateAsync();

        result.Count.ShouldBe(1);
        result[0].TargetPath.Value.ShouldBe("a/b/c.html");

        var expected = RedirectHelper.GetRedirectHtml("c/");
        DecodeContent(result[0]).ShouldBe(expected);
    }

    // ── Collision avoidance ───────────────────────────────────────────────────

    [Fact]
    public async Task Skips_redirect_when_flat_file_already_claimed_by_another_service()
    {
        var markdownService = new StubContentService(
        [
            new PageToGenerate("/about", "about/index.html"),
        ]);

        var razorService = new StubContentService(
        [
            new PageToGenerate("/about", "about.html"),
        ]);

        var sp = BuildServiceProvider(markdownService, razorService);
        var sut = new FlatFileRedirectContentService(sp);

        var result = await sut.GetContentToCreateAsync();

        result.ShouldBeEmpty();
    }

    // ── Deduplication ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Deduplicates_same_output_from_multiple_services()
    {
        var service1 = new StubContentService(
        [
            new PageToGenerate("/about", "about/index.html"),
        ]);

        var service2 = new StubContentService(
        [
            new PageToGenerate("/about", "about/index.html"),
        ]);

        var sp = BuildServiceProvider(service1, service2);
        var sut = new FlatFileRedirectContentService(sp);

        var result = await sut.GetContentToCreateAsync();

        result.Count.ShouldBe(1);
    }

    // ── Multiple services ─────────────────────────────────────────────────────

    [Fact]
    public async Task Collects_pages_from_multiple_content_services()
    {
        var service1 = new StubContentService(
        [
            new PageToGenerate("/about", "about/index.html"),
        ]);

        var service2 = new StubContentService(
        [
            new PageToGenerate("/contact", "contact/index.html"),
        ]);

        var sp = BuildServiceProvider(service1, service2);
        var sut = new FlatFileRedirectContentService(sp);

        var result = await sut.GetContentToCreateAsync();

        result.Count.ShouldBe(2);
        result.ShouldContain(c => c.TargetPath.Value == "about.html");
        result.ShouldContain(c => c.TargetPath.Value == "contact.html");
    }

    // ── Self-filtering ────────────────────────────────────────────────────────

    [Fact]
    public async Task Does_not_recurse_into_own_instances()
    {
        var stub = new StubContentService(
        [
            new PageToGenerate("/about", "about/index.html"),
        ]);

        var sp = BuildServiceProvider(stub);
        var sut = new FlatFileRedirectContentService(sp);

        // Should complete without stack overflow
        var result = await sut.GetContentToCreateAsync();

        result.Count.ShouldBe(1);
    }

    // ── Other IContentService members return empty ────────────────────────────

    [Fact]
    public async Task GetPagesToGenerateAsync_returns_empty()
    {
        var sp = BuildServiceProvider();
        var sut = new FlatFileRedirectContentService(sp);

        var result = await sut.GetPagesToGenerateAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetContentTocEntriesAsync_returns_empty()
    {
        var sp = BuildServiceProvider();
        var sut = new FlatFileRedirectContentService(sp);

        var result = await sut.GetContentTocEntriesAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetContentToCopyAsync_returns_empty()
    {
        var sp = BuildServiceProvider();
        var sut = new FlatFileRedirectContentService(sp);

        var result = await sut.GetContentToCopyAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCrossReferencesAsync_returns_empty()
    {
        var sp = BuildServiceProvider();
        var sut = new FlatFileRedirectContentService(sp);

        var result = await sut.GetCrossReferencesAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void SearchPriority_is_zero()
    {
        var sp = BuildServiceProvider();
        var sut = new FlatFileRedirectContentService(sp);

        sut.SearchPriority.ShouldBe(0);
    }

    // ── Static helper tests ───────────────────────────────────────────────────

    [Theory]
    [InlineData("about/index.html", true)]
    [InlineData("docs/intro/index.html", true)]
    [InlineData("index.html", true)]
    [InlineData("about.html", false)]
    [InlineData("about/page.html", false)]
    [InlineData("about\\index.html", true)]
    public void IsFolderBasedOutput_identifies_folder_based_paths(string path, bool expected)
    {
        FlatFileRedirectContentService.IsFolderBasedOutput(new FilePath(path)).ShouldBe(expected);
    }

    [Theory]
    [InlineData("about/index.html", "about.html")]
    [InlineData("docs/intro/index.html", "docs/intro.html")]
    [InlineData("tags/csharp/index.html", "tags/csharp.html")]
    [InlineData("a/b/c/index.html", "a/b/c.html")]
    public void GetFlatFilePath_converts_folder_to_flat(string path, string expected)
    {
        FlatFileRedirectContentService.GetFlatFilePath(new FilePath(path)).ShouldBe(expected);
    }

    [Fact]
    public void GetFlatFilePath_returns_null_for_root_index()
    {
        FlatFileRedirectContentService.GetFlatFilePath(new FilePath("index.html")).ShouldBeNull();
    }

    [Theory]
    [InlineData("about/index.html", "about/")]
    [InlineData("docs/intro/index.html", "intro/")]
    [InlineData("tags/csharp/index.html", "csharp/")]
    [InlineData("a/b/c/index.html", "c/")]
    public void GetRelativeRedirectUrl_extracts_last_segment(string path, string expected)
    {
        FlatFileRedirectContentService.GetRelativeRedirectUrl(new FilePath(path)).ShouldBe(expected);
    }

    // ── Stub ──────────────────────────────────────────────────────────────────

    private class StubContentService(ImmutableList<PageToGenerate> pages) : IContentService
    {
        public int SearchPriority => 1;

        public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync() =>
            Task.FromResult(pages);

        public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() =>
            Task.FromResult(ImmutableList<ContentTocItem>.Empty);

        public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() =>
            Task.FromResult(ImmutableList<ContentToCopy>.Empty);

        public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() =>
            Task.FromResult(ImmutableList<CrossReference>.Empty);
    }
}
