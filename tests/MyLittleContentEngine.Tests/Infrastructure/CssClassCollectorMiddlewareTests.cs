using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using MyLittleContentEngine.MonorailCss;
using MyLittleContentEngine.Services.Infrastructure;
using Shouldly;

namespace MyLittleContentEngine.Tests.Infrastructure;

[Collection("CssClassCollector")]
public class CssClassCollectorProcessorTests : IDisposable
{
    public CssClassCollectorProcessorTests() => CssClassCollector.ClearCache(null);
    public void Dispose() => CssClassCollector.ClearCache(null);

    [Fact]
    public async Task HtmlResponse_ExtractsClasses()
    {
        var (processor, collector) = CreateProcessor();
        var context = CreateHttpContext("text/html");

        await processor.ProcessAsync(
            """<div class="prose dark:prose-invert"><h1 class="text-2xl font-bold">Hello</h1></div>""",
            context);

        var classes = collector.GetClasses();
        classes.ShouldContain("prose");
        classes.ShouldContain("dark:prose-invert");
        classes.ShouldContain("text-2xl");
        classes.ShouldContain("font-bold");
    }

    [Fact]
    public async Task JsonResponse_ExtractsClassesFromBackslashEscapedHtml()
    {
        var (processor, collector) = CreateProcessor();
        var context = CreateHttpContext("application/json");
        var json = """{"htmlContent":"<div class=\"prose dark:text-base-300\"><h1 class=\"font-bold\">Title</h1></div>"}""";

        await processor.ProcessAsync(json, context);

        var classes = collector.GetClasses();
        classes.ShouldContain("prose");
        classes.ShouldContain("dark:text-base-300");
        classes.ShouldContain("font-bold");
    }

    [Fact]
    public async Task JsonResponse_ExtractsClassesFromUnicodeEscapedHtml()
    {
        var (processor, collector) = CreateProcessor();
        var context = CreateHttpContext("application/json");
        var json = """{"htmlContent":"\u003Cdiv class=\u0022prose dark:text-base-300\u0022\u003E\u003Ch1 class=\u0022font-bold\u0022\u003ETitle\u003C/h1\u003E\u003C/div\u003E"}""";

        await processor.ProcessAsync(json, context);

        var classes = collector.GetClasses();
        classes.ShouldContain("prose");
        classes.ShouldContain("dark:text-base-300");
        classes.ShouldContain("font-bold");
    }

    [Fact]
    public async Task ProcessAsync_ReturnsBodyUnchanged()
    {
        var (processor, _) = CreateProcessor();
        var context = CreateHttpContext("application/json");
        var json = """{"htmlContent":"<div class=\"prose\">content</div>"}""";

        var result = await processor.ProcessAsync(json, context);

        result.ShouldBe(json);
    }

    [Fact]
    public void ShouldProcess_FalseForNonHtmlNonJson()
    {
        var (processor, _) = CreateProcessor();
        var context = CreateHttpContext("text/css");

        processor.ShouldProcess(context).ShouldBeFalse();
    }

    [Fact]
    public void ShouldProcess_FalseForNullContentType()
    {
        var (processor, _) = CreateProcessor();
        var context = new DefaultHttpContext();

        processor.ShouldProcess(context).ShouldBeFalse();
    }

    private static (IResponseProcessor Processor, CssClassCollector Collector) CreateProcessor()
    {
        var collector = new CssClassCollector();
        var processor = (IResponseProcessor)new CssClassCollectorProcessor(
            collector,
            NullLogger<CssClassCollectorProcessor>.Instance);
        return (processor, collector);
    }

    private static HttpContext CreateHttpContext(string contentType)
    {
        var context = new DefaultHttpContext();
        context.Response.ContentType = contentType;
        return context;
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
