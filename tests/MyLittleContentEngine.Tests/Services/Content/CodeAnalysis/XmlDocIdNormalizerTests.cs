using MyLittleContentEngine.Services.Content.CodeAnalysis.SymbolAnalysis;

namespace MyLittleContentEngine.Tests.Services.Content.CodeAnalysis;

public class XmlDocIdNormalizerTests
{
    [Fact]
    public void Normalize_WithNoParameters_ReturnsUnchanged()
    {
        // Arrange
        var xmlDocId = "M:MyNamespace.MyType.MyMethod";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal(xmlDocId, result);
    }

    [Fact]
    public void Normalize_WithSingleParameter_StripsNamespace()
    {
        // Arrange
        var xmlDocId = "M:MyNamespace.MyType.MyMethod(System.String)";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal("M:MyNamespace.MyType.MyMethod(String)", result);
    }

    [Fact]
    public void Normalize_WithMultipleParameters_StripsAllNamespaces()
    {
        // Arrange
        var xmlDocId = "M:MyNamespace.MyType.MyMethod(System.String,System.Int32,System.Boolean)";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal("M:MyNamespace.MyType.MyMethod(String,Int32,Boolean)", result);
    }

    [Fact]
    public void Normalize_WithGenericParameter_StripsNamespace()
    {
        // Arrange
        var xmlDocId = "M:MyNamespace.MyType.MyMethod(System.Collections.Generic.List{System.String})";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal("M:MyNamespace.MyType.MyMethod(List{String})", result);
    }

    [Fact]
    public void Normalize_WithNestedGenerics_StripsAllNamespaces()
    {
        // Arrange
        var xmlDocId = "M:MyNamespace.MyType.MyMethod(System.Func{System.IServiceProvider,MyNamespace.Options{``0}})";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal("M:MyNamespace.MyType.MyMethod(Func{IServiceProvider,Options{``0}})", result);
    }

    [Fact]
    public void Normalize_WithGenericMethodParameter_PreservesGenericMarkers()
    {
        // Arrange
        var xmlDocId = "M:MyNamespace.MyType.MyMethod``1(``0,System.String)";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal("M:MyNamespace.MyType.MyMethod``1(``0,String)", result);
    }

    [Fact]
    public void Normalize_WithGenericTypeParameter_PreservesGenericMarkers()
    {
        // Arrange
        var xmlDocId = "M:MyNamespace.MyType`1.MyMethod(`0,System.String)";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal("M:MyNamespace.MyType`1.MyMethod(`0,String)", result);
    }

    [Fact]
    public void Normalize_RealWorldExample_ProgressSample()
    {
        // Arrange - Example from issue description
        var xmlDocIdWithNamespace = "M:Spectre.Docs.Examples.AsciiCast.Samples.ProgressSample.Run(Spectre.Console.IAnsiConsole)";
        var xmlDocIdWithoutNamespace = "M:Spectre.Docs.Examples.AsciiCast.Samples.ProgressSample.Run(IAnsiConsole)";

        // Act
        var result1 = XmlDocIdNormalizer.Normalize(xmlDocIdWithNamespace);
        var result2 = XmlDocIdNormalizer.Normalize(xmlDocIdWithoutNamespace);

        // Assert - Both should normalize to the same value
        Assert.Equal("M:Spectre.Docs.Examples.AsciiCast.Samples.ProgressSample.Run(IAnsiConsole)", result1);
        Assert.Equal("M:Spectre.Docs.Examples.AsciiCast.Samples.ProgressSample.Run(IAnsiConsole)", result2);
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void Normalize_RealWorldExample_WithMarkdownContentService()
    {
        // Arrange - Example from issue description
        var xmlDocId = "M:MyLittleContentEngine.ContentEngineExtensions.WithMarkdownContentService``1(MyLittleContentEngine.IConfiguredContentEngineServiceCollection,System.Func{System.IServiceProvider,MyLittleContentEngine.MarkdownContentOptions{``0}})";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal("M:MyLittleContentEngine.ContentEngineExtensions.WithMarkdownContentService``1(IConfiguredContentEngineServiceCollection,Func{IServiceProvider,MarkdownContentOptions{``0}})", result);
    }

    [Fact]
    public void Normalize_WithArrayParameter_StripsNamespace()
    {
        // Arrange
        var xmlDocId = "M:MyNamespace.MyType.MyMethod(System.String[])";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal("M:MyNamespace.MyType.MyMethod(String[])", result);
    }

    [Fact]
    public void Normalize_WithMultidimensionalArray_StripsNamespace()
    {
        // Arrange
        var xmlDocId = "M:MyNamespace.MyType.MyMethod(System.Int32[0:,0:])";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal("M:MyNamespace.MyType.MyMethod(Int32[0:,0:])", result);
    }

    [Fact]
    public void Normalize_WithRefParameter_StripsNamespace()
    {
        // Arrange
        var xmlDocId = "M:MyNamespace.MyType.MyMethod(System.String@)";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal("M:MyNamespace.MyType.MyMethod(String@)", result);
    }

    [Fact]
    public void Normalize_WithPointerParameter_StripsNamespace()
    {
        // Arrange
        var xmlDocId = "M:MyNamespace.MyType.MyMethod(System.Int32*)";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal("M:MyNamespace.MyType.MyMethod(Int32*)", result);
    }

    [Fact]
    public void Normalize_WithPropertyIndexer_StripsNamespace()
    {
        // Arrange
        var xmlDocId = "P:MyNamespace.MyType.Item(System.String,System.Int32)";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal("P:MyNamespace.MyType.Item(String,Int32)", result);
    }

    [Fact]
    public void Normalize_WithEmptyString_ReturnsEmpty()
    {
        // Arrange
        var xmlDocId = "";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Normalize_WithNull_ReturnsNull()
    {
        // Arrange
        string? xmlDocId = null;

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Normalize_WithEmptyParameterList_ReturnsUnchanged()
    {
        // Arrange
        var xmlDocId = "M:MyNamespace.MyType.MyMethod()";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal("M:MyNamespace.MyType.MyMethod()", result);
    }

    [Fact]
    public void Normalize_WithParameterAlreadyWithoutNamespace_ReturnsUnchanged()
    {
        // Arrange
        var xmlDocId = "M:MyNamespace.MyType.MyMethod(String,Int32)";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal(xmlDocId, result);
    }

    [Fact]
    public void Normalize_WithMalformedId_MissingClosingParen_ReturnsUnchanged()
    {
        // Arrange
        var xmlDocId = "M:MyNamespace.MyType.MyMethod(System.String";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal(xmlDocId, result);
    }

    [Fact]
    public void Normalize_WithMalformedId_ClosingParenBeforeOpening_ReturnsUnchanged()
    {
        // Arrange
        var xmlDocId = "M:MyNamespace.MyType.MyMethod)System.String(";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal(xmlDocId, result);
    }

    [Fact]
    public void Normalize_WithTypePrefix_NoParameters_ReturnsUnchanged()
    {
        // Arrange
        var xmlDocId = "T:MyNamespace.MyType";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal(xmlDocId, result);
    }

    [Fact]
    public void Normalize_WithComplexNestedGenerics_StripsAllNamespaces()
    {
        // Arrange
        var xmlDocId = "M:MyNamespace.MyType.MyMethod(System.Collections.Generic.Dictionary{System.String,System.Collections.Generic.List{System.Int32}})";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal("M:MyNamespace.MyType.MyMethod(Dictionary{String,List{Int32}})", result);
    }

    [Fact]
    public void Normalize_KeepsMemberNameFullyQualified()
    {
        // Arrange - Verifies that the method name BEFORE the parameters keeps its namespace
        var xmlDocId = "M:My.Deeply.Nested.Namespace.Type.Method(System.String)";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert - Method name should still be fully qualified
        Assert.Equal("M:My.Deeply.Nested.Namespace.Type.Method(String)", result);
        Assert.StartsWith("M:My.Deeply.Nested.Namespace.Type.Method(", result);
    }

    [Fact]
    public void Normalize_WithTupleParameter_StripsNamespaces()
    {
        // Arrange
        var xmlDocId = "M:MyNamespace.MyType.MyMethod(System.ValueTuple{System.String,System.Int32})";

        // Act
        var result = XmlDocIdNormalizer.Normalize(xmlDocId);

        // Assert
        Assert.Equal("M:MyNamespace.MyType.MyMethod(ValueTuple{String,Int32})", result);
    }
}
