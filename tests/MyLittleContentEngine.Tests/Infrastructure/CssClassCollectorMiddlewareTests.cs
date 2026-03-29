using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using MyLittleContentEngine.MonorailCss;
using Shouldly;

namespace MyLittleContentEngine.Tests.Infrastructure;

[Collection("CssClassCollector")]
public class CssClassCollectorMiddlewareTests : IDisposable
{
    public CssClassCollectorMiddlewareTests()
    {
        // CssClassCollector uses a static HashSet — clear between tests.
        CssClassCollector.ClearCache(null);
    }

    public void Dispose() => CssClassCollector.ClearCache(null);

    [Fact]
    public async Task HtmlResponse_ExtractsClasses()
    {
        var collector = new CssClassCollector();
        var middleware = CreateMiddleware(async context =>
        {
            await WriteResponse(context, "text/html",
                """<div class="prose dark:prose-invert"><h1 class="text-2xl font-bold">Hello</h1></div>""");
        });

        var httpContext = CreateHttpContext();
        await middleware.Invoke(httpContext, collector, NullLogger<CssClassCollectorMiddleware>.Instance);

        var classes = collector.GetClasses();
        classes.ShouldContain("prose");
        classes.ShouldContain("dark:prose-invert");
        classes.ShouldContain("text-2xl");
        classes.ShouldContain("font-bold");
    }

    [Fact]
    public async Task JsonResponse_ExtractsClassesFromBackslashEscapedHtml()
    {
        var collector = new CssClassCollector();
        // Simulate JSON with \" escaping (standard JSON escape for double quotes)
        var json = """{"htmlContent":"<div class=\"prose dark:text-base-300\"><h1 class=\"font-bold\">Title</h1></div>"}""";

        var middleware = CreateMiddleware(async context =>
        {
            await WriteResponse(context, "application/json", json);
        });

        var httpContext = CreateHttpContext();
        await middleware.Invoke(httpContext, collector, NullLogger<CssClassCollectorMiddleware>.Instance);

        var classes = collector.GetClasses();
        classes.ShouldContain("prose");
        classes.ShouldContain("dark:text-base-300");
        classes.ShouldContain("font-bold");
    }

    [Fact]
    public async Task JsonResponse_ExtractsClassesFromUnicodeEscapedHtml()
    {
        var collector = new CssClassCollector();
        // Simulate JSON with \u0022 escaping (JavaScriptEncoder.Default encodes " as \u0022)
        // and \u003C/\u003E for < and > (HTML-sensitive characters)
        var json = """{"htmlContent":"\u003Cdiv class=\u0022prose dark:text-base-300\u0022\u003E\u003Ch1 class=\u0022font-bold\u0022\u003ETitle\u003C/h1\u003E\u003C/div\u003E"}""";

        var middleware = CreateMiddleware(async context =>
        {
            await WriteResponse(context, "application/json", json);
        });

        var httpContext = CreateHttpContext();
        await middleware.Invoke(httpContext, collector, NullLogger<CssClassCollectorMiddleware>.Instance);

        var classes = collector.GetClasses();
        classes.ShouldContain("prose");
        classes.ShouldContain("dark:text-base-300");
        classes.ShouldContain("font-bold");
    }

    [Fact]
    public async Task JsonResponse_DoesNotModifyResponseBody()
    {
        var collector = new CssClassCollector();
        var json = """{"htmlContent":"<div class=\"prose\">content</div>"}""";

        var middleware = CreateMiddleware(async context =>
        {
            await WriteResponse(context, "application/json", json);
        });

        var httpContext = CreateHttpContext();
        await middleware.Invoke(httpContext, collector, NullLogger<CssClassCollectorMiddleware>.Instance);

        var responseContent = await ReadResponseContent(httpContext);
        responseContent.ShouldBe(json);
    }

    [Fact]
    public async Task NonHtmlNonJsonResponse_SkipsExtraction()
    {
        var collector = new CssClassCollector();
        var middleware = CreateMiddleware(async context =>
        {
            await WriteResponse(context, "text/css",
                """.prose { color: red; } .font-bold { font-weight: 700; }""");
        });

        var httpContext = CreateHttpContext();
        await middleware.Invoke(httpContext, collector, NullLogger<CssClassCollectorMiddleware>.Instance);

        collector.GetClasses().ShouldBeEmpty();
    }

    [Fact]
    public async Task NullContentType_SkipsExtraction()
    {
        var collector = new CssClassCollector();
        var middleware = CreateMiddleware(async context =>
        {
            var bytes = Encoding.UTF8.GetBytes("""<div class="prose">content</div>""");
            await context.Response.Body.WriteAsync(bytes);
        });

        var httpContext = CreateHttpContext();
        await middleware.Invoke(httpContext, collector, NullLogger<CssClassCollectorMiddleware>.Instance);

        collector.GetClasses().ShouldBeEmpty();
    }

    private static CssClassCollectorMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new CssClassCollectorMiddleware(next);
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task WriteResponse(HttpContext context, string contentType, string content)
    {
        context.Response.ContentType = contentType;
        context.Response.StatusCode = 200;
        var bytes = Encoding.UTF8.GetBytes(content);
        await context.Response.Body.WriteAsync(bytes);
    }

    private static async Task<string> ReadResponseContent(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}

[Collection("CssClassCollector")]
public class ContentFileScannerTests : IDisposable
{
    public ContentFileScannerTests() => CssClassCollector.ClearCache(null);
    public void Dispose() => CssClassCollector.ClearCache(null);
    [Fact]
    public void ExtractPotentialClasses_FromHtmlClassAttributes()
    {
        var content = """<div class="prose dark:prose-invert max-w-full">Hello</div>""";
        var classes = MonorailServiceExtensions.ExtractPotentialClasses(content);

        classes.ShouldContain("prose");
        classes.ShouldContain("dark:prose-invert");
        classes.ShouldContain("max-w-full");
    }

    [Fact]
    public void ExtractPotentialClasses_FromJsStringConstants()
    {
        var content = """
            const PROSE_CLASSES = 'prose dark:prose-invert dark:text-base-300 max-w-full';
            const H1_CLASSES = 'font-display text-2xl lg:text-4xl font-bold';
            """;

        var classes = MonorailServiceExtensions.ExtractPotentialClasses(content);

        // Single-word classes like "prose" must also be captured
        classes.ShouldContain("prose");
        classes.ShouldContain("dark:prose-invert");
        classes.ShouldContain("dark:text-base-300");
        classes.ShouldContain("max-w-full");
        classes.ShouldContain("font-display");
        classes.ShouldContain("text-2xl");
        classes.ShouldContain("lg:text-4xl");
        classes.ShouldContain("font-bold");
    }

    [Fact]
    public void ExtractPotentialClasses_FromJsTemplateLiterals()
    {
        var content = """
            function build() {
                return `<main class="prose dark:prose-invert">${data.content}</main>`;
            }
            """;

        var classes = MonorailServiceExtensions.ExtractPotentialClasses(content);

        // Picked up by the class="..." regex
        classes.ShouldContain("prose");
        classes.ShouldContain("dark:prose-invert");
    }

    [Fact]
    public void ExtractPotentialClasses_EmptyContent_ReturnsEmpty()
    {
        MonorailServiceExtensions.ExtractPotentialClasses("").ShouldBeEmpty();
    }

    [Fact]
    public void ScanContentFiles_ReadsFromFileProvider()
    {
        var collector = new CssClassCollector();
        var jsContent = """const CLASSES = 'flex-col dark:bg-base-800 hover:text-primary-500';""";

        var fileProvider = new TestFileProvider(new Dictionary<string, string>
        {
            ["spa-nav.js"] = jsContent
        });

        MonorailServiceExtensions.ScanContentFiles(collector, fileProvider, ["spa-nav.js"]);

        var classes = collector.GetClasses();
        classes.ShouldContain("flex-col");
        classes.ShouldContain("dark:bg-base-800");
        classes.ShouldContain("hover:text-primary-500");
    }

    [Fact]
    public void ScanContentFiles_MissingFile_SkipsGracefully()
    {
        var collector = new CssClassCollector();
        var fileProvider = new TestFileProvider(new Dictionary<string, string>());

        MonorailServiceExtensions.ScanContentFiles(collector, fileProvider, ["nonexistent.js"]);

        collector.GetClasses().ShouldBeEmpty();
    }

    /// <summary>
    /// Simple in-memory file provider for testing.
    /// </summary>
    private class TestFileProvider(Dictionary<string, string> files) : IFileProvider
    {
        public IFileInfo GetFileInfo(string subpath)
        {
            if (files.TryGetValue(subpath, out var content))
                return new TestFileInfo(subpath, content);

            return new NotFoundFileInfo(subpath);
        }

        public IDirectoryContents GetDirectoryContents(string subpath) =>
            throw new NotImplementedException();

        public IChangeToken Watch(string filter) =>
            throw new NotImplementedException();
    }

    private class TestFileInfo(string name, string content) : IFileInfo
    {
        public bool Exists => true;
        public long Length => Encoding.UTF8.GetByteCount(content);
        public string? PhysicalPath => null;
        public string Name => name;
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public bool IsDirectory => false;

        public Stream CreateReadStream() => new MemoryStream(Encoding.UTF8.GetBytes(content));
    }
}
