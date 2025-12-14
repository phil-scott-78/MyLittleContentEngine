using MyLittleContentEngine.Services;
using Shouldly;
using Testably.Abstractions.Testing;

namespace MyLittleContentEngine.Tests.ValueObjects;

public class FilePathTests
{
    public FilePathTests()
    {
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
        fileSystem
            .Initialize()
            .WithSubdirectory("test")
            .Initialized(s =>
                s.WithFile(@"C:\test\file.txt").Which(f => f.HasStringContent("content"))
            );
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Constructor_HandlesEmptyPaths(string? input, string expected)
    {
        var path = new FilePath(input);
        path.Value.ShouldBe(expected);
    }


    [Fact]
    public void ToUrlPath_ConvertsToUrlPath()
    {
        var filePath = new FilePath(@"content\blog\post.md");
        var urlPath = filePath.ToUrlPath();
        urlPath.Value.ShouldBe("content/blog/post.md");
    }

    [Fact]
    public void Equality_ComparesValues()
    {
        var path1 = new FilePath(@"test\file.txt");
        var path2 = new FilePath(@"test\file.txt");
        var path3 = new FilePath(@"test\other.txt");

        path1.ShouldBe(path2);
        path1.ShouldNotBe(path3);
        (path1 == path2).ShouldBeTrue();
        (path1 != path3).ShouldBeTrue();
    }

    [Fact]
    public void Equality_IsCaseInsensitiveOnWindows()
    {
        if (OperatingSystem.IsWindows())
        {
            var path1 = new FilePath(@"Test\File.txt");
            var path2 = new FilePath(@"test\file.txt");
            path1.ShouldBe(path2);
        }
    }

    [Fact]
    public void ImplicitConversion_FromString()
    {
        FilePath path = @"test\file.txt";
        path.Value.ShouldBe(@"test\file.txt");
    }

    [Fact]
    public void ImplicitConversion_ToString()
    {
        var path = new FilePath(@"test\file.txt");
        string value = path;
        value.ShouldBe(@"test\file.txt");
    }

    [Fact]
    public void TryParse_ValidPath_ReturnsTrue()
    {
        var success = FilePath.TryParse(@"test\file.txt", out var path);
        success.ShouldBeTrue();
        path!.ShouldBe(new FilePath(@"test\file.txt"));
    }

    [Fact]
    public void TryParse_NullPath_ReturnsTrue()
    {
        var success = FilePath.TryParse(null, out var path);
        success.ShouldBeTrue();
        path!.ShouldBe(new FilePath(""));
    }
}