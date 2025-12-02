using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;
using Shouldly;

namespace MyLittleContentEngine.Tests.Content;

public class CodeTransformerIndentTests
{
    [Fact]
    public void NormalizeIndents_PlainTextWithCommonIndent()
    {
        const string input = """
            <pre><code>    var x = 1;
                var y = 2;
                return x + y;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Should remove 4 leading spaces from all lines
        result.ShouldContain("<span class=\"line\">var x = 1;</span>");
        result.ShouldContain("<span class=\"line\">var y = 2;</span>");
        result.ShouldContain("<span class=\"line\">return x + y;</span>");
        result.ShouldNotContain("    var x");
    }

    [Fact]
    public void NormalizeIndents_PlainTextWithDifferentIndents()
    {
        const string input = """
            <pre><code>    function test() {
                    console.log("nested");
                }
                return;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Should remove minimum indent (4 spaces) from all lines
        result.ShouldContain("<span class=\"line\">function test() {</span>");
        result.ShouldContain("<span class=\"line\">    console.log(\"nested\");</span>"); // 8 - 4 = 4 spaces
        result.ShouldContain("<span class=\"line\">}</span>");
        result.ShouldContain("<span class=\"line\">return;</span>");
    }

    [Fact]
    public void NormalizeIndents_EmptyLinesPreserved()
    {
        const string input = """
            <pre><code>    var x = 1;

                var y = 2;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Empty line should be preserved as "  " (two spaces)
        result.ShouldContain("<span class=\"line\">var x = 1;</span>");
        result.ShouldContain("<span class=\"line\">  </span>"); // Empty line preserved
        result.ShouldContain("<span class=\"line\">var y = 2;</span>");
    }

    [Fact]
    public void NormalizeIndents_NoCommonIndent()
    {
        const string input = """
            <pre><code>var x = 1;
                var y = 2;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // First line has no indent, so no normalization should occur
        result.ShouldContain("<span class=\"line\">var x = 1;</span>");
        result.ShouldContain("<span class=\"line\">    var y = 2;</span>"); // Indent preserved
    }

    [Fact]
    public void NormalizeIndents_AllEmptyLines()
    {
        const string input = """
            <pre><code>

                    </code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // All lines are whitespace, should be preserved
        result.ShouldContain("<span class=\"line\">  </span>");
        result.ShouldNotContain("var"); // No actual code
    }

    [Fact]
    public void NormalizeIndents_SingleLineWithIndent()
    {
        const string input = """
            <pre><code>    var x = 1;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Single line with indent should have it removed
        result.ShouldContain("<span class=\"line\">var x = 1;</span>");
        result.ShouldNotContain("    var");
    }

    [Fact]
    public void NormalizeIndents_PreservesHtmlStructure()
    {
        const string input = """
            <pre class="language-javascript"><code>    var x = 1;
                var y = 2;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // HTML structure should be preserved
        result.ShouldStartWith("<pre");
        result.ShouldContain("class=\"language-javascript\"");
        result.ShouldContain("<code>");
        result.ShouldContain("</code>");
        result.ShouldContain("</pre>");

        // Indents should be normalized
        result.ShouldContain("<span class=\"line\">var x = 1;</span>");
        result.ShouldContain("<span class=\"line\">var y = 2;</span>");
    }

    [Fact]
    public void NormalizeIndents_WithHighlightDirective()
    {
        const string input = """
            <pre><code>    var x = 1; // [!code highlight]
                var y = 2;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Should normalize indents AND apply highlight transformation
        result.ShouldContain("class=\"line highlight\"");
        result.ShouldContain("var x = 1;");
        result.ShouldContain("var y = 2;");
        result.ShouldNotContain("[!code highlight]");
        result.ShouldNotContain("    var x");
    }

    [Fact]
    public void NormalizeIndents_WithFocusDirective()
    {
        const string input = """
            <pre><code>    function test() {
                    var x = 1; // [!code focus]
                    var y = 2;
                }</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Should normalize indents AND apply focus transformation
        result.ShouldContain("class=\"line focused\"");
        result.ShouldContain("class=\"line blurred\"");
        result.ShouldContain("function test() {");
        result.ShouldNotContain("[!code focus]");

        // Relative indentation should be preserved
        result.ShouldContain("<span class=\"line focused\">    var x = 1;</span>"); // 8 - 4 = 4 spaces
    }

    [Fact]
    public void NormalizeIndents_WithDiffDirectives()
    {
        const string input = """
            <pre><code>    function test() {
            -       var x = 1; // [!code --]
            +       let x = 1; // [!code ++]
                }</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Should normalize indents AND apply diff transformations
        result.ShouldContain("class=\"line diff-remove\"");
        result.ShouldContain("class=\"line diff-add\"");
        result.ShouldContain("has-diff");
        result.ShouldNotContain("[!code --]");
        result.ShouldNotContain("[!code ++]");
    }

    [Fact]
    public void NormalizeIndents_WithSnippetDirectives()
    {
        const string input = """
            <pre><code>    // [!code include-start]
                var x = 1;
                var y = 2;
                // [!code include-end]
                var z = 3;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Should process snippet directives first, then normalize remaining lines
        result.ShouldContain("var x = 1;");
        result.ShouldContain("var y = 2;");
        result.ShouldNotContain("var z = 3;"); // Excluded by include directive
        result.ShouldNotContain("[!code include-start]");
        result.ShouldNotContain("[!code include-end]");
    }

    [Fact]
    public void NormalizeIndents_TabsNotAffected()
    {
        const string input = """
            <pre><code>    function test() {
            		var x = 1;
                }</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Only spaces should be normalized (tabs are preserved)
        result.ShouldContain("function test() {");
        result.ShouldContain("	var x = 1;"); // Tab preserved (but moved relative to normalized indent)
    }

    [Fact]
    public void NormalizeIndents_MixedContentTypes()
    {
        const string input = """
            <pre><code>    // Comment
                var x = 1;

                return x;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Should handle comments, code, empty lines
        result.ShouldContain("<span class=\"line\">// Comment</span>");
        result.ShouldContain("<span class=\"line\">var x = 1;</span>");
        result.ShouldContain("<span class=\"line\">  </span>"); // Empty line
        result.ShouldContain("<span class=\"line\">return x;</span>");
    }

    [Fact]
    public void NormalizeIndents_LinesWithLessIndentThanMinimum()
    {
        const string input = """
            <pre><code>        var x = 1;
                var y = 2;
                    var z = 3;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Due to raw string literal normalization, actual indents are 8, 4, 8
        // Minimum indent is 4 spaces (var y line)
        // Should remove 4 spaces from all lines
        result.ShouldContain("<span class=\"line\">    var x = 1;</span>"); // 8 - 4 = 4 spaces
        result.ShouldContain("<span class=\"line\">var y = 2;</span>");     // 4 - 4 = 0 spaces
        result.ShouldContain("<span class=\"line\">    var z = 3;</span>"); // 8 - 4 = 4 spaces
    }

    [Fact]
    public void NormalizeIndents_RealWorldCSharpExample()
    {
        const string input = """
            <pre class="language-csharp"><code>    public class Example
                {
                    public void Method()
                    {
                        var x = 1;
                    }
                }</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Real-world example with proper C# indentation structure
        result.ShouldContain("<span class=\"line\">public class Example</span>");
        result.ShouldContain("<span class=\"line\">{</span>");
        result.ShouldContain("<span class=\"line\">    public void Method()</span>"); // 8 - 4 = 4 spaces
        result.ShouldContain("<span class=\"line\">    {</span>");
        result.ShouldContain("<span class=\"line\">        var x = 1;</span>"); // 12 - 4 = 8 spaces
        result.ShouldContain("<span class=\"line\">    }</span>");
        result.ShouldContain("<span class=\"line\">}</span>");
    }
}
