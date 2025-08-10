using MyLittleContentEngine.Services;

namespace MyLittleContentEngine.Tests.ValueObjects;

public class UrlPathTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("/", "/")]
    [InlineData(".", "/")]
    [InlineData("//", "/")]
    [InlineData("///", "/")]
    [InlineData("/path//to///file", "/path/to/file")]
    [InlineData("path\\to\\file", "path/to/file")]
    public void Constructor_NormalizesPath(string? input, string expected)
    {
        var path = new UrlPath(input);
        Assert.Equal(expected, path.Value);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("/", false)]
    [InlineData("/path", false)]
    [InlineData("path", false)]
    public void IsEmpty_ReturnsCorrectValue(string input, bool expected)
    {
        var path = new UrlPath(input);
        Assert.Equal(expected, path.IsEmpty);
    }

    [Theory]
    [InlineData("/path", true)]
    [InlineData("path", false)]
    [InlineData("/", true)]
    [InlineData("", false)]
    public void IsAbsolute_ReturnsCorrectValue(string input, bool expected)
    {
        var path = new UrlPath(input);
        Assert.Equal(expected, path.IsAbsolute);
    }

    [Fact]
    public void Combine_HandlesEmptyPaths()
    {
        var result = UrlPath.Combine(UrlPath.Empty, UrlPath.Empty);
        Assert.Equal("", result.Value);
    }

    [Fact]
    public void Combine_PreservesAbsolutePath()
    {
        var result = UrlPath.Combine(new UrlPath("/base"), new UrlPath("path"));
        Assert.Equal("/base/path", result.Value);
    }

    [Fact]
    public void Combine_HandlesMultiplePaths()
    {
        var result = UrlPath.Combine(
            new UrlPath("/api"),
            new UrlPath("v1"),
            new UrlPath("users")
        );
        Assert.Equal("/api/v1/users", result.Value);
    }

    [Fact]
    public void SlashOperator_CombinesPaths()
    {
        var path1 = new UrlPath("/api");
        var path2 = new UrlPath("users");
        var result = path1 / path2;
        Assert.Equal("/api/users", result.Value);
    }

    [Theory]
    [InlineData("path", "/path")]
    [InlineData("/path", "/path")]
    [InlineData("", "/")]
    public void EnsureLeadingSlash_AddsSlashWhenNeeded(string input, string expected)
    {
        var path = new UrlPath(input);
        var result = path.EnsureLeadingSlash();
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("/path", "path")]
    [InlineData("path", "path")]
    [InlineData("/", "/")]
    public void RemoveLeadingSlash_RemovesSlashWhenPresent(string input, string expected)
    {
        var path = new UrlPath(input);
        var result = path.RemoveLeadingSlash();
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("path", "path/")]
    [InlineData("path/", "path/")]
    [InlineData("", "/")]
    public void EnsureTrailingSlash_AddsSlashWhenNeeded(string input, string expected)
    {
        var path = new UrlPath(input);
        var result = path.EnsureTrailingSlash();
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("path/", "path")]
    [InlineData("path", "path")]
    [InlineData("/", "")]  // Special case for RequestPath compatibility
    public void RemoveTrailingSlash_RemovesSlashWhenPresent(string input, string expected)
    {
        var path = new UrlPath(input);
        var result = path.RemoveTrailingSlash();
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void AppendQueryOrFragment_AppendsQueryString()
    {
        var path = new UrlPath("/api/users");
        var result = path.AppendQueryOrFragment("?page=1");
        Assert.Equal("/api/users?page=1", result.Value);
    }

    [Fact]
    public void AppendQueryOrFragment_AppendsFragment()
    {
        var path = new UrlPath("/docs");
        var result = path.AppendQueryOrFragment("#section1");
        Assert.Equal("/docs#section1", result.Value);
    }

    [Fact]
    public void AppendQueryOrFragment_ThrowsForInvalidInput()
    {
        var path = new UrlPath("/api");
        Assert.Throws<ArgumentException>(() => path.AppendQueryOrFragment("invalid"));
    }

    [Theory]
    [InlineData("/api/users/123", "/api/users")]
    [InlineData("/api", "/")]
    [InlineData("api/users", "api")]
    [InlineData("/", "/")]
    [InlineData("", "")]
    public void GetParent_ReturnsParentPath(string input, string expected)
    {
        var path = new UrlPath(input);
        var parent = path.GetParent();
        Assert.Equal(expected, parent.Value);
    }

    [Theory]
    [InlineData("/api/users/123", "123")]
    [InlineData("/api", "api")]
    [InlineData("users", "users")]
    [InlineData("/", "")]
    [InlineData("", "")]
    public void GetLastSegment_ReturnsLastSegment(string input, string expected)
    {
        var path = new UrlPath(input);
        var segment = path.GetLastSegment();
        Assert.Equal(expected, segment);
    }

    [Fact]
    public void Equality_ComparesValues()
    {
        var path1 = new UrlPath("/api/users");
        var path2 = new UrlPath("/api/users");
        var path3 = new UrlPath("/api/posts");

        Assert.Equal(path1, path2);
        Assert.NotEqual(path1, path3);
        Assert.True(path1 == path2);
        Assert.True(path1 != path3);
    }

    [Fact]
    public void ImplicitConversion_FromString()
    {
        UrlPath path = "/api/users";
        Assert.Equal("/api/users", path.Value);
    }

    [Fact]
    public void ImplicitConversion_ToString()
    {
        var path = new UrlPath("/api/users");
        string value = path;
        Assert.Equal("/api/users", value);
    }

    [Fact]
    public void TryParse_ValidPath_ReturnsTrue()
    {
        var success = UrlPath.TryParse("/api/users", out var path);
        Assert.True(success);
        Assert.Equal("/api/users", path!.Value);
    }

    [Fact]
    public void TryParse_NullPath_ReturnsTrue()
    {
        var success = UrlPath.TryParse(null, out var path);
        Assert.True(success);
        Assert.Equal("", path!.Value);
    }
}