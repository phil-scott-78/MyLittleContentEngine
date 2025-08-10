using MyLittleContentEngine.Services;
using Shouldly;

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
        path.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("/", false)]
    [InlineData("/path", false)]
    [InlineData("path", false)]
    public void IsEmpty_ReturnsCorrectValue(string input, bool expected)
    {
        var path = new UrlPath(input);
        path.IsEmpty.ShouldBe(expected);
    }

    [Theory]
    [InlineData("/path", true)]
    [InlineData("path", false)]
    [InlineData("/", true)]
    [InlineData("", false)]
    public void IsAbsolute_ReturnsCorrectValue(string input, bool expected)
    {
        var path = new UrlPath(input);
        path.IsAbsolute.ShouldBe(expected);
    }

    [Fact]
    public void Combine_HandlesEmptyPaths()
    {
        var result = UrlPath.Combine(UrlPath.Empty, UrlPath.Empty);
        result.Value.ShouldBe("");
    }

    [Fact]
    public void Combine_PreservesAbsolutePath()
    {
        var result = UrlPath.Combine(new UrlPath("/base"), new UrlPath("path"));
        result.Value.ShouldBe("/base/path");
    }

    [Fact]
    public void Combine_HandlesMultiplePaths()
    {
        var result = UrlPath.Combine(
            new UrlPath("/api"),
            new UrlPath("v1"),
            new UrlPath("users")
        );
        result.Value.ShouldBe("/api/v1/users");
    }

    [Fact]
    public void SlashOperator_CombinesPaths()
    {
        var path1 = new UrlPath("/api");
        var path2 = new UrlPath("users");
        var result = path1 / path2;
        result.Value.ShouldBe("/api/users");
    }

    [Theory]
    [InlineData("path", "/path")]
    [InlineData("/path", "/path")]
    [InlineData("", "/")]
    public void EnsureLeadingSlash_AddsSlashWhenNeeded(string input, string expected)
    {
        var path = new UrlPath(input);
        var result = path.EnsureLeadingSlash();
        result.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("/path", "path")]
    [InlineData("path", "path")]
    [InlineData("/", "/")]
    public void RemoveLeadingSlash_RemovesSlashWhenPresent(string input, string expected)
    {
        var path = new UrlPath(input);
        var result = path.RemoveLeadingSlash();
        result.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("path", "path/")]
    [InlineData("path/", "path/")]
    [InlineData("", "/")]
    public void EnsureTrailingSlash_AddsSlashWhenNeeded(string input, string expected)
    {
        var path = new UrlPath(input);
        var result = path.EnsureTrailingSlash();
        result.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("path/", "path")]
    [InlineData("path", "path")]
    [InlineData("/", "")]  // Special case for RequestPath compatibility
    public void RemoveTrailingSlash_RemovesSlashWhenPresent(string input, string expected)
    {
        var path = new UrlPath(input);
        var result = path.RemoveTrailingSlash();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public void AppendQueryOrFragment_AppendsQueryString()
    {
        var path = new UrlPath("/api/users");
        var result = path.AppendQueryOrFragment("?page=1");
        result.Value.ShouldBe("/api/users?page=1");
    }

    [Fact]
    public void AppendQueryOrFragment_AppendsFragment()
    {
        var path = new UrlPath("/docs");
        var result = path.AppendQueryOrFragment("#section1");
        result.Value.ShouldBe("/docs#section1");
    }

    [Fact]
    public void AppendQueryOrFragment_ThrowsForInvalidInput()
    {
        var path = new UrlPath("/api");
        Should.Throw<ArgumentException>(() => path.AppendQueryOrFragment("invalid"));
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
        parent.Value.ShouldBe(expected);
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
        segment.ShouldBe(expected);
    }

    [Fact]
    public void Equality_ComparesValues()
    {
        var path1 = new UrlPath("/api/users");
        var path2 = new UrlPath("/api/users");
        var path3 = new UrlPath("/api/posts");

        path1.ShouldBe(path2);
        path1.ShouldNotBe(path3);
        (path1 == path2).ShouldBeTrue();
        (path1 != path3).ShouldBeTrue();
    }

    [Fact]
    public void ImplicitConversion_FromString()
    {
        UrlPath path = "/api/users";
        path.Value.ShouldBe("/api/users");
    }

    [Fact]
    public void ImplicitConversion_ToString()
    {
        var path = new UrlPath("/api/users");
        string value = path;
        value.ShouldBe("/api/users");
    }

    [Fact]
    public void TryParse_ValidPath_ReturnsTrue()
    {
        var success = UrlPath.TryParse("/api/users", out var path);
        success.ShouldBeTrue();
        path!.ShouldBe(new UrlPath("/api/users"));
    }

    [Fact]
    public void TryParse_NullPath_ReturnsTrue()
    {
        var success = UrlPath.TryParse(null, out var path);
        success.ShouldBeTrue();
        path!.ShouldBe(new UrlPath(""));
    }
}