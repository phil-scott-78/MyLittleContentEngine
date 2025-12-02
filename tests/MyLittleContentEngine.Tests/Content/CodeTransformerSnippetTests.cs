using MyLittleContentEngine.Services;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;
using Shouldly;

namespace MyLittleContentEngine.Tests.Content;

public class CodeTransformerSnippetTests
{
    [Fact]
    public void Transform_IncludeMode_SingleRegion_ShowsOnlyIncludedLines()
    {
        const string input = """
            <pre><code>public void MyMethod()
            {
                // Setup code here
                // [!code include-start]
                var important = DoSomething();
                // [!code include-end]
                // Cleanup code here
            }</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Should only contain the included line
        result.ShouldContain("var important = DoSomething();");

        // Should NOT contain excluded lines or markers
        result.ShouldNotContain("Setup code");
        result.ShouldNotContain("Cleanup code");
        result.ShouldNotContain("[!code include-start]");
        result.ShouldNotContain("[!code include-end]");

        // Verify line count
        var lineCount = result.Split("<span class=\"line\">").Length - 1;
        lineCount.ShouldBe(1);
    }

    [Fact]
    public void Transform_IncludeMode_MultipleRegions_ShowsAllIncludedLines()
    {
        const string input = """
            <pre><code>// [!code include-start]
            Line 1 - shown
            // [!code include-end]
            Line 2 - hidden
            // [!code include-start]
            Line 3 - shown
            // [!code include-end]
            Line 4 - hidden</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("Line 1 - shown");
        result.ShouldContain("Line 3 - shown");
        result.ShouldNotContain("Line 2 - hidden");
        result.ShouldNotContain("Line 4 - hidden");

        var lineCount = result.Split("<span class=\"line\">").Length - 1;
        lineCount.ShouldBe(2);
    }

    [Fact]
    public void Transform_IncludeMode_EmptyRegion_ShowsNothing()
    {
        const string input = """
            <pre><code>Line 1
            // [!code include-start]
            // [!code include-end]
            Line 2</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Only the marker lines are in the include region, so nothing is shown
        result.ShouldNotContain("Line 1");
        result.ShouldNotContain("Line 2");

        var lineCount = result.Split("<span class=\"line\">").Length - 1;
        lineCount.ShouldBe(0);
    }

    [Fact]
    public void Transform_ExcludeMode_SingleRegion_HidesExcludedLines()
    {
        const string input = """
            <pre><code>public void MyMethod()
            {
                var x = 1;
                // [!code exclude-start]
                var helper = HelperMethod();
                // [!code exclude-end]
                return Process(x);
            }</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("var x = 1;");
        result.ShouldContain("return Process(x);");
        result.ShouldNotContain("var helper = HelperMethod();");
        result.ShouldNotContain("[!code exclude-start]");
    }

    [Fact]
    public void Transform_ExcludeMode_MultipleRegions_HidesAllExcludedLines()
    {
        const string input = """
            <pre><code>Line 1 - shown
            // [!code exclude-start]
            Line 2 - hidden
            // [!code exclude-end]
            Line 3 - shown
            // [!code exclude-start]
            Line 4 - hidden
            // [!code exclude-end]
            Line 5 - shown</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("Line 1 - shown");
        result.ShouldContain("Line 3 - shown");
        result.ShouldContain("Line 5 - shown");
        result.ShouldNotContain("Line 2 - hidden");
        result.ShouldNotContain("Line 4 - hidden");

        var lineCount = result.Split("<span class=\"line\">").Length - 1;
        lineCount.ShouldBe(3);
    }

    [Fact]
    public void Transform_MixedMode_AppliesBothIncludeAndExclude()
    {
        const string input = """
            <pre><code>Line 1 - hidden (not in include)
            // [!code include-start]
            Line 2 - shown
            // [!code exclude-start]
            Line 3 - hidden (excluded)
            // [!code exclude-end]
            Line 4 - shown
            // [!code include-end]
            Line 5 - hidden (not in include)</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("Line 2 - shown");
        result.ShouldContain("Line 4 - shown");
        result.ShouldNotContain("Line 1 - hidden");
        result.ShouldNotContain("Line 3 - hidden");
        result.ShouldNotContain("Line 5 - hidden");

        var lineCount = result.Split("<span class=\"line\">").Length - 1;
        lineCount.ShouldBe(2);
    }

    [Fact]
    public void Transform_NestedIncludeRegions_ShouldReturnAllContent()
    {
        const string input = """
            <pre><code>// [!code include-start]
            Line 1
            // [!code include-start]
            Line 2
            // [!code include-end]
            // [!code include-end]</code></pre>
            """;

        var result = CodeTransformer.Transform(input);
        result.ShouldContain("Line 1");
        result.ShouldContain("Line 2");
    }

    [Fact]
    public void Transform_NestedExcludeRegions_ShouldReturnAllContent()
    {
        const string input = """
            <pre><code>// [!code exclude-start]
            Line 1
            // [!code exclude-start]
            Line 2
            // [!code exclude-end]
            // [!code exclude-end]</code></pre>
            """;

        var result = CodeTransformer.Transform(input);
        result.ShouldContain("Line 1");
        result.ShouldContain("Line 2");
    }

    [Fact]
    public void Transform_UnmatchedIncludeEnd_IgnoresDirective()
    {
        const string input = """
            <pre><code>Line 1
            // [!code include-end]
            Line 2</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Should not throw, just remove the directive
        result.ShouldContain("Line 1");
        result.ShouldContain("Line 2");
        result.ShouldNotContain("[!code include-end]");
    }

    [Fact]
    public void Transform_UnmatchedIncludeStart_IgnoresDirective()
    {
        const string input = """
            <pre><code>Line 1
            // [!code include-start]
            Line 2</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Should not throw, just remove the directive
        // No include-end, so no transformation happens
        result.ShouldContain("Line 1");
        result.ShouldContain("Line 2");
        result.ShouldNotContain("[!code include-start]");
    }

    [Fact]
    public void Transform_UnmatchedExcludeEnd_IgnoresDirective()
    {
        const string input = """
            <pre><code>Line 1
            // [!code exclude-end]
            Line 2</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("Line 1");
        result.ShouldContain("Line 2");
        result.ShouldNotContain("[!code exclude-end]");
    }

    [Fact]
    public void Transform_UnmatchedExcludeStart_IgnoresDirective()
    {
        const string input = """
            <pre><code>Line 1
            // [!code exclude-start]
            Line 2</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("Line 1");
        result.ShouldContain("Line 2");
        result.ShouldNotContain("[!code exclude-start]");
    }

    [Fact]
    public void Transform_SnippetDirectives_WorkWithJavaScriptComments()
    {
        const string input = """
            <pre><code>// [!code include-start]
            const x = 1;
            // [!code include-end]
            const y = 2;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("const x = 1;");
        result.ShouldNotContain("const y = 2;");
    }

    [Fact]
    public void Transform_SnippetDirectives_WorkWithPythonComments()
    {
        const string input = """
            <pre><code># [!code include-start]
            x = 1
            # [!code include-end]
            y = 2</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("x = 1");
        result.ShouldNotContain("y = 2");
    }

    [Fact]
    public void Transform_SnippetDirectives_WorkWithSqlComments()
    {
        const string input = """
            <pre><code>-- [!code include-start]
            SELECT * FROM users;
            -- [!code include-end]
            SELECT * FROM orders;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("SELECT * FROM users;");
        result.ShouldNotContain("SELECT * FROM orders;");
    }

    [Fact]
    public void Transform_SnippetDirectives_WorkWithHtmlComments()
    {
        const string input = """
            <pre><code>&lt;!-- [!code include-start] --&gt;
            &lt;div&gt;Content&lt;/div&gt;
            &lt;!-- [!code include-end] --&gt;
            &lt;div&gt;Hidden&lt;/div&gt;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("Content");
        result.ShouldNotContain("Hidden");
    }

    [Fact]
    public void Transform_SnippetWithHighlight_AppliesBothTransformations()
    {
        const string input = """
            <pre><code>// [!code include-start]
            let x = 1; // [!code highlight]
            let y = 2;
            // [!code include-end]
            let z = 3;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("let x = 1;");
        result.ShouldContain("let y = 2;");
        result.ShouldNotContain("let z = 3;");
        result.ShouldContain("highlight");
        result.ShouldContain("has-highlighted");
    }

    [Fact]
    public void Transform_SnippetRemovesLineWithHighlight_RemovesTransformation()
    {
        const string input = """
            <pre><code>let x = 1;
            // [!code exclude-start]
            let y = 2; // [!code highlight]
            // [!code exclude-end]
            let z = 3;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("let x = 1;");
        result.ShouldContain("let z = 3;");
        result.ShouldNotContain("let y = 2;");
        // Should NOT have highlight classes since the highlighted line was removed
        result.ShouldNotContain("has-highlighted");
    }

    [Fact]
    public void Transform_SnippetWithFocus_AppliesBothTransformations()
    {
        const string input = """
            <pre><code>// [!code include-start]
            let x = 1; // [!code focus]
            let y = 2;
            // [!code include-end]
            let z = 3;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("let x = 1;");
        result.ShouldContain("let y = 2;");
        result.ShouldNotContain("let z = 3;");
        result.ShouldContain("focused");
        result.ShouldContain("has-focused");
        result.ShouldContain("blurred");
    }

    [Fact]
    public void Transform_SnippetWithDiffMarkers_AppliesBothTransformations()
    {
        const string input = """
            <pre><code>// [!code include-start]
            let x = 1; // [!code ++]
            let y = 2; // [!code --]
            // [!code include-end]
            let z = 3;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("let x = 1;");
        result.ShouldContain("let y = 2;");
        result.ShouldNotContain("let z = 3;");
        result.ShouldContain("diff-add");
        result.ShouldContain("diff-remove");
        result.ShouldContain("has-diff");
    }

    [Fact]
    public void Transform_SnippetWithSyntaxHighlighting_PreservesSpans()
    {
        const string input = """
            <pre><code><span class="hljs-comment">// [!code include-start]</span>
            <span class="hljs-keyword">let</span> <span class="hljs-variable">x</span> = <span class="hljs-number">1</span>;
            <span class="hljs-comment">// [!code include-end]</span>
            <span class="hljs-keyword">let</span> <span class="hljs-variable">y</span> = <span class="hljs-number">2</span>;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("hljs-keyword");
        result.ShouldContain("hljs-variable");
        result.ShouldContain("hljs-number");
        result.ShouldNotContain("let y = 2;");
    }

    [Fact]
    public void Transform_IncludeRegion_FirstAndLastLines()
    {
        const string input = """
            <pre><code>// [!code include-start]
            First line
            // [!code include-end]
            Middle line
            // [!code include-start]
            Last line
            // [!code include-end]</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("First line");
        result.ShouldContain("Last line");
        result.ShouldNotContain("Middle line");
    }

    [Fact]
    public void Transform_CaseInsensitiveDirectives()
    {
        const string input = """
            <pre><code>// [!code INCLUDE-START]
            Line 1
            // [!code Include-End]
            Line 2</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("Line 1");
        result.ShouldNotContain("Line 2");
    }

    [Fact]
    public void Transform_ExcludeMode_IncludingMarkersInRegion()
    {
        const string input = """
            <pre><code>Line 1 - shown
            // [!code exclude-start]
            Line 2 - hidden
            Line 3 - hidden
            // [!code exclude-end]
            Line 4 - shown</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("Line 1 - shown");
        result.ShouldContain("Line 4 - shown");
        result.ShouldNotContain("Line 2 - hidden");
        result.ShouldNotContain("Line 3 - hidden");
        result.ShouldNotContain("[!code exclude-start]");
        result.ShouldNotContain("[!code exclude-end]");

        var lineCount = result.Split("<span class=\"line\">").Length - 1;
        lineCount.ShouldBe(2);
    }
}
