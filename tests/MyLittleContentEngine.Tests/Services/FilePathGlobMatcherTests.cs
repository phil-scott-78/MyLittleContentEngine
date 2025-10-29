using System.Collections.Immutable;
using MyLittleContentEngine.Services;
using Shouldly;

namespace MyLittleContentEngine.Tests.Services;

public class FilePathGlobMatcherTests
{
    [Theory]
    [InlineData("test.md", "*.md", true)]
    [InlineData("test.txt", "*.md", false)]
    [InlineData("folder/test.md", "*.md", false)] // * doesn't cross directory boundaries
    [InlineData("test.razor", "*.razor", true)]
    [InlineData("Component.razor", "*.razor", true)]
    [InlineData("styles.css", "*.razor", false)]
    public void IsIgnored_SimpleWildcard_MatchesCorrectly(string path, string pattern, bool expectedIgnored)
    {
        // Arrange
        var patterns = ImmutableList.Create(new FilePath(pattern));

        // Act
        var result = FilePathGlobMatcher.IsIgnored(path, patterns);

        // Assert
        result.ShouldBe(expectedIgnored);
    }

    [Theory]
    [InlineData("folder/test.md", "**/*.md", true)]
    [InlineData("deep/nested/folder/test.md", "**/*.md", true)]
    [InlineData("test.md", "**/*.md", true)]
    [InlineData("folder/test.txt", "**/*.md", false)]
    [InlineData("folder/subfolder/component.razor", "**/*.razor", true)]
    public void IsIgnored_RecursiveWildcard_MatchesCorrectly(string path, string pattern, bool expectedIgnored)
    {
        // Arrange
        var patterns = ImmutableList.Create(new FilePath(pattern));

        // Act
        var result = FilePathGlobMatcher.IsIgnored(path, patterns);

        // Assert
        result.ShouldBe(expectedIgnored);
    }

    [Theory]
    [InlineData("temp/file.txt", "temp/*.txt", true)]
    [InlineData("temp/subfolder/file.txt", "temp/*.txt", false)] // * doesn't match subdirectories
    [InlineData("other/file.txt", "temp/*.txt", false)]
    [InlineData("temp/data.json", "temp/*.txt", false)]
    public void IsIgnored_DirectoryPrefixWithWildcard_MatchesCorrectly(string path, string pattern, bool expectedIgnored)
    {
        // Arrange
        var patterns = ImmutableList.Create(new FilePath(pattern));

        // Act
        var result = FilePathGlobMatcher.IsIgnored(path, patterns);

        // Assert
        result.ShouldBe(expectedIgnored);
    }

    [Theory]
    [InlineData("app.css", "app.css", true)]
    [InlineData("styles/app.css", "app.css", false)]
    [InlineData("App.css", "app.css", true)] // Case insensitive by default
    [InlineData("APP.CSS", "app.css", true)]
    [InlineData("other.css", "app.css", false)]
    public void IsIgnored_ExactPath_BackwardCompatibility(string path, string pattern, bool expectedIgnored)
    {
        // Arrange
        var patterns = ImmutableList.Create(new FilePath(pattern));

        // Act
        var result = FilePathGlobMatcher.IsIgnored(path, patterns);

        // Assert
        result.ShouldBe(expectedIgnored);
    }

    [Fact]
    public void IsIgnored_MultiplePatterns_MatchesAny()
    {
        // Arrange
        var patterns = ImmutableList.Create(
            new FilePath("*.md"),
            new FilePath("*.razor"),
            new FilePath("*.mdx")
        );

        // Act & Assert
        FilePathGlobMatcher.IsIgnored("test.md", patterns).ShouldBeTrue();
        FilePathGlobMatcher.IsIgnored("component.razor", patterns).ShouldBeTrue();
        FilePathGlobMatcher.IsIgnored("doc.mdx", patterns).ShouldBeTrue();
        FilePathGlobMatcher.IsIgnored("styles.css", patterns).ShouldBeFalse();
    }

    [Fact]
    public void IsIgnored_EmptyPatternList_ReturnsFalse()
    {
        // Arrange
        var patterns = ImmutableList<FilePath>.Empty;

        // Act
        var result = FilePathGlobMatcher.IsIgnored("any/path/file.txt", patterns);

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData("Component.razor.metadata.yml", "*.razor.metadata.yml", true)]
    [InlineData("pages/Home.razor.metadata.yml", "**/*.razor.metadata.yml", true)]
    [InlineData("Component.metadata.yml", "*.razor.metadata.yml", false)]
    public void IsIgnored_ComplexExtensions_MatchesCorrectly(string path, string pattern, bool expectedIgnored)
    {
        // Arrange
        var patterns = ImmutableList.Create(new FilePath(pattern));

        // Act
        var result = FilePathGlobMatcher.IsIgnored(path, patterns);

        // Assert
        result.ShouldBe(expectedIgnored);
    }

    [Theory]
    [InlineData("folder\\test.md", "*.md", false)] // Backslash path, pattern for current dir only
    [InlineData("folder\\test.md", "**/*.md", true)] // Backslash path with recursive pattern
    [InlineData("test.md", "*.md", true)] // Backslash in path gets normalized
    public void IsIgnored_BackslashPaths_NormalizedToForwardSlash(string path, string pattern, bool expectedIgnored)
    {
        // Arrange
        var patterns = ImmutableList.Create(new FilePath(pattern));

        // Act
        var result = FilePathGlobMatcher.IsIgnored(path, patterns);

        // Assert
        result.ShouldBe(expectedIgnored);
    }

    [Fact]
    public void IsIgnored_DefaultIgnoredPatterns_MatchesExpectedFiles()
    {
        // Arrange - These are the default patterns from ContentEngineOptions
        var patterns = ImmutableList.Create(
            new FilePath("**/*.razor"),
            new FilePath("**/*.razor.metadata.yml"),
            new FilePath("**/*.md"),
            new FilePath("**/*.mdx")
        );

        // Act & Assert - Files in root directory that should be ignored
        FilePathGlobMatcher.IsIgnored("Component.razor", patterns).ShouldBeTrue();
        FilePathGlobMatcher.IsIgnored("Home.razor.metadata.yml", patterns).ShouldBeTrue();
        FilePathGlobMatcher.IsIgnored("blog-post.md", patterns).ShouldBeTrue();
        FilePathGlobMatcher.IsIgnored("article.mdx", patterns).ShouldBeTrue();

        // Act & Assert - Files in subdirectories that should be ignored
        FilePathGlobMatcher.IsIgnored("components/Header.razor", patterns).ShouldBeTrue();
        FilePathGlobMatcher.IsIgnored("pages/Home.razor.metadata.yml", patterns).ShouldBeTrue();
        FilePathGlobMatcher.IsIgnored("blog/posts/article.md", patterns).ShouldBeTrue();
        FilePathGlobMatcher.IsIgnored("docs/deep/nested/guide.mdx", patterns).ShouldBeTrue();

        // Act & Assert - Files that should NOT be ignored (root and subdirectories)
        FilePathGlobMatcher.IsIgnored("styles.css", patterns).ShouldBeFalse();
        FilePathGlobMatcher.IsIgnored("script.js", patterns).ShouldBeFalse();
        FilePathGlobMatcher.IsIgnored("image.png", patterns).ShouldBeFalse();
        FilePathGlobMatcher.IsIgnored("data.json", patterns).ShouldBeFalse();
        FilePathGlobMatcher.IsIgnored("assets/styles.css", patterns).ShouldBeFalse();
        FilePathGlobMatcher.IsIgnored("js/app.js", patterns).ShouldBeFalse();
    }
}
