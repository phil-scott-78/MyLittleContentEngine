using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;
using Shouldly;

namespace MyLittleContentEngine.Tests.Services.Content.MarkdigExtensions.CodeHighlighting;

public class TextMateLanguageRegistryTests
{
    [Fact]
    public void Constructor_WithoutCallback_CreatesRegistrySuccessfully()
    {
        // Act
        var registry = new TextMateLanguageRegistry();

        // Assert
        registry.ShouldNotBeNull();
        registry.Registry.ShouldNotBeNull();
        registry.RegistryOptions.ShouldNotBeNull();
    }

    [Fact]
    public void AddGrammar_WithValidParameters_AddsCustomMapping()
    {
        // Arrange
        var registry = new TextMateLanguageRegistry();

        // Act
        var result = registry.AddGrammar("mylang", "source.mylang");

        // Assert
        result.ShouldBe(registry); // Fluent API
        var scopeName = registry.GetScopeNameForLanguage("mylang");
        scopeName.ShouldBe("source.mylang");
    }

    [Fact]
    public void AddGrammar_WithCaseInsensitiveLanguageId_ReturnsMapping()
    {
        // Arrange
        var registry = new TextMateLanguageRegistry(r =>
        {
            r.AddGrammar("MyLang", "source.mylang");
        });

        // Act
        var scopeName = registry.GetScopeNameForLanguage("mylang");

        // Assert
        scopeName.ShouldBe("source.mylang");
    }

    [Fact]
    public void AddGrammar_WithNullLanguageId_ThrowsArgumentException()
    {
        // Arrange
        var registry = new TextMateLanguageRegistry();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            registry.AddGrammar(null!, "source.test"));
    }

    [Fact]
    public void AddGrammar_WithNullScopeName_ThrowsArgumentException()
    {
        // Arrange
        var registry = new TextMateLanguageRegistry();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            registry.AddGrammar("test", null!));
    }

    [Fact]
    public void AddGrammarFromJson_WithValidParameters_AddsMapping()
    {
        // Arrange
        var registry = new TextMateLanguageRegistry();
        var grammarJson = """{"scopeName": "source.test", "patterns": []}""";

        // Act
        var result = registry.AddGrammarFromJson("testlang", grammarJson);

        // Assert
        result.ShouldBe(registry); // Fluent API
        var scopeName = registry.GetScopeNameForLanguage("testlang");
        // The scope name should be extracted from the grammar JSON
        scopeName.ShouldBe("source.test");
    }

    [Fact]
    public void GetScopeNameForLanguage_WithBuiltInLanguage_ReturnsScopeName()
    {
        // Arrange
        var registry = new TextMateLanguageRegistry();

        // Act
        var scopeName = registry.GetScopeNameForLanguage("csharp");

        // Assert
        scopeName.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void GetScopeNameForLanguage_WithCustomLanguage_ReturnsCustomScopeName()
    {
        // Arrange
        var registry = new TextMateLanguageRegistry(r =>
        {
            r.AddGrammar("customlang", "source.custom");
        });

        // Act
        var scopeName = registry.GetScopeNameForLanguage("customlang");

        // Assert
        scopeName.ShouldBe("source.custom");
    }

    [Fact]
    public void GetScopeNameForLanguage_CustomLanguageOverridesBuiltIn()
    {
        // Arrange - Override a built-in language
        var registry = new TextMateLanguageRegistry(r =>
        {
            r.AddGrammar("csharp", "source.custom.csharp");
        });

        // Act
        var scopeName = registry.GetScopeNameForLanguage("csharp");

        // Assert
        scopeName.ShouldBe("source.custom.csharp");
    }

    [Fact]
    public void AddGrammar_FluentApi_SupportsChaining()
    {
        // Arrange
        var registry = new TextMateLanguageRegistry();

        // Act
        registry
            .AddGrammar("lang1", "source.lang1")
            .AddGrammar("lang2", "source.lang2")
            .AddGrammar("lang3", "source.lang3");

        // Assert
        registry.GetScopeNameForLanguage("lang1").ShouldBe("source.lang1");
        registry.GetScopeNameForLanguage("lang2").ShouldBe("source.lang2");
        registry.GetScopeNameForLanguage("lang3").ShouldBe("source.lang3");
    }
}
