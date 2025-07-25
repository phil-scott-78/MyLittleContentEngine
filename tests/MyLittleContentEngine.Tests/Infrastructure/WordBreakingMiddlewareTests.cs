using System.Text;
using Microsoft.AspNetCore.Http;
using MyLittleContentEngine.Services.Infrastructure;
using Shouldly;
using Xunit;

namespace MyLittleContentEngine.Tests.Infrastructure;

public class WordBreakingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithLongIdentifierInH1_ShouldInsertWordBreaks()
    {
        // Arrange
        var options = CreateOptions();
        var context = CreateHttpContext();
        var html = """<h1 class="font-display text-xl sm:text-2xl lg:text-4xl font-bold tracking-tight text-base-900 dark:text-base-50">MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting</h1>""";
        var expectedHtml = """<h1 class="font-display text-xl sm:text-2xl lg:text-4xl font-bold tracking-tight text-base-900 dark:text-base-50">MyLittleContentEngine.<wbr />Services.<wbr />Content.<wbr />MarkdigExtensions.<wbr />CodeHighlighting</h1>""";
        
        // Act
        var result = await ProcessHtmlThroughMiddleware(options, context, html);
        
        // Assert
        result.ShouldBe(expectedHtml);
    }

    [Fact]
    public async Task InvokeAsync_WithLongIdentifierInParagraph_ShouldInsertWordBreaks()
    {
        // Arrange
        var options = CreateOptions();
        var context = CreateHttpContext();
        var html = """<p>The namespace MyLittleContentEngine.Services.Content.MarkdigExtensions contains utilities.</p>""";
        var expectedHtml = """<p>The namespace MyLittleContentEngine.<wbr />Services.<wbr />Content.<wbr />MarkdigExtensions contains utilities.</p>""";
        
        // Act
        var result = await ProcessHtmlThroughMiddleware(options, context, html);
        
        // Assert
        result.ShouldBe(expectedHtml);
    }

    [Fact]
    public async Task InvokeAsync_WithLongIdentifierInScript_ShouldNotInsertWordBreaks()
    {
        // Arrange
        var options = CreateOptions();
        var context = CreateHttpContext();
        var html = """<script>var x = "MyLittleContentEngine.Services.Content.MarkdigExtensions";</script>""";
        
        // Act
        var result = await ProcessHtmlThroughMiddleware(options, context, html);
        
        // Assert
        result.ShouldBe(html); // Should remain unchanged
    }

    [Fact]
    public async Task InvokeAsync_WithLongIdentifierInStyle_ShouldNotInsertWordBreaks()
    {
        // Arrange
        var options = CreateOptions();
        var context = CreateHttpContext();
        var html = """<style>.MyLittleContentEngine-Services-Content-MarkdigExtensions { color: red; }</style>""";
        
        // Act
        var result = await ProcessHtmlThroughMiddleware(options, context, html);
        
        // Assert
        result.ShouldBe(html); // Should remain unchanged
    }

    [Fact]
    public async Task InvokeAsync_WithLongIdentifierInPre_ShouldNotInsertWordBreaks()
    {
        // Arrange
        var options = CreateOptions();
        var context = CreateHttpContext();
        var html = """<pre>MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting</pre>""";
        
        // Act
        var result = await ProcessHtmlThroughMiddleware(options, context, html);
        
        // Assert
        result.ShouldBe(html); // Should remain unchanged
    }

    [Fact]
    public async Task InvokeAsync_WithLongIdentifierInCode_ShouldNotInsertWordBreaks()
    {
        // Arrange
        var options = CreateOptions();
        var context = CreateHttpContext();
        var html = """<code>MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting</code>""";
        
        // Act
        var result = await ProcessHtmlThroughMiddleware(options, context, html);
        
        // Assert
        result.ShouldBe(html); // Should remain unchanged
    }

    [Fact]
    public async Task InvokeAsync_WithShortWords_ShouldNotInsertWordBreaks()
    {
        // Arrange
        var options = CreateOptions();
        var context = CreateHttpContext();
        var html = """<p>Short words here.</p>""";
        
        // Act
        var result = await ProcessHtmlThroughMiddleware(options, context, html);
        
        // Assert
        result.ShouldBe(html); // Should remain unchanged
    }

    [Fact]
    public async Task InvokeAsync_WithMultipleLongIdentifiers_ShouldProcessAll()
    {
        // Arrange
        var options = CreateOptions();
        var context = CreateHttpContext();
        var html = """<div><p>System.Collections.Generic.List<string></p><span>MyLittleContentEngine.Services.Content</span></div>""";
        var expectedHtml = """<div><p>System.<wbr />Collections.<wbr />Generic.<wbr />List<string></p><span>MyLittleContentEngine.<wbr />Services.<wbr />Content</span></div>""";
        
        // Act
        var result = await ProcessHtmlThroughMiddleware(options, context, html);
        
        // Assert
        result.ShouldBe(expectedHtml);
    }

    [Fact]
    public async Task InvokeAsync_WithCustomMinWordLength_ShouldRespectSetting()
    {
        // Arrange
        var options = new WordBreakingMiddlewareOptions { MinWordLength = 10 };
        var context = CreateHttpContext();
        var html = """<p>Short.Word versus System.Collections.Generic</p>""";
        var expectedHtml = """<p>Short.<wbr />Word versus System.<wbr />Collections.<wbr />Generic</p>""";
        
        // Act
        var result = await ProcessHtmlThroughMiddleware(options, context, html);
        
        // Assert
        result.ShouldBe(expectedHtml);
    }

    [Fact]
    public async Task InvokeAsync_WithDisabledMiddleware_ShouldNotProcessContent()
    {
        // Arrange
        var options = new WordBreakingMiddlewareOptions { Enabled = false };
        var context = CreateHttpContext();
        var html = """<p>MyLittleContentEngine.Services.Content.MarkdigExtensions</p>""";
        
        // Act
        var result = await ProcessHtmlThroughMiddleware(options, context, html);
        
        // Assert
        result.ShouldBe(html); // Should remain unchanged
    }

    [Fact]
    public async Task InvokeAsync_WithNonGetRequest_ShouldNotProcessContent()
    {
        // Arrange
        var options = CreateOptions();
        var context = CreateHttpContext();
        context.Request.Method = HttpMethods.Post;
        var html = """<p>MyLittleContentEngine.Services.Content.MarkdigExtensions</p>""";
        
        // Act
        var result = await ProcessHtmlThroughMiddleware(options, context, html);
        
        // Assert
        result.ShouldBe(html); // Should remain unchanged
    }

    [Fact]
    public async Task InvokeAsync_WithNonHtmlContentType_ShouldNotProcessContent()
    {
        // Arrange
        var options = CreateOptions();
        var context = CreateHttpContext();
        context.Response.ContentType = "application/json";
        var html = """{"name": "MyLittleContentEngine.Services.Content.MarkdigExtensions"}""";
        
        // Act
        var result = await ProcessHtmlThroughMiddleware(options, context, html);
        
        // Assert
        result.ShouldBe(html); // Should remain unchanged
    }

    [Fact]
    public async Task InvokeAsync_WithErrorStatusCode_ShouldNotProcessContent()
    {
        // Arrange
        var options = CreateOptions();
        var context = CreateHttpContext();
        context.Response.StatusCode = 404;
        var html = """<p>MyLittleContentEngine.Services.Content.MarkdigExtensions</p>""";
        
        // Act
        var result = await ProcessHtmlThroughMiddleware(options, context, html);
        
        // Assert
        result.ShouldBe(html); // Should remain unchanged
    }

    [Fact]
    public async Task InvokeAsync_WithNestedTags_ShouldProcessTextCorrectly()
    {
        // Arrange
        var options = CreateOptions();
        var context = CreateHttpContext();
        var html = """<div><strong>MyLittleContentEngine.Services</strong> and <em>Content.MarkdigExtensions</em></div>""";
        var expectedHtml = """<div><strong>MyLittleContentEngine.<wbr />Services</strong> and <em>Content.<wbr />MarkdigExtensions</em></div>""";
        
        // Act
        var result = await ProcessHtmlThroughMiddleware(options, context, html);
        
        // Assert
        result.ShouldBe(expectedHtml);
    }

    [Fact]
    public async Task InvokeAsync_WithComplexHtml_ShouldPreserveStructure()
    {
        // Arrange
        var options = CreateOptions();
        var context = CreateHttpContext();
        var html = """
<!DOCTYPE html>
<html>
<head>
    <title>Test</title>
    <style>.class { color: red; }</style>
</head>
<body>
    <h1>System.Collections.Generic.List<string></h1>
    <script>console.log("test");</script>
    <p>Some text with MyLittleContentEngine.Services.Content</p>
</body>
</html>
""";
        var expectedHtml = """
<!DOCTYPE html>
<html>
<head>
    <title>Test</title>
    <style>.class { color: red; }</style>
</head>
<body>
    <h1>System.<wbr />Collections.<wbr />Generic.<wbr />List<string></h1>
    <script>console.log("test");</script>
    <p>Some text with MyLittleContentEngine.<wbr />Services.<wbr />Content</p>
</body>
</html>
""";
        
        // Act
        var result = await ProcessHtmlThroughMiddleware(options, context, html);
        
        // Assert
        result.ShouldBe(expectedHtml);
    }

    private static WordBreakingMiddlewareOptions CreateOptions(WordBreakingMiddlewareOptions? options = null)
    {
        return options ?? new WordBreakingMiddlewareOptions();
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Response.StatusCode = 200;
        context.Response.ContentType = "text/html; charset=utf-8";
        return context;
    }

    private static async Task<string> ProcessHtmlThroughMiddleware(WordBreakingMiddlewareOptions options, HttpContext context, string html)
    {
        var inputBytes = Encoding.UTF8.GetBytes(html);
        var outputStream = new MemoryStream();
        
        context.Response.Body = outputStream;
        
        // Set up the next middleware to write the input HTML
        RequestDelegate next = async ctx =>
        {
            await ctx.Response.Body.WriteAsync(inputBytes);
        };
        
        // Create the middleware with the proper next delegate and options
        var middleware = new WordBreakingMiddleware(next, options);
        
        await middleware.InvokeAsync(context);
        
        outputStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(outputStream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}