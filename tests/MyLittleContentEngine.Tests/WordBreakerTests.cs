using Microsoft.AspNetCore.Components;
using MyLittleContentEngine.Services.Content;
using Shouldly;
using Xunit;

namespace MyLittleContentEngine.Tests;

public class WordBreakerTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("short", "short")]
    [InlineData("System.Collections.Generic.List<string>", "System.<wbr />Collections.<wbr />Generic.<wbr />List<<wbr />string>")]
    [InlineData("MyNamespace.MyClass+InnerClass", "MyNamespace.<wbr />MyClass+<wbr />InnerClass")]
    [InlineData("Dictionary<string,int>", "Dictionary<<wbr />string,<wbr />int>")]
    [InlineData("int[]", "int[<wbr />]")]
    [InlineData("ref string", "ref string")]
    [InlineData("T*", "T*")]
    [InlineData("string&", "string&")]
    [InlineData("`1", "`<wbr />1")]
    public void InsertWordBreaks_ShouldInsertBreaksCorrectly(string input, string expected)
    {
        var result = WordBreaker.InsertWordBreaks(input);
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("short", false)]
    [InlineData("verylongwordwithoutbreakchars", false)]
    [InlineData("System.Collections.Generic.List<string>", true)]
    [InlineData("MyNamespace.MyClass+InnerClass", true)]
    [InlineData("Dictionary<string,int>", true)]
    [InlineData("int[]", false)] // Too short
    [InlineData("verylongwordwithoutbreakchars.SomeProperty", true)]
    public void ShouldInsertWordBreaks_ShouldReturnCorrectValue(string input, bool expected)
    {
        var result = WordBreaker.ShouldInsertWordBreaks(input);
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("", 5, false)]
    [InlineData("short", 5, false)]
    [InlineData("System.Collections", 5, true)]
    [InlineData("System.Collections", 20, false)]
    public void ShouldInsertWordBreaks_WithCustomMinLength_ShouldReturnCorrectValue(string input, int minLength, bool expected)
    {
        var result = WordBreaker.ShouldInsertWordBreaks(input, minLength);
        result.ShouldBe(expected);
    }

    [Fact]
    public void CreateMarkupStringWithWordBreaks_ShouldReturnMarkupString()
    {
        var input = "System.Collections.Generic.List<string>";
        var result = WordBreaker.CreateMarkupStringWithWordBreaks(input);
        
        result.ShouldBeOfType<MarkupString>();
        result.Value.ShouldBe("System.<wbr />Collections.<wbr />Generic.<wbr />List<<wbr />string>");
    }

    [Fact]
    public void CreateMarkupStringWithWordBreaks_WithEmptyString_ShouldReturnEmptyMarkupString()
    {
        var result = WordBreaker.CreateMarkupStringWithWordBreaks("");
        
        result.ShouldBeOfType<MarkupString>();
        result.Value.ShouldBe("");
    }

    [Theory]
    [InlineData("System.Collections.Generic.List<string>")]
    [InlineData("MyNamespace.MyClass+InnerClass")]
    [InlineData("Dictionary<string,int>")]
    [InlineData("ComplexType<T,U>[].Method()")]
    public void InsertWordBreaks_ShouldNotDuplicateBreaks(string input)
    {
        var firstPass = WordBreaker.InsertWordBreaks(input);
        var secondPass = WordBreaker.InsertWordBreaks(firstPass);
        
        // Should not insert additional breaks when processing already processed text
        secondPass.ShouldNotContain("<wbr /><wbr /><wbr />");
    }

    [Fact]
    public void InsertWordBreaks_ShouldHandleConsecutiveBreakCharacters()
    {
        var input = "Type<>[]";
        var result = WordBreaker.InsertWordBreaks(input);
        
        result.ShouldBe("Type<<wbr />>[<wbr />]");
    }

    [Theory]
    [InlineData(".", ".")]
    [InlineData("+", "+")]
    [InlineData(",", ",")]
    [InlineData("<", "<")]
    [InlineData(">", ">")]
    [InlineData("[", "[")]
    [InlineData("]", "]")]
    [InlineData("&", "&")]
    [InlineData("*", "*")]
    [InlineData("`", "`")]
    public void InsertWordBreaks_ShouldHandleIndividualBreakCharacters(string input, string expected)
    {
        var result = WordBreaker.InsertWordBreaks(input);
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("end.", "end.")]
    [InlineData("end+", "end+")]
    [InlineData("end,", "end,")]
    [InlineData("end<", "end<")]
    [InlineData("end>", "end>")]
    [InlineData("end[", "end[")]
    [InlineData("end]", "end]")]
    [InlineData("end&", "end&")]
    [InlineData("end*", "end*")]
    [InlineData("end`", "end`")]
    public void InsertWordBreaks_ShouldNotInsertBreakAtEndOfString(string input, string expected)
    {
        var result = WordBreaker.InsertWordBreaks(input);
        result.ShouldBe(expected);
    }
}