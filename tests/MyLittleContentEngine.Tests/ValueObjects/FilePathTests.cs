using MyLittleContentEngine.Services;
using Shouldly;
using Testably.Abstractions.Testing;

namespace MyLittleContentEngine.Tests.ValueObjects;

public class FilePathTests
{
    private readonly MockFileSystem _fileSystem;

    public FilePathTests()
    {
        _fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
        _fileSystem
            .Initialize()
            .WithSubdirectory("test")
            .Initialized(s => 
                s.WithFile(@"C:\test\file.txt").Which(f => f.HasStringContent("content"))
            );

        // Set the mock file system for extension methods
        FilePath.FileSystem = _fileSystem;
        FilePathExtensions.DefaultFileSystem = _fileSystem;
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

    [Theory]
    [InlineData(@"test\file.txt", false)]
    [InlineData("", false)]
    public void IsAbsolute_ReturnsCorrectValue(string input, bool expected)
    {
        var path = new FilePath(input);
        path.IsAbsolute.ShouldBe(expected);
    }

    [Fact]
    public void Combine_HandlesMultiplePaths()
    {
        var result = FilePath.Combine(
            new FilePath("folder"),
            new FilePath("sub-folder"),
            new FilePath("file.txt")
        );
        result.Value.ShouldBe(@"folder\sub-folder\file.txt");
    }

    [Fact]
    public void SlashOperator_CombinesPaths()
    {
        var path1 = new FilePath(@"test");
        var path2 = new FilePath("file.txt");
        var result = path1 / path2;
        result.Value.ShouldBe(@"test\file.txt");
    }

    [Fact]
    public void GetDirectory_ReturnsParentDirectory()
    {
        var path = new FilePath(@"test\folder\file.txt");
        var directory = path.GetDirectory();
        directory.Value.ShouldBe(@"test\folder");
    }

    [Fact]
    public void GetFileName_ReturnsFileName()
    {
        var path = new FilePath(@"test\file.txt");
        var fileName = path.GetFileName();
        fileName.ShouldBe("file.txt");
    }

    [Fact]
    public void GetFileNameWithoutExtension_ReturnsNameWithoutExtension()
    {
        var path = new FilePath(@"test\file.txt");
        var fileName = path.GetFileNameWithoutExtension();
        fileName.ShouldBe("file");
    }

    [Fact]
    public void GetExtension_ReturnsExtension()
    {
        var path = new FilePath(@"test\file.txt");
        var extension = path.GetExtension();
        extension.ShouldBe(".txt");
    }

    [Fact]
    public void ChangeExtension_ChangesExtension()
    {
        var path = new FilePath(@"test\file.txt");
        var newPath = path.ChangeExtension(".md");
        newPath.Value.ShouldBe(@"test\file.md");
    }

    [Fact]
    public void GetRelativeTo_ReturnsRelativePath()
    {
        var path = new FilePath(@"test\folder\subfolder\file.txt");
        var basePath = new FilePath(@"test");
        var relative = path.GetRelativeTo(basePath);
        relative.Value.ShouldBe(@"folder\subfolder\file.txt");
    }

    [Fact]
    public void FileExists_ReturnsTrueForExistingFile()
    {
        var path = new FilePath(@"test\file.txt");
        path.FileExists(_fileSystem).ShouldBeTrue();
    }

    [Fact]
    public void FileExists_ReturnsFalseForNonExistingFile()
    {
        var path = new FilePath(@"test\nonexistent.txt");
        path.FileExists(_fileSystem).ShouldBeFalse();
    }

    [Fact]
    public void DirectoryExists_ReturnsTrueForExistingDirectory()
    {
        var path = new FilePath(@"test");
        path.DirectoryExists(_fileSystem).ShouldBeTrue();
    }

    [Fact]
    public void DirectoryExists_ReturnsFalseForNonExistingDirectory()
    {
        var path = new FilePath(@"nonexistent");
        path.DirectoryExists(_fileSystem).ShouldBeFalse();
    }

    [Fact]
    public void ToUrlPath_ConvertsToUrlPath()
    {
        var filePath = new FilePath(@"content\blog\post.md");
        var urlPath = filePath.ToUrlPath();
        urlPath.Value.ShouldBe("content/blog/post.md");
    }

    [Fact]
    public void FromUrlPath_ConvertsFromUrlPath()
    {
        var urlPath = new UrlPath("content/blog/post.md");
        var filePath = FilePath.FromUrlPath(urlPath);
        filePath.Value.ShouldBe(@"content\blog\post.md");
    }

    [Fact]
    public void EnsureDirectoryExists_CreatesDirectory()
    {
        var path = new FilePath(@"C:\newdir\file.txt");
        path.EnsureDirectoryExists(_fileSystem);
        _fileSystem.Directory.Exists(@"C:\newdir").ShouldBeTrue();
    }

    [Fact]
    public void GetParent_ReturnsParentDirectory()
    {
        var path = new FilePath(@"test\folder\file.txt");
        var parent = path.GetParent();
        parent.Value.ShouldBe(@"test\folder");
    }

    [Fact]
    public void IsValid_ReturnsTrueForValidPath()
    {
        var path = new FilePath(@"test\file.txt");
        path.IsValid().ShouldBeTrue();
    }

    [Fact]
    public void Combine_WithStringSegments()
    {
        var basePath = new FilePath(@"test");
        var result = basePath.Combine("folder", "subfolder", "file.txt");
        result.Value.ShouldBe(@"test\folder\subfolder\file.txt");
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