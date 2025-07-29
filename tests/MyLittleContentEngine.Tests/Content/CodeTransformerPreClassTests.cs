using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;
using Shouldly;

namespace MyLittleContentEngine.Tests.Content;

public class CodeTransformerPreClassTests
{
    [Fact]
    public void Transform_AddsFocusedClassToPre()
    {
        const string input = """
            <pre><code>int x = 5;
            int y = 10; // [!code focus]
            int z = 15;
            </code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("<pre class=\"has-focused\">");
        result.ShouldContain("<span class=\"line blurred\">int x = 5;</span>");
        result.ShouldContain("<span class=\"line focused\">int y = 10;</span>");
        result.ShouldContain("<span class=\"line blurred\">int z = 15;</span>");
    }

    [Fact]
    public void Transform_AddsHighlightedClassToPre()
    {
        const string input = """
            <pre><code>function test() {
            let x = 1; // [!code highlight]
            return x;
            }</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("<pre class=\"has-highlighted\">");
        result.ShouldContain("line highlight");
        result.ShouldContain("let x = 1;");
    }

    [Fact]
    public void Transform_AddsDiffClassToPre()
    {
        const string input = """
            <pre><code>const a = 1;
            const b = 2; // [!code ++]
            const c = 3; // [!code --]
            </code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("<pre class=\"has-diff\">");
        result.ShouldContain("<span class=\"line diff-add\">const b = 2;</span>");
        result.ShouldContain("<span class=\"line diff-remove\">const c = 3;</span>");
    }

    [Fact]
    public void Transform_AddsMultipleClassesToPre()
    {
        const string input = """
            <pre><code>function test() {
                let x = 1; // [!code highlight]
                let y = 2; // [!code focus]
                let z = 3; // [!code ++]
                throw new Error(); // [!code error]
            }</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        // Should have all relevant classes
        result.ShouldContain("has-focused");
        result.ShouldContain("has-highlighted");
        result.ShouldContain("has-diff");
        result.ShouldContain("has-errors");
    }

    [Fact]
    public void Transform_PreservesExistingPreClasses()
    {
        const string input = """
            <pre class="language-javascript"><code>let x = 1; // [!code focus]</code></pre>
            """;

        var result = CodeTransformer.Transform(input);

        result.ShouldContain("language-javascript");
        result.ShouldContain("has-focused");
    }
}