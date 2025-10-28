using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.CodeAnalysis.SyntaxHighlighting;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;
using Moq;
using Shouldly;

[assembly:CaptureConsole(CaptureOut = true)]

namespace MyLittleContentEngine.Tests.Services.Content;

public class CodeHighlighterServiceTests
{
    [Fact]
    public void Highlight_WithCSharpCode_ReturnsHighlightedHtml()
    {
        // Arrange
        var service = new CodeHighlighterService();
        var code = """
            var message = "Hello, World!";
            Console.WriteLine(message);
            """;

        // Act
        var result = service.Highlight(code, "csharp");

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.Language.ShouldBe("csharp");
        result.PlainText.ShouldBe(code);
        result.Html.ShouldContain("<div class=\"code-highlight-wrapper not-prose\">");
        result.Html.ShouldContain("<pre><code>");
        result.Html.ShouldContain("</code></pre>");
    }

    [Fact]
    public void Highlight_WithJavaScriptCode_UsesTextMateHighlighter()
    {
        // Arrange
        var service = new CodeHighlighterService();
        var code = """
            const message = "Hello, World!";
            console.log(message);
            """;

        // Act
        var result = service.Highlight(code, "javascript");

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.Language.ShouldBe("javascript");
        result.Html.ShouldContain("<pre><code>");
        result.Html.ShouldContain("</code></pre>");
    }

    [Fact]
    public void Highlight_WithGbnfCode_UsesGbnfHighlighter()
    {
        // Arrange
        var service = new CodeHighlighterService();
        var code = """
            root ::= (statement)+
            statement ::= "hello" | "world"
            """;

        // Act
        var result = service.Highlight(code, "gbnf");

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.Language.ShouldBe("gbnf");
        result.Html.ShouldContain("<pre><code>");
    }

    [Fact]
    public void Highlight_WithShellCode_UsesShellHighlighter()
    {
        // Arrange
        var service = new CodeHighlighterService();
        var code = """
            echo "Hello, World!"
            ls -la
            """;

        // Act
        var result = service.Highlight(code, "bash");

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.Language.ShouldBe("bash");
        result.Html.ShouldContain("<pre><code>");
    }

    [Fact]
    public void Highlight_WithPlainText_ReturnsEncodedText()
    {
        // Arrange
        var service = new CodeHighlighterService();
        var code = """
            This is plain text.
            No highlighting needed.
            """;

        // Act
        var result = service.Highlight(code, "text");

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.Language.ShouldBe("text");
        result.Html.ShouldContain("<pre><code>");
        result.Html.ShouldContain("This is plain text.");
    }

    [Fact]
    public void Highlight_WithEmptyLanguage_ReturnsEncodedText()
    {
        // Arrange
        var service = new CodeHighlighterService();
        var code = "Some code without a language";

        // Act
        var result = service.Highlight(code, "");

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.Language.ShouldBe("");
        result.Html.ShouldContain("<pre><code>");
        result.Html.ShouldContain("Some code without a language");
    }

    [Fact]
    public void Highlight_WithCustomOptions_UsesProvidedOptions()
    {
        // Arrange
        var service = new CodeHighlighterService();
        var code = "var x = 1;";
        var customOptions = new CodeHighlightRenderOptions
        {
            OuterWrapperCss = "custom-wrapper",
            StandaloneContainerCss = "custom-container",
            PreBaseCss = "custom-pre-base",
            PreStandaloneCss = "custom-pre-standalone"
        };

        // Act
        var result = service.Highlight(code, "csharp", customOptions);

        // Assert
        result.ShouldNotBeNull();
        result.Html.ShouldContain("custom-wrapper");
        result.Html.ShouldContain("custom-container");
        result.Html.ShouldContain("custom-pre-standalone");
    }

    [Fact]
    public void Highlight_InTabGroup_OmitsStandaloneContainerCss()
    {
        // Arrange
        var service = new CodeHighlighterService();
        var code = "var x = 1;";

        // Act
        var result = service.Highlight(code, "csharp", isInTabGroup: true);

        // Assert
        result.ShouldNotBeNull();
        result.Html.ShouldContain("code-highlight-wrapper");
        result.Html.ShouldNotContain("standalone-code-container");
        result.Html.ShouldNotContain("standalone-code-highlight");
    }

    [Fact]
    public void Highlight_NotInTabGroup_IncludesStandaloneContainerCss()
    {
        // Arrange
        var service = new CodeHighlighterService();
        var code = "var x = 1;";

        // Act
        var result = service.Highlight(code, "csharp", isInTabGroup: false);

        // Assert
        result.ShouldNotBeNull();
        result.Html.ShouldContain("code-highlight-wrapper");
        result.Html.ShouldContain("standalone-code-container");
        result.Html.ShouldContain("standalone-code-highlight");
    }

    [Fact]
    public void Highlight_WithRoslynService_UsesCSharpHighlighter()
    {
        // Arrange
        var mockSyntaxHighlighter = new Mock<ISyntaxHighlightingService>();
        mockSyntaxHighlighter
            .Setup(x => x.HighlightAsync(It.IsAny<string>(), Language.CSharp))
            .ReturnsAsync(new HighlightedCode
            {
                Html = "<span class=\"keyword\">var</span> x = <span class=\"number\">1</span>;",
                PlainText = "var x = 1;",
                Language = Language.CSharp,
                Success = true,
                ErrorMessage = null
            });

        var service = new CodeHighlighterService(mockSyntaxHighlighter.Object);
        var code = "var x = 1;";

        // Act
        var result = service.Highlight(code, "csharp");

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.Html.ShouldContain("<span class=\"keyword\">var</span>");
        mockSyntaxHighlighter.Verify(
            x => x.HighlightAsync("var x = 1;", Language.CSharp),
            Times.Once);
    }

    [Fact]
    public void Highlight_WithCaseInsensitiveLanguage_WorksCorrectly()
    {
        // Arrange
        var service = new CodeHighlighterService();
        var code = "var x = 1;";

        // Act
        var resultLower = service.Highlight(code, "csharp");
        var resultUpper = service.Highlight(code, "CSHARP");
        var resultMixed = service.Highlight(code, "CSharp");

        // Assert
        resultLower.Success.ShouldBeTrue();
        resultUpper.Success.ShouldBeTrue();
        resultMixed.Success.ShouldBeTrue();
    }

    
    [Theory]
    [InlineData("c#")]
    [InlineData("cs")]
    [InlineData("csharp")]
    public void Highlight_WithCSharpAliases_AllWork(string languageAlias)
    {
        // Arrange
        var service = new CodeHighlighterService();
        var code = "var x = 1;";

        // Act
        var result = service.Highlight(code, languageAlias);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        // Service now returns wrapped HTML with divs
        result.Html.ShouldContain("code-highlight-wrapper");
        result.Html.ShouldContain("<pre>");
        result.Html.ShouldContain("</code>");  // Changed to closing tag to avoid any parsing ambiguity
    }

    [Theory]
    [InlineData("vb")]
    [InlineData("vbnet")]
    public void Highlight_WithVisualBasicAliases_AllWork(string languageAlias)
    {
        // Arrange
        var service = new CodeHighlighterService();
        var code = "Dim x As Integer = 1";

        // Act
        var result = service.Highlight(code, languageAlias);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        // Service now returns wrapped HTML with divs
        result.Html.ShouldContain("code-highlight-wrapper");
        result.Html.ShouldContain("<pre>");
        result.Html.ShouldContain("</code>");  // Changed to closing tag to avoid any parsing ambiguity
    }

    [Theory]
    [InlineData("bash")]
    [InlineData("shell")]
    public void Highlight_WithShellAliases_AllWork(string languageAlias)
    {
        // Arrange
        var service = new CodeHighlighterService();
        var code = "echo 'Hello'";

        // Act
        var result = service.Highlight(code, languageAlias);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.Html.ShouldContain("<pre><code>");
    }

    [Fact]
    public void Highlight_WithHtmlSpecialCharacters_EscapesCorrectly()
    {
        // Arrange
        var service = new CodeHighlighterService();
        var code = """
            var html = "<div>Hello & Goodbye</div>";
            """;

        // Act
        var result = service.Highlight(code, "text");

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        // The HTML should be escaped in the output
        result.Html.ShouldContain("&lt;div&gt;");
        result.Html.ShouldContain("&amp;");
    }

    [Fact]
    public void Highlight_AppliesLineTransformations()
    {
        // Arrange
        var service = new CodeHighlighterService();
        var code = """
            var x = 1; // [!code highlight]
            var y = 2;
            """;

        // Act
        var result = service.Highlight(code, "javascript");

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        // CodeTransformer should add line classes
        result.Html.ShouldContain("<span class=\"line");
    }

    [Theory]
    [InlineData("markdown")]
    [InlineData("md")]
    public void Highlight_WithMarkdown_SkipsTransformations(string languageAlias)
    {
        // Arrange
        var service = new CodeHighlighterService();
        var code = """
            # Header
            Some text // [!code highlight]
            """;

        // Act
        var result = service.Highlight(code, languageAlias);

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        // For markdown, transformations should be skipped, but wrapping still happens
        result.Html.ShouldContain("code-highlight-wrapper");
        result.Html.ShouldContain("<pre>");
        result.Html.ShouldContain("</code>");  // Changed to closing tag to avoid any parsing ambiguity
        // Should NOT contain line transformation classes since transformations are skipped
        result.Html.ShouldNotContain("<span class=\"line");
    }
}
