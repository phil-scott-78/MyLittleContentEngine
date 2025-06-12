using Markdig.Syntax;
using MyLittleContentEngine.Services.Content.MarkdigExtensions;
using Shouldly;

namespace MyLittleContentEngine.Tests;

public class CodeBlockExtensionsTests
{
    private FencedCodeBlock CreateCodeBlock(string arguments)
    {
        return new FencedCodeBlock(null!)
        {
            Arguments = arguments
        };
    }

    [Fact]
    public void GetArgumentPairs_WithNullArguments_ReturnsEmptyDictionary()
    {
        var codeBlock = CreateCodeBlock(null!);

        var result = codeBlock.GetArgumentPairs();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetArgumentPairs_WithEmptyArguments_ReturnsEmptyDictionary()
    {
        var codeBlock = CreateCodeBlock("");

        var result = codeBlock.GetArgumentPairs();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetArgumentPairs_WithWhitespaceOnlyArguments_ReturnsEmptyDictionary()
    {
        var codeBlock = CreateCodeBlock("   \t\n  ");

        var result = codeBlock.GetArgumentPairs();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetArgumentPairs_WithSingleKeyValuePair_ReturnsCorrectDictionary()
    {
        var codeBlock = CreateCodeBlock("key=value");

        var result = codeBlock.GetArgumentPairs();

        result.Count.ShouldBe(1);
        result["key"].ShouldBe("value");
    }

    [Fact]
    public void GetArgumentPairs_WithMultipleKeyValuePairs_ReturnsCorrectDictionary()
    {
        var codeBlock = CreateCodeBlock("key1=value1 key2=value2 key3=value3");

        var result = codeBlock.GetArgumentPairs();

        result.Count.ShouldBe(3);
        result["key1"].ShouldBe("value1");
        result["key2"].ShouldBe("value2");
        result["key3"].ShouldBe("value3");
    }

    [Fact]
    public void GetArgumentPairs_WithSingleQuotedValues_ParsesCorrectly()
    {
        var codeBlock = CreateCodeBlock("key1='value with spaces' key2='another value'");

        var result = codeBlock.GetArgumentPairs();

        result.Count.ShouldBe(2);
        result["key1"].ShouldBe("value with spaces");
        result["key2"].ShouldBe("another value");
    }

    [Fact]
    public void GetArgumentPairs_WithDoubleQuotedValues_ParsesCorrectly()
    {
        var codeBlock = CreateCodeBlock("key1=\"value with spaces\" key2=\"another value\"");

        var result = codeBlock.GetArgumentPairs();

        result.Count.ShouldBe(2);
        result["key1"].ShouldBe("value with spaces");
        result["key2"].ShouldBe("another value");
    }

    [Fact]
    public void GetArgumentPairs_WithMixedQuotedAndUnquotedValues_ParsesCorrectly()
    {
        var codeBlock = CreateCodeBlock("key1=unquoted key2='single quoted' key3=\"double quoted\"");

        var result = codeBlock.GetArgumentPairs();

        result.Count.ShouldBe(3);
        result["key1"].ShouldBe("unquoted");
        result["key2"].ShouldBe("single quoted");
        result["key3"].ShouldBe("double quoted");
    }

    [Fact(Skip = "not working yet")]
    public void GetArgumentPairs_WithExtraWhitespace_HandlesCorrectly()
    {
        var codeBlock = CreateCodeBlock("  key1=value1   key2  =  value2   key3='value3'  ");

        var result = codeBlock.GetArgumentPairs();

        result.Count.ShouldBe(3);
        result["key1"].ShouldBe("value1");
        result["key2"].ShouldBe("value2");
        result["key3"].ShouldBe("value3");
    }

    [Fact]
    public void GetArgumentPairs_WithUnterminatedQuotes_HandlesGracefully()
    {
        var codeBlock = CreateCodeBlock("key1='unterminated quote key2=value2");

        var result = codeBlock.GetArgumentPairs();

        result.Count.ShouldBe(1);
        result["key1"].ShouldBe("unterminated quote key2=value2");
    }

    [Fact]
    public void GetArgumentPairs_WithEmptyQuotedValue_ReturnsEmptyString()
    {
        var codeBlock = CreateCodeBlock("key1='' key2=\"\"");

        var result = codeBlock.GetArgumentPairs();

        result.Count.ShouldBe(2);
        result["key1"].ShouldBe("");
        result["key2"].ShouldBe("");
    }

    [Fact(Skip = "not working yet")]
    public void GetArgumentPairs_WithKeyWithoutEquals_IgnoresKey()
    {
        var codeBlock = CreateCodeBlock("validkey=value invalidkey anotherkey=value2");

        var result = codeBlock.GetArgumentPairs();

        result.Count.ShouldBe(2);
        result["validkey"].ShouldBe("value");
        result["anotherkey"].ShouldBe("value2");
        result.ContainsKey("invalidkey").ShouldBeFalse();
    }

    [Fact(Skip = "not working yet")]
    public void GetArgumentPairs_WithKeyWithoutValue_IgnoresKey()
    {
        var codeBlock = CreateCodeBlock("key1=value1 key2= key3=value3");

        var result = codeBlock.GetArgumentPairs();

        result.Count.ShouldBe(2);
        result["key1"].ShouldBe("value1");
        result["key3"].ShouldBe("value3");
        result.ContainsKey("key2").ShouldBeFalse();
    }

    [Fact]
    public void GetArgumentPairs_WithCaseInsensitiveKeys_OverwritesWithLastValue()
    {
        var codeBlock = CreateCodeBlock("KEY=first key=second Key=third");

        var result = codeBlock.GetArgumentPairs();

        result.Count.ShouldBe(1);
        result["key"].ShouldBe("third");
    }

    [Fact]
    public void GetArgumentPairs_WithSpecialCharactersInQuotedValues_PreservesCharacters()
    {
        var codeBlock = CreateCodeBlock("key1='value!@#$%^&*()' key2=\"value+={}[]|\\:;\"");

        var result = codeBlock.GetArgumentPairs();

        result.Count.ShouldBe(2);
        result["key1"].ShouldBe("value!@#$%^&*()");
        result["key2"].ShouldBe("value+={}[]|\\:;");
    }

    [Fact]
    public void GetArgumentPairs_WithQuotesInQuotedValues_HandlesCorrectly()
    {
        var codeBlock = CreateCodeBlock("key1='value with \"double\" quotes' key2=\"value with 'single' quotes\"");

        var result = codeBlock.GetArgumentPairs();

        result.Count.ShouldBe(2);
        result["key1"].ShouldBe("value with \"double\" quotes");
        result["key2"].ShouldBe("value with 'single' quotes");
    }
}