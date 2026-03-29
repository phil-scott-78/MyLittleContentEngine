using MyLittleContentEngine.Services;
using MyLittleContentEngine.Services.Content;
using Shouldly;
using Testably.Abstractions.Testing;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Task = System.Threading.Tasks.Task;

namespace MyLittleContentEngine.Tests.Content;

public class RedirectContentServiceTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly RedirectContentService _service;
    private readonly FilePath _contentPath;

    public RedirectContentServiceTests()
    {
        _fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        _contentPath = new FilePath("/content");

        var engineOptions = new ContentEngineOptions
        {
            SiteTitle = "Test Site",
            SiteDescription = "Test Description",
            IndexPageHtml = "index.html",
            ContentRootPath = _contentPath,
            FrontMatterDeserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithCaseInsensitivePropertyMatching()
                .IgnoreUnmatchedProperties()
                .Build()
        };

        var filePathOps = new FilePathOperations(_fileSystem);

        _service = new RedirectContentService(
            engineOptions,
            _fileSystem,
            filePathOps);
    }

    // ── GetContentToCreateAsync ────────────────────────────────────────────────
    // Now always returns empty; redirect HTML is written by OutputGenerationService
    // when it intercepts the 301 responses from ContentEngineRedirectMiddleware.

    [Fact]
    public async Task GetContentToCreateAsync_NoRedirectsFile_ReturnsEmpty()
    {
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);

        var result = await _service.GetContentToCreateAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetContentToCreateAsync_EmptyYamlFile_ReturnsEmpty()
    {
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", "");

        var result = await _service.GetContentToCreateAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetContentToCreateAsync_ValidRedirects_ReturnsEmpty()
    {
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
              /old-page: /new-page
              /archived: https://archive.example.com
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        var result = await _service.GetContentToCreateAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetContentToCreateAsync_InvalidYamlSyntax_ReturnsEmpty()
    {
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var invalidYaml = """
            redirects:
              invalid: yaml: syntax: here
              unclosed: "string
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", invalidYaml);

        var result = await _service.GetContentToCreateAsync();

        result.ShouldBeEmpty();
    }

    // ── GetPagesToGenerateAsync ────────────────────────────────────────────────
    // Returns a PageToGenerate per redirect source so the static generator
    // crawls those URLs and captures the 301 responses.

    [Fact]
    public async Task GetPagesToGenerateAsync_NoRedirectsFile_ReturnsEmpty()
    {
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);

        var result = await _service.GetPagesToGenerateAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPagesToGenerateAsync_EmptyYamlFile_ReturnsEmpty()
    {
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", "");

        var result = await _service.GetPagesToGenerateAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPagesToGenerateAsync_ValidRedirects_ReturnsPagesForEachSource()
    {
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
              /old-page: /new-page
              /archived: https://archive.example.com
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        var result = await _service.GetPagesToGenerateAsync();

        result.Count.ShouldBe(2);

        var oldPage = result.FirstOrDefault(p => p.Url.Value == "/old-page");
        oldPage.ShouldNotBeNull();
        oldPage.OutputFile.Value.ShouldBe("old-page.html");

        var archived = result.FirstOrDefault(p => p.Url.Value == "/archived");
        archived.ShouldNotBeNull();
        archived.OutputFile.Value.ShouldBe("archived.html");
    }

    [Fact]
    public async Task GetPagesToGenerateAsync_PathWithoutLeadingSlash_NormalizesPath()
    {
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
              about: /about-us
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        var result = await _service.GetPagesToGenerateAsync();

        result.Count.ShouldBe(1);
        result[0].Url.Value.ShouldBe("/about");
        result[0].OutputFile.Value.ShouldBe("about.html");
    }

    [Fact]
    public async Task GetPagesToGenerateAsync_NestedPath_CreatesCorrectOutputFile()
    {
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
              /docs/old-guide: /docs/new-guide
              api/v1/users: /api/v2/users
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        var result = await _service.GetPagesToGenerateAsync();

        result.Count.ShouldBe(2);

        var docsPage = result.FirstOrDefault(p => p.Url.Value == "/docs/old-guide");
        docsPage.ShouldNotBeNull();
        docsPage.OutputFile.Value.ShouldBe("docs/old-guide.html");

        var apiPage = result.FirstOrDefault(p => p.Url.Value == "/api/v1/users");
        apiPage.ShouldNotBeNull();
        apiPage.OutputFile.Value.ShouldBe("api/v1/users.html");
    }

    [Fact]
    public async Task GetPagesToGenerateAsync_EmptySourcePath_SkipsEntry()
    {
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
              "": /target
              /valid: /destination
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        var result = await _service.GetPagesToGenerateAsync();

        result.Count.ShouldBe(1);
        result[0].Url.Value.ShouldBe("/valid");
    }

    [Fact]
    public async Task GetPagesToGenerateAsync_EmptyTargetUrl_SkipsEntry()
    {
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
              /source: ""
              /valid: /destination
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        var result = await _service.GetPagesToGenerateAsync();

        result.Count.ShouldBe(1);
        result[0].Url.Value.ShouldBe("/valid");
    }

    [Fact]
    public async Task GetPagesToGenerateAsync_InvalidYamlSyntax_ReturnsEmpty()
    {
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var invalidYaml = """
            redirects:
              invalid: yaml: syntax: here
              unclosed: "string
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", invalidYaml);

        var result = await _service.GetPagesToGenerateAsync();

        result.ShouldBeEmpty();
    }

    // ── GetRedirectMappingsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetRedirectMappingsAsync_ValidRedirects_ReturnsMapping()
    {
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
              /old-page: /new-page
              /external: https://example.com
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        var result = await _service.GetRedirectMappingsAsync();

        result.Count.ShouldBe(2);
        result["/old-page"].ShouldBe("/new-page");
        result["/external"].ShouldBe("https://example.com");
    }

    [Fact]
    public async Task GetRedirectMappingsAsync_NoFile_ReturnsEmpty()
    {
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);

        var result = await _service.GetRedirectMappingsAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRedirectMappingsAsync_PathWithoutLeadingSlash_NormalizesKey()
    {
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
              about: /about-us
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        var result = await _service.GetRedirectMappingsAsync();

        result.ContainsKey("/about").ShouldBeTrue();
        result["/about"].ShouldBe("/about-us");
    }

    // ── Other IContentService members ─────────────────────────────────────────

    [Fact]
    public async Task GetContentTocEntriesAsync_ReturnsEmpty()
    {
        var result = await _service.GetContentTocEntriesAsync();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetContentToCopyAsync_ReturnsEmpty()
    {
        var result = await _service.GetContentToCopyAsync();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCrossReferencesAsync_ReturnsEmpty()
    {
        var result = await _service.GetCrossReferencesAsync();
        result.ShouldBeEmpty();
    }

    [Fact]
    public void SearchPriority_IsZero()
    {
        _service.SearchPriority.ShouldBe(0);
    }
}