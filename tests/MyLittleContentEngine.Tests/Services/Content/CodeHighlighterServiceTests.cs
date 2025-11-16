using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.CodeAnalysis.SyntaxHighlighting;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;
using Moq;
using Shouldly;

[assembly:CaptureConsole(CaptureOut = true)]

namespace MyLittleContentEngine.Tests.Services.Content;

public class CodeHighlighterServiceTests
{
    private static ITextMateHighlighter CreateTextMateHighlighter()
    {
        var registry = new TextMateLanguageRegistry();
        return new TextMateHighlighter(registry);
    }

    [Fact]
    public void Highlight_WithCSharpCode_ReturnsHighlightedHtml()
    {
        // Arrange
        var service = new CodeHighlighterService(CreateTextMateHighlighter());
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
        var service = new CodeHighlighterService(CreateTextMateHighlighter());
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
        var service = new CodeHighlighterService(CreateTextMateHighlighter());
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
        var service = new CodeHighlighterService(CreateTextMateHighlighter());
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
        var service = new CodeHighlighterService(CreateTextMateHighlighter());
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
        var service = new CodeHighlighterService(CreateTextMateHighlighter());
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
        var service = new CodeHighlighterService(CreateTextMateHighlighter());
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
        var service = new CodeHighlighterService(CreateTextMateHighlighter());
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
        var service = new CodeHighlighterService(CreateTextMateHighlighter());
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

        var service = new CodeHighlighterService(CreateTextMateHighlighter(), mockSyntaxHighlighter.Object);
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
        var service = new CodeHighlighterService(CreateTextMateHighlighter());
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
        var service = new CodeHighlighterService(CreateTextMateHighlighter());
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
        var service = new CodeHighlighterService(CreateTextMateHighlighter());
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
        var service = new CodeHighlighterService(CreateTextMateHighlighter());
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
        var service = new CodeHighlighterService(CreateTextMateHighlighter());
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
        var service = new CodeHighlighterService(CreateTextMateHighlighter());
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
        var service = new CodeHighlighterService(CreateTextMateHighlighter());
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

    [Fact]
    public void Highlight_WithXmlDocIdModifier_SingleId_ReturnsHighlightedSymbol()
    {
        // Arrange
        var mockSyntaxHighlighter = new Mock<ISyntaxHighlightingService>();
        mockSyntaxHighlighter
            .Setup(x => x.HighlightSymbolAsync("T:System.String", false))
            .ReturnsAsync(new HighlightedCode
            {
                Html = "<pre><code><span class=\"keyword\">public</span> <span class=\"keyword\">class</span> <span class=\"class\">String</span></code></pre>",
                PlainText = "public class String",
                Language = Language.CSharp,
                Success = true,
                ErrorMessage = null
            });

        var service = new CodeHighlighterService(CreateTextMateHighlighter(), mockSyntaxHighlighter.Object);

        // Act
        var result = service.Highlight("T:System.String", "csharp:xmldocid");

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.Html.ShouldContain("<span class=\"keyword\">public</span>");
        result.Html.ShouldContain("<span class=\"class\">String</span>");
        mockSyntaxHighlighter.Verify(
            x => x.HighlightSymbolAsync("T:System.String", false),
            Times.Once);
    }

    [Fact]
    public void Highlight_WithXmlDocIdModifier_MultipleIds_CombinesIntoOneBlock()
    {
        // Arrange
        var mockSyntaxHighlighter = new Mock<ISyntaxHighlightingService>();
        mockSyntaxHighlighter
            .Setup(x => x.HighlightSymbolAsync("T:System.String", false))
            .ReturnsAsync(new HighlightedCode
            {
                Html = "<pre><code><span class=\"keyword\">public</span> <span class=\"keyword\">class</span> <span class=\"class\">String</span></code></pre>",
                PlainText = "public class String",
                Language = Language.CSharp,
                Success = true,
                ErrorMessage = null
            });
        mockSyntaxHighlighter
            .Setup(x => x.HighlightSymbolAsync("T:System.Int32", false))
            .ReturnsAsync(new HighlightedCode
            {
                Html = "<pre><code><span class=\"keyword\">public</span> <span class=\"keyword\">struct</span> <span class=\"struct\">Int32</span></code></pre>",
                PlainText = "public struct Int32",
                Language = Language.CSharp,
                Success = true,
                ErrorMessage = null
            });

        var service = new CodeHighlighterService(CreateTextMateHighlighter(), mockSyntaxHighlighter.Object);
        var code = """
            T:System.String
            T:System.Int32
            """;

        // Act
        var result = service.Highlight(code, "csharp:xmldocid");

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.Html.ShouldContain("<span class=\"class\">String</span>");
        result.Html.ShouldContain("<span class=\"struct\">Int32</span>");
        // Verify both IDs were processed
        mockSyntaxHighlighter.Verify(
            x => x.HighlightSymbolAsync("T:System.String", false),
            Times.Once);
        mockSyntaxHighlighter.Verify(
            x => x.HighlightSymbolAsync("T:System.Int32", false),
            Times.Once);
    }

    [Fact]
    public void Highlight_WithXmlDocIdModifier_MixedSuccessAndFailure_ShowsInlineErrors()
    {
        // Arrange
        var mockSyntaxHighlighter = new Mock<ISyntaxHighlightingService>();
        mockSyntaxHighlighter
            .Setup(x => x.HighlightSymbolAsync("T:System.String", false))
            .ReturnsAsync(new HighlightedCode
            {
                Html = "<pre><code><span class=\"keyword\">public</span> <span class=\"keyword\">class</span> <span class=\"class\">String</span></code></pre>",
                PlainText = "public class String",
                Language = Language.CSharp,
                Success = true,
                ErrorMessage = null
            });
        mockSyntaxHighlighter
            .Setup(x => x.HighlightSymbolAsync("T:System.NonExistent", false))
            .ReturnsAsync(new HighlightedCode
            {
                Html = "",
                PlainText = "",
                Language = Language.CSharp,
                Success = false,
                ErrorMessage = "Symbol not found"
            });

        var service = new CodeHighlighterService(CreateTextMateHighlighter(), mockSyntaxHighlighter.Object);
        var code = """
            T:System.String
            T:System.NonExistent
            """;

        // Act
        var result = service.Highlight(code, "csharp:xmldocid");

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.Html.ShouldContain("<span class=\"class\">String</span>");
        result.Html.ShouldContain("<span class=\"comment\">// Error:");
        result.Html.ShouldContain("Symbol not found");
        result.Html.ShouldContain("T:System.NonExistent");
    }

    [Fact]
    public void Highlight_WithXmlDocIdModifier_BodyOnlyModifier_PassesBodyOnlyFlag()
    {
        // Arrange
        var mockSyntaxHighlighter = new Mock<ISyntaxHighlightingService>();
        mockSyntaxHighlighter
            .Setup(x => x.HighlightSymbolAsync("M:System.String.Concat(System.String,System.String)", true))
            .ReturnsAsync(new HighlightedCode
            {
                Html = "<pre><code><span class=\"keyword\">return</span> str0 + str1;</code></pre>",
                PlainText = "return str0 + str1;",
                Language = Language.CSharp,
                Success = true,
                ErrorMessage = null
            });

        var service = new CodeHighlighterService(CreateTextMateHighlighter(), mockSyntaxHighlighter.Object);

        // Act
        var result = service.Highlight("M:System.String.Concat(System.String,System.String)", "csharp:xmldocid,bodyonly");

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.Html.ShouldContain("<span class=\"keyword\">return</span>");
        mockSyntaxHighlighter.Verify(
            x => x.HighlightSymbolAsync("M:System.String.Concat(System.String,System.String)", true),
            Times.Once);
    }

    [Fact]
    public void Highlight_WithXmlDocIdModifier_MultipleIdsWithBodyOnly_CombinesBodies()
    {
        // Arrange
        var mockSyntaxHighlighter = new Mock<ISyntaxHighlightingService>();
        mockSyntaxHighlighter
            .Setup(x => x.HighlightSymbolAsync("M:MyClass.Method1", true))
            .ReturnsAsync(new HighlightedCode
            {
                Html = "<pre><code><span class=\"keyword\">return</span> <span class=\"number\">1</span>;</code></pre>",
                PlainText = "return 1;",
                Language = Language.CSharp,
                Success = true,
                ErrorMessage = null
            });
        mockSyntaxHighlighter
            .Setup(x => x.HighlightSymbolAsync("M:MyClass.Method2", true))
            .ReturnsAsync(new HighlightedCode
            {
                Html = "<pre><code><span class=\"keyword\">return</span> <span class=\"number\">2</span>;</code></pre>",
                PlainText = "return 2;",
                Language = Language.CSharp,
                Success = true,
                ErrorMessage = null
            });

        var service = new CodeHighlighterService(CreateTextMateHighlighter(), mockSyntaxHighlighter.Object);
        var code = """
            M:MyClass.Method1
            M:MyClass.Method2
            """;

        // Act
        var result = service.Highlight(code, "csharp:xmldocid,bodyonly");

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.Html.ShouldContain("<span class=\"number\">1</span>");
        result.Html.ShouldContain("<span class=\"number\">2</span>");
        mockSyntaxHighlighter.Verify(
            x => x.HighlightSymbolAsync("M:MyClass.Method1", true),
            Times.Once);
        mockSyntaxHighlighter.Verify(
            x => x.HighlightSymbolAsync("M:MyClass.Method2", true),
            Times.Once);
    }

    [Fact]
    public void Highlight_WithXmlDocIdModifier_EmptyLinesFiltered()
    {
        // Arrange
        var mockSyntaxHighlighter = new Mock<ISyntaxHighlightingService>();
        mockSyntaxHighlighter
            .Setup(x => x.HighlightSymbolAsync("T:System.String", false))
            .ReturnsAsync(new HighlightedCode
            {
                Html = "<pre><code><span class=\"keyword\">public</span> <span class=\"keyword\">class</span> <span class=\"class\">String</span></code></pre>",
                PlainText = "public class String",
                Language = Language.CSharp,
                Success = true,
                ErrorMessage = null
            });

        var service = new CodeHighlighterService(CreateTextMateHighlighter(), mockSyntaxHighlighter.Object);
        var code = """
            T:System.String


            """;

        // Act
        var result = service.Highlight(code, "csharp:xmldocid");

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        // Should only call once for the valid ID, empty lines should be filtered
        mockSyntaxHighlighter.Verify(
            x => x.HighlightSymbolAsync("T:System.String", false),
            Times.Once);
        mockSyntaxHighlighter.Verify(
            x => x.HighlightSymbolAsync(It.IsAny<string>(), false),
            Times.Once); // Only one call total
    }

    [Fact]
    public void Highlight_WithXmlDocIdModifier_NonCSharpLanguage_ReturnsError()
    {
        // Arrange
        var mockSyntaxHighlighter = new Mock<ISyntaxHighlightingService>();
        var service = new CodeHighlighterService(CreateTextMateHighlighter(), mockSyntaxHighlighter.Object);

        // Act
        var result = service.Highlight("T:System.String", "javascript:xmldocid");

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue(); // Overall highlighting succeeds with error message
        result.Html.ShouldContain("Error: Only C# is supported for xmldocid modifier");
        mockSyntaxHighlighter.Verify(
            x => x.HighlightSymbolAsync(It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public void Highlight_WithXmlDocIdModifier_NoSyntaxHighlighter_ReturnsEncodedText()
    {
        // Arrange
        var service = new CodeHighlighterService(CreateTextMateHighlighter(), syntaxHighlighter: null);

        // Act
        var result = service.Highlight("T:System.String", "csharp:xmldocid");

        // Assert
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
        result.Html.ShouldContain("T:System.String");
        result.Html.ShouldContain("<pre><code>");
    }
}
