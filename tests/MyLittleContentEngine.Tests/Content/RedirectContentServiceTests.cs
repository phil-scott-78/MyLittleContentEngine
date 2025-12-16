using System.Collections.Immutable;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Testably.Abstractions.Testing;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Tests.TestHelpers;
using Shouldly;
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
            filePathOps,
            engineOptions,
            new OutputOptions(),
            new NullLogger<RedirectContentService>()); // OutputOptions
    }

    [Fact]
    public async Task GetContentToCreateAsync_NoRedirectsFile_ReturnsEmpty()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);

        // Act
        var result = await _service.GetContentToCreateAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetContentToCreateAsync_EmptyYamlFile_ReturnsEmpty()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", "");

        // Act
        var result = await _service.GetContentToCreateAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetContentToCreateAsync_WhitespaceOnlyYaml_ReturnsEmpty()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", "   \n\n   ");

        // Act
        var result = await _service.GetContentToCreateAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetContentToCreateAsync_NoRedirectsKey_ReturnsEmpty()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            other_key: value
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        // Act
        var result = await _service.GetContentToCreateAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetContentToCreateAsync_EmptyRedirects_ReturnsEmpty()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        // Act
        var result = await _service.GetContentToCreateAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetContentToCreateAsync_ValidRedirects_ReturnsContentToCreate()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
              /old-page: /new-page
              /archived: https://archive.example.com
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        // Act
        var result = await _service.GetContentToCreateAsync();

        // Assert
        result.Count.ShouldBe(2);

        var firstRedirect = result.First(c => c.TargetPath.Value == "old-page.html");
        firstRedirect.ShouldNotBeNull();
        var htmlContent = Encoding.UTF8.GetString(firstRedirect.Bytes);
        htmlContent.ShouldContain("/new-page");
        htmlContent.ShouldContain("meta http-equiv=\"refresh\"");
        htmlContent.ShouldContain("<!DOCTYPE html>");

        var secondRedirect = result.First(c => c.TargetPath.Value == "archived.html");
        secondRedirect.ShouldNotBeNull();
        var html2 = Encoding.UTF8.GetString(secondRedirect.Bytes);
        html2.ShouldContain("https://archive.example.com");
        html2.ShouldContain("meta http-equiv=\"refresh\"");
    }

    [Fact]
    public async Task GetContentToCreateAsync_PathWithoutLeadingSlash_CreatesCorrectFile()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
              about: /about-us
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        // Act
        var result = await _service.GetContentToCreateAsync();

        // Assert
        result.Count.ShouldBe(1);
        result[0].TargetPath.Value.ShouldBe("about.html");
        var htmlContent = Encoding.UTF8.GetString(result[0].Bytes);
        htmlContent.ShouldContain("/about-us");
    }

    [Fact]
    public async Task GetContentToCreateAsync_NestedPath_CreatesCorrectFile()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
              /docs/old-guide: /docs/new-guide
              api/v1/users: /api/v2/users
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        // Act
        var result = await _service.GetContentToCreateAsync();

        // Assert
        result.Count.ShouldBe(2);

        var docsRedirect = result.First(c => c.TargetPath.Value == "docs/old-guide.html");
        docsRedirect.ShouldNotBeNull();
        var htmlContent = Encoding.UTF8.GetString(docsRedirect.Bytes);
        htmlContent.ShouldContain("/docs/new-guide");

        var apiRedirect = result.First(c => c.TargetPath.Value == "api/v1/users.html");
        apiRedirect.ShouldNotBeNull();
        var html2 = Encoding.UTF8.GetString(apiRedirect.Bytes);
        html2.ShouldContain("/api/v2/users");
    }

    [Fact]
    public async Task GetContentToCreateAsync_RelativeTargetUrl_GeneratesCorrectHtml()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
              /old: /new
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        // Act
        var result = await _service.GetContentToCreateAsync();

        // Assert
        result.Count.ShouldBe(1);
        var htmlContent = Encoding.UTF8.GetString(result[0].Bytes);
        htmlContent.ShouldContain("URL='/new'");
        htmlContent.ShouldContain("<a href=\"/new\">");
    }

    [Fact]
    public async Task GetContentToCreateAsync_AbsoluteTargetUrl_GeneratesCorrectHtml()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
              /external: https://example.com/page
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        // Act
        var result = await _service.GetContentToCreateAsync();

        // Assert
        result.Count.ShouldBe(1);
        var htmlContent = Encoding.UTF8.GetString(result[0].Bytes);
        htmlContent.ShouldContain("URL='https://example.com/page'");
        htmlContent.ShouldContain("<a href=\"https://example.com/page\">");
    }

    [Fact]
    public async Task GetContentToCreateAsync_InvalidYamlSyntax_ReturnsEmpty()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var invalidYaml = """
            redirects:
              invalid: yaml: syntax: here
              unclosed: "string
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", invalidYaml);

        // Act
        var result = await _service.GetContentToCreateAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetContentToCreateAsync_EmptySourcePath_SkipsEntry()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
              "": /target
              /valid: /destination
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        // Act
        var result = await _service.GetContentToCreateAsync();

        // Assert
        result.Count.ShouldBe(1);
        result[0].TargetPath.Value.ShouldBe("valid.html");
    }

    [Fact]
    public async Task GetContentToCreateAsync_EmptyTargetUrl_SkipsEntry()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
              /source: ""
              /valid: /destination
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        // Act
        var result = await _service.GetContentToCreateAsync();

        // Assert
        result.Count.ShouldBe(1);
        result[0].TargetPath.Value.ShouldBe("valid.html");
    }

    [Fact]
    public async Task GetContentToCreateAsync_MixedValidInvalidEntries_ProcessesValidOnes()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
              /valid1: /target1
              "": /target2
              /valid2: ""
              /valid3: /target3
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        // Act
        var result = await _service.GetContentToCreateAsync();

        // Assert
        result.Count.ShouldBe(2);
        result.Any(c => c.TargetPath.Value == "valid1.html").ShouldBeTrue();
        result.Any(c => c.TargetPath.Value == "valid3.html").ShouldBeTrue();
    }

    [Fact]
    public async Task GetContentToCreateAsync_HtmlContentIsUtf8Encoded()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_contentPath.Value);
        var yamlContent = """
            redirects:
              /test: /target
            """;
        _fileSystem.File.WriteAllText($"{_contentPath.Value}/_redirects.yml", yamlContent);

        // Act
        var result = await _service.GetContentToCreateAsync();

        // Assert
        result.Count.ShouldBe(1);

        // Verify it's valid UTF-8 by decoding and checking
        var htmlContent = Encoding.UTF8.GetString(result[0].Bytes);
        htmlContent.ShouldContain("<meta charset=\"utf-8\">");
    }

    [Fact]
    public async Task GetPagesToGenerateAsync_ReturnsEmpty()
    {
        // Act
        var result = await _service.GetPagesToGenerateAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetContentTocEntriesAsync_ReturnsEmpty()
    {
        // Act
        var result = await _service.GetContentTocEntriesAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetContentToCopyAsync_ReturnsEmpty()
    {
        // Act
        var result = await _service.GetContentToCopyAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCrossReferencesAsync_ReturnsEmpty()
    {
        // Act
        var result = await _service.GetCrossReferencesAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void SearchPriority_IsZero()
    {
        // Act & Assert
        _service.SearchPriority.ShouldBe(0);
    }

    [Fact]
    public async Task GetContentToCreateAsync_WithBaseUrl_PrependsBaseUrlToRelativeRedirects()
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        var contentPath = new FilePath("/content");
        fileSystem.Directory.CreateDirectory(contentPath.Value);

        var yamlContent = """
            redirects:
              /old-page: /new-page
              /docs/old: /docs/new
            """;
        fileSystem.File.WriteAllText($"{contentPath.Value}/_redirects.yml", yamlContent);

        var engineOptions = new ContentEngineOptions
        {
            SiteTitle = "Test Site",
            SiteDescription = "Test Description",
            ContentRootPath = contentPath,
            FrontMatterDeserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithCaseInsensitivePropertyMatching()
                .IgnoreUnmatchedProperties()
                .Build()
        };

        var outputOptions = new OutputOptions { BaseUrl = "/website/" };
        var service = new RedirectContentService(
            engineOptions,
            fileSystem,
            new FilePathOperations(fileSystem),
            engineOptions,
            outputOptions,
            new NullLogger<RedirectContentService>());

        // Act
        var result = await service.GetContentToCreateAsync();

        // Assert
        result.Count.ShouldBe(2);

        // Check that internal redirect has BaseUrl prepended
        var firstRedirect = result.First(c => c.TargetPath.Value == "old-page.html");
        var htmlContent = Encoding.UTF8.GetString(firstRedirect.Bytes);
        htmlContent.ShouldContain("/website/new-page");
        htmlContent.ShouldContain("URL='/website/new-page'");
        htmlContent.ShouldContain("<a href=\"/website/new-page\">");

        var secondRedirect = result.First(c => c.TargetPath.Value == "docs/old.html");
        var html2 = Encoding.UTF8.GetString(secondRedirect.Bytes);
        html2.ShouldContain("/website/docs/new");
    }

    [Fact]
    public async Task GetContentToCreateAsync_WithBaseUrl_DoesNotModifyExternalUrls()
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        var contentPath = new FilePath("/content");
        fileSystem.Directory.CreateDirectory(contentPath.Value);

        var yamlContent = """
            redirects:
              /external: https://example.com/page
              /http-url: http://example.org/other
            """;
        fileSystem.File.WriteAllText($"{contentPath.Value}/_redirects.yml", yamlContent);

        var engineOptions = new ContentEngineOptions
        {
            SiteTitle = "Test Site",
            SiteDescription = "Test Description",
            ContentRootPath = contentPath,
            FrontMatterDeserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithCaseInsensitivePropertyMatching()
                .IgnoreUnmatchedProperties()
                .Build()
        };

        var outputOptions = new OutputOptions { BaseUrl = "/website/" };
        var service = new RedirectContentService(
            engineOptions,
            fileSystem,
            new FilePathOperations(fileSystem),
            engineOptions,
            outputOptions,
            new NullLogger<RedirectContentService>());

        // Act
        var result = await service.GetContentToCreateAsync();

        // Assert
        result.Count.ShouldBe(2);

        // External URLs should remain unchanged
        var httpsRedirect = result.First(c => c.TargetPath.Value == "external.html");
        var htmlContent = Encoding.UTF8.GetString(httpsRedirect.Bytes);
        htmlContent.ShouldContain("https://example.com/page");
        htmlContent.ShouldNotContain("/website/https:");

        var httpRedirect = result.First(c => c.TargetPath.Value == "http-url.html");
        var html2 = Encoding.UTF8.GetString(httpRedirect.Bytes);
        html2.ShouldContain("http://example.org/other");
        html2.ShouldNotContain("/website/http:");
    }

    [Fact]
    public async Task GetContentToCreateAsync_WithBaseUrl_MixedRelativeAndExternal()
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        var contentPath = new FilePath("/content");
        fileSystem.Directory.CreateDirectory(contentPath.Value);

        var yamlContent = """
            redirects:
              /internal: /new-internal
              /external: https://example.com
            """;
        fileSystem.File.WriteAllText($"{contentPath.Value}/_redirects.yml", yamlContent);

        var engineOptions = new ContentEngineOptions
        {
            SiteTitle = "Test Site",
            SiteDescription = "Test Description",
            ContentRootPath = contentPath,
            FrontMatterDeserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithCaseInsensitivePropertyMatching()
                .IgnoreUnmatchedProperties()
                .Build()
        };

        var outputOptions = new OutputOptions { BaseUrl = "/my-app" };
        var service = new RedirectContentService(
            engineOptions,
            fileSystem,
            new FilePathOperations(fileSystem),
            engineOptions,
            outputOptions,
            new NullLogger<RedirectContentService>());

        // Act
        var result = await service.GetContentToCreateAsync();

        // Assert
        result.Count.ShouldBe(2);

        var internalRedirect = result.First(c => c.TargetPath.Value == "internal.html");
        var internalHtml = Encoding.UTF8.GetString(internalRedirect.Bytes);
        internalHtml.ShouldContain("/my-app/new-internal");

        var externalRedirect = result.First(c => c.TargetPath.Value == "external.html");
        var externalHtml = Encoding.UTF8.GetString(externalRedirect.Bytes);
        externalHtml.ShouldContain("https://example.com");
        externalHtml.ShouldNotContain("/my-app/https:");
    }
}
