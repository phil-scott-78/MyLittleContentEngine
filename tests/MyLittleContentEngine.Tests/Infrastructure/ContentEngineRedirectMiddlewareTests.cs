using Microsoft.AspNetCore.Http;
using MyLittleContentEngine.Services;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Infrastructure;
using Shouldly;
using Testably.Abstractions.Testing;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Task = System.Threading.Tasks.Task;

namespace MyLittleContentEngine.Tests.Infrastructure;

public class ContentEngineRedirectMiddlewareTests
{
    private static RedirectContentService CreateService(MockFileSystem fileSystem, string contentPath, string yamlContent)
    {
        fileSystem.Directory.CreateDirectory(contentPath);
        fileSystem.File.WriteAllText($"{contentPath}/_redirects.yml", yamlContent);

        var options = new ContentEngineOptions
        {
            SiteTitle = "Test",
            SiteDescription = "Test",
            ContentRootPath = new FilePath(contentPath),
            FrontMatterDeserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithCaseInsensitivePropertyMatching()
                .IgnoreUnmatchedProperties()
                .Build()
        };

        return new RedirectContentService(options, fileSystem, new FilePathOperations(fileSystem));
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact]
    public async Task InvokeAsync_KnownPath_Returns301WithLocation()
    {
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        var service = CreateService(fileSystem, "/content", """
            redirects:
              /old-page: /new-page
            """);

        var nextCalled = false;
        var middleware = new ContentEngineRedirectMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateContext("/old-page");

        await middleware.InvokeAsync(context, service, new OutputOptions());

        context.Response.StatusCode.ShouldBe(301);
        context.Response.Headers.Location.ToString().ShouldBe("/new-page");
        nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task InvokeAsync_UnknownPath_CallsNext()
    {
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        var service = CreateService(fileSystem, "/content", """
            redirects:
              /old-page: /new-page
            """);

        var nextCalled = false;
        var middleware = new ContentEngineRedirectMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateContext("/some-other-page");

        await middleware.InvokeAsync(context, service, new OutputOptions());

        nextCalled.ShouldBeTrue();
        context.Response.StatusCode.ShouldNotBe(301);
    }

    [Fact]
    public async Task InvokeAsync_TrailingSlashOnRequest_NormalizesAndMatches()
    {
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        var service = CreateService(fileSystem, "/content", """
            redirects:
              /old-page: /new-page
            """);

        var middleware = new ContentEngineRedirectMiddleware(_ => Task.CompletedTask);
        var context = CreateContext("/old-page/");

        await middleware.InvokeAsync(context, service, new OutputOptions());

        context.Response.StatusCode.ShouldBe(301);
        context.Response.Headers.Location.ToString().ShouldBe("/new-page");
    }

    [Fact]
    public async Task InvokeAsync_ExternalTarget_PreservesExternalUrl()
    {
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        var service = CreateService(fileSystem, "/content", """
            redirects:
              /external: https://example.com/page
            """);

        var middleware = new ContentEngineRedirectMiddleware(_ => Task.CompletedTask);
        var context = CreateContext("/external");

        await middleware.InvokeAsync(context, service, new OutputOptions());

        context.Response.StatusCode.ShouldBe(301);
        context.Response.Headers.Location.ToString().ShouldBe("https://example.com/page");
    }

    [Fact]
    public async Task InvokeAsync_WithBaseUrl_PrependsBaseUrlToInternalTarget()
    {
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        var service = CreateService(fileSystem, "/content", """
            redirects:
              /old-page: /new-page
            """);

        var middleware = new ContentEngineRedirectMiddleware(_ => Task.CompletedTask);
        var context = CreateContext("/old-page");
        var outputOptions = new OutputOptions { BaseUrl = "/website" };

        await middleware.InvokeAsync(context, service, outputOptions);

        context.Response.StatusCode.ShouldBe(301);
        context.Response.Headers.Location.ToString().ShouldBe("/website/new-page");
    }

    [Fact]
    public async Task InvokeAsync_WithBaseUrl_DoesNotModifyExternalTarget()
    {
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        var service = CreateService(fileSystem, "/content", """
            redirects:
              /external: https://example.com
            """);

        var middleware = new ContentEngineRedirectMiddleware(_ => Task.CompletedTask);
        var context = CreateContext("/external");
        var outputOptions = new OutputOptions { BaseUrl = "/website" };

        await middleware.InvokeAsync(context, service, outputOptions);

        context.Response.StatusCode.ShouldBe(301);
        context.Response.Headers.Location.ToString().ShouldBe("https://example.com");
    }

    [Fact]
    public async Task InvokeAsync_EmptyRedirectMap_CallsNext()
    {
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        fileSystem.Directory.CreateDirectory("/content");

        var options = new ContentEngineOptions
        {
            SiteTitle = "Test",
            SiteDescription = "Test",
            ContentRootPath = new FilePath("/content"),
            FrontMatterDeserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithCaseInsensitivePropertyMatching()
                .IgnoreUnmatchedProperties()
                .Build()
        };
        var service = new RedirectContentService(options, fileSystem, new FilePathOperations(fileSystem));

        var nextCalled = false;
        var middleware = new ContentEngineRedirectMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateContext("/any-path");

        await middleware.InvokeAsync(context, service, new OutputOptions());

        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_RootPath_MatchesCorrectly()
    {
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
        var service = CreateService(fileSystem, "/content", """
            redirects:
              /: /home
            """);

        var middleware = new ContentEngineRedirectMiddleware(_ => Task.CompletedTask);
        var context = CreateContext("/");

        await middleware.InvokeAsync(context, service, new OutputOptions());

        context.Response.StatusCode.ShouldBe(301);
        context.Response.Headers.Location.ToString().ShouldBe("/home");
    }
}