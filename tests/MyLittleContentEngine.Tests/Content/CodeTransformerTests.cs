using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;
using Shouldly;

namespace MyLittleContentEngine.Tests.Content;

public class CodeTransformerTests
{
    [Fact]
    public void Transform_RemovesDirectiveAndEmptyComments()
    {
        const string input = """
            <pre><code>function calculateSum(numbers) {
                let total = 0; // [!code highlight]
                for (const num of numbers) {
                    total += num;
                }
                return total; // [!code hl] return the total
            }</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldNotContain("[!code highlight]");
        result.ShouldNotContain("[!code hl]");
        result.ShouldContain("// return the total");
        result.ShouldNotContain("let total = 0; //");
        result.ShouldContain("highlight");
    }

    [Fact]
    public void Transform_PreservesCommentsWithContent()
    {
        const string input = """
            <pre><code>function test() {
                // This is a regular comment
                let x = 1; // [!code focus]
                return x; // Another comment
            }</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("// This is a regular comment");
        result.ShouldContain("// Another comment");
        result.ShouldNotContain("// [!code focus]");
        result.ShouldContain("focused");
    }

    [Fact]
    public void Transform_HandlesHashComments()
    {
        const string input = """
            <pre><code>def calculate():
                total = 0  # [!code highlight]
                return total  # This returns the total
            </code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldNotContain("# [!code");
        result.ShouldContain("# This returns the total");
        result.ShouldNotContain("total = 0  #");
        result.ShouldContain("highlight");
    }

    [Fact]
    public void Transform_HandlesSqlComments()
    {
        const string input = """
            <pre><code>SELECT * FROM users -- [!code focus]
            WHERE active = 1; -- This filters active users
            </code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldNotContain("-- [!code focus]");
        result.ShouldContain("-- This filters active users");
        result.ShouldContain("focused");
    }

    [Fact]
    public void Transform_HandlesHtmlComments()
    {
        const string input = """
            <pre><code>&lt;div&gt;
                &lt;!-- [!code highlight] --&gt;
                &lt;p&gt;Content&lt;/p&gt; &lt;!-- This is content --&gt;
            &lt;/div&gt;</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldNotContain("[!code highlight]");
        result.ShouldContain("This is content");
        result.ShouldContain("highlight");
    }

    [Fact]
    public void Transform_HandlesVbNetComments()
    {
        const string input = """
            <pre><code>Dim total As Integer = 0 ' [!code focus]
            Return total ' This returns the total
            </code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldNotContain("' [!code focus]");
        result.ShouldContain("' This returns the total");
        result.ShouldContain("focused");
    }

    [Fact]
    public void Transform_HandlesBasicRemComments()
    {
        const string input = """
            <pre><code>10 PRINT "Hello" REM [!code highlight]
            20 PRINT "World" REM This prints world
            </code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldNotContain("REM [!code highlight]");
        result.ShouldContain("REM This prints world");
        result.ShouldContain("highlight");
    }

    [Fact]
    public void Transform_HandlesAssemblyComments()
    {
        const string input = """
            <pre><code>MOV AX, 0x1234 ; [!code focus]
            INT 21h        ; This calls DOS interrupt
            </code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldNotContain("; [!code focus]");
        result.ShouldContain("; This calls DOS interrupt");
        result.ShouldContain("focused");
    }

    [Fact]
    public void Transform_HandlesBatchComments()
    {
        const string input = """
            <pre><code>echo "Starting" % [!code highlight]
            pause % This waits for user input
            </code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldNotContain("% [!code highlight]");
        result.ShouldContain("% This waits for user input");
        result.ShouldContain("highlight");
    }

    [Fact]
    public void Transform_HandlesCStyleBlockComments()
    {
        const string input = """
            <pre><code>int x = 5; /* [!code focus] */
            int y = 10; /* This is a variable */
            </code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldNotContain("/* [!code focus] */");
        result.ShouldContain("/* This is a variable */");
        result.ShouldContain("focused");
    }

    [Fact]
    public void Transform_HandlesCssComments()
    {
        const string input = """
            <pre><code>.class { color: red; } /* [!code highlight] */
            .other { margin: 0; } /* This sets margin */
            </code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldNotContain("/* [!code highlight] */");
        result.ShouldContain("/* This sets margin */");
        result.ShouldContain("highlight");
    }

    [Fact]
    public void Transform_HandlesBlockCommentWithOnlyDirective()
    {
        const string input = """
            <pre><code>int x = 5;
            /* [!code focus] */
            int y = 10;
            </code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldNotContain("/* [!code focus] */");
        result.ShouldNotContain("/*  */");
        result.ShouldContain("focused");
    }

    [Fact]
    public void Transform_HandlesSyntaxHighlightedCommentSplitAcrossSpans()
    {
        const string input = """
            <pre><code>
            <span class="hljs-keyword">function</span> <span class="hljs-title">divide</span><span class="hljs-punctuation">(</span><span class="hljs-variable">a</span><span class="hljs-punctuation">,</span> <span class="hljs-variable">b</span><span class="hljs-punctuation">)</span> <span class="hljs-punctuation">{</span>
                <span class="hljs-keyword">if</span> (<span class="hljs-variable">b</span> <span class="hljs-operator">=</span> <span class="hljs-number">0</span>) <span class="hljs-punctuation">{</span> <span class="hljs-comment">//</span><span class="hljs-comment"> [!code error]</span>
                    <span class="hljs-variable">console</span><span class="hljs-punctuation">.</span><span class="hljs-title">warn</span>(<span class="hljs-string">"Division by zero"</span>)<span class="hljs-punctuation">;</span>
                <span class="hljs-punctuation">}</span>
            <span class="hljs-punctuation">}</span>
            </code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldNotContain("[!code error]");
        // Should not contain orphaned comment markers
        result.ShouldNotContain("<span class=\"hljs-comment\">//</span>");
        result.ShouldContain("error");
        result.ShouldContain("Division by zero");
    }

    [Fact]
    public void Transform_PreservesSyntaxHighlightedCommentsWithContent()
    {
        const string input = """
            <pre><code>
            <span class="hljs-keyword">let</span> <span class="hljs-variable">x</span> = <span class="hljs-number">1</span>; <span class="hljs-comment">//</span><span class="hljs-comment"> This is important</span>
            <span class="hljs-keyword">let</span> <span class="hljs-variable">y</span> = <span class="hljs-number">2</span>; <span class="hljs-comment">//</span><span class="hljs-comment"> [!code highlight]</span>
            <span class="hljs-keyword">return</span> <span class="hljs-variable">total</span>; <span class="hljs-comment">//</span><span class="hljs-comment"> [!code hl] return the total</span>
            </code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldNotContain("[!code highlight]");
        result.ShouldNotContain("[!code hl]");
        result.ShouldContain("This is important");
        // Most importantly, should preserve comment marker with content
        result.ShouldContain("// return the total");
        result.ShouldContain("highlight");
        // Should keep the comment with content merged
        result.ShouldContain("hljs-comment");
    }

    [Fact]
    public void Transform_HandlesMultipleSyntaxHighlightedCommentsOnSameLine()
    {
        const string input = """
            <pre><code>
            <span class="hljs-variable">total</span> += <span class="hljs-variable">num</span>; <span class="hljs-comment">/*</span><span class="hljs-comment"> [!code focus] </span><span class="hljs-comment">*/</span> <span class="hljs-comment">//</span><span class="hljs-comment"> accumulate</span>
            </code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldNotContain("[!code focus]");
        result.ShouldContain("accumulate");
        result.ShouldContain("focused");
        // Should not contain empty comment blocks
        result.ShouldNotContain("<span class=\"hljs-comment\">/*</span>");
        result.ShouldNotContain("<span class=\"hljs-comment\">*/</span>");
    }

    [Fact]
    public void Transform_PreservesEmptyLines()
    {
        // Note: This input has exactly 8 lines (no trailing newline in the code element):
        // 1. <!-- Before -->
        // 2. <a href="/docs/getting-started">Getting Started</a>
        // 3. <img src="/images/logo.png" alt="Logo">
        // 4. <script src="/scripts/app.js"></script>
        // 5. (empty line)
        // 6. <!-- After -->
        // 7. <a href="/my-app/docs/getting-started">Getting Started</a>
        // 8. <img src="/my-app/images/logo.png" alt="Logo">
        // 9. <script src="/my-app/scripts/app.js"></script>
        const string input = """
            <pre><code><span class="hljs-comment">&lt;!--</span><span class="hljs-comment"> Before </span><span class="hljs-comment">--&gt;</span>
            <span class="hljs-tag">&lt;</span><span class="hljs-tag">a</span><span class="hljs-tag"> </span><span class="hljs-attr">href</span><span class="hljs-punctuation">=</span><span class="hljs-string">"</span><span class="hljs-string">/docs/getting-started</span><span class="hljs-string">"</span><span class="hljs-tag">&gt;</span>Getting Started<span class="hljs-tag">&lt;/</span><span class="hljs-tag">a</span><span class="hljs-tag">&gt;</span>
            <span class="hljs-tag">&lt;</span><span class="hljs-tag">img</span><span class="hljs-tag"> </span><span class="hljs-attr">src</span><span class="hljs-punctuation">=</span><span class="hljs-string">"</span><span class="hljs-string">/images/logo.png</span><span class="hljs-string">"</span><span class="hljs-tag"> </span><span class="hljs-attr">alt</span><span class="hljs-punctuation">=</span><span class="hljs-string">"</span><span class="hljs-string">Logo</span><span class="hljs-string">"</span><span class="hljs-tag">&gt;</span>
            <span class="hljs-tag">&lt;</span><span class="hljs-tag">script</span><span class="hljs-tag"> </span><span class="hljs-attr">src</span><span class="hljs-punctuation">=</span><span class="hljs-string">"</span><span class="hljs-string">/scripts/app.js</span><span class="hljs-string">"</span><span class="hljs-tag">&gt;</span><span class="hljs-tag">&lt;</span><span class="hljs-tag">/</span><span class="hljs-tag">script</span><span class="hljs-tag">&gt;</span>
            
            <span class="hljs-comment">&lt;!--</span><span class="hljs-comment"> After </span><span class="hljs-comment">--&gt;</span>
            <span class="hljs-tag">&lt;</span><span class="hljs-tag">a</span><span class="hljs-tag"> </span><span class="hljs-attr">href</span><span class="hljs-punctuation">=</span><span class="hljs-string">"</span><span class="hljs-string">/my-app/docs/getting-started</span><span class="hljs-string">"</span><span class="hljs-tag">&gt;</span>Getting Started<span class="hljs-tag">&lt;/</span><span class="hljs-tag">a</span><span class="hljs-tag">&gt;</span>
            <span class="hljs-tag">&lt;</span><span class="hljs-tag">img</span><span class="hljs-tag"> </span><span class="hljs-attr">src</span><span class="hljs-punctuation">=</span><span class="hljs-string">"</span><span class="hljs-string">/my-app/images/logo.png</span><span class="hljs-string">"</span><span class="hljs-tag"> </span><span class="hljs-attr">alt</span><span class="hljs-punctuation">=</span><span class="hljs-string">"</span><span class="hljs-string">Logo</span><span class="hljs-string">"</span><span class="hljs-tag">&gt;</span>
            <span class="hljs-tag">&lt;</span><span class="hljs-tag">script</span><span class="hljs-tag"> </span><span class="hljs-attr">src</span><span class="hljs-punctuation">=</span><span class="hljs-string">"</span><span class="hljs-string">/my-app/scripts/app.js</span><span class="hljs-string">"</span><span class="hljs-tag">&gt;</span><span class="hljs-tag">&lt;</span><span class="hljs-tag">/</span><span class="hljs-tag">script</span><span class="hljs-tag">&gt;</span></code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Count the number of line spans
        var lineCount = result.Split("<span class=\"line\">").Length - 1;
        lineCount.ShouldBe(9); // Should have 9 lines including the empty line

        // Check that the result contains an empty line span
        result.ShouldContain("""
                             <span class="line">  </span>
                             """);
    }

    [Fact]
    public void Transform_HandlesWordHighlighting_WithoutPopover()
    {
        const string input = """
            <pre><code>function test() {
                let greeting = "Hello"; // [!code word:Hello] 
                return greeting;
            }</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldNotContain("[!code word:Hello]");
        result.ShouldContain("has-word-highlights");
        result.ShouldContain("<span class=\"word-highlight\">Hello</span>");
        result.ShouldNotContain("word-highlight-message");
    }

    [Fact]
    public void Transform_HandlesWordHighlighting_WithMessage()
    {
        const string input = """
            <pre><code>function test() {
                let variable = "value"; // [!code word:value|This is the variable value]
                return variable;
            }</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldNotContain("[!code word:value|This is the variable value]");
        result.ShouldContain("has-word-highlights");
        result.ShouldContain("word-highlight-wrapper");
        result.ShouldContain("<span class=\"word-highlight-with-message\">value</span>");
        result.ShouldContain("<div class=\"word-highlight-message\">This is the variable value");
    }

    [Fact]
    public void Transform_HandlesWordHighlighting_IgnoresInvalidDirectives()
    {
        const string input = """
            <pre><code>function test() {
                let x = 1; // [!code word:]
                let y = 2; // [!code word]
                return x + y;
            }</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldNotContain("word-highlight");
        result.ShouldNotContain("has-word-highlights");
    }

    [Fact]
    public void Transform_HandlesWordHighlighting_OnlyHighlightsFirstOccurrence()
    {
        const string input = """
            <pre><code>function test() {
                let Hello = "Hello world"; // [!code word:Hello]
                return Hello;
            }</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Should only contain one highlighted span, not multiple
        var highlightCount = result.Split("<span class=\"word-highlight\">Hello</span>").Length - 1;
        highlightCount.ShouldBe(1);
    }

    [Fact]
    public void Transform_HandlesWordHighlighting_WithWhitespaceInMessage()
    {
        const string input = """
            <pre><code>function test() {
                console.log("debug"); // [!code word:debug|  This is a debug message  ]
            }</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("word-highlight-wrapper");
        result.ShouldContain("<div class=\"word-highlight-message\">This is a debug message");
    }
}