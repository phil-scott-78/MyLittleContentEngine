using System.IO.Abstractions.TestingHelpers;
using MyLittleContentEngine.Services;
using Xunit;

namespace MyLittleContentEngine.Tests.ValueObjects;

public class FilePathTests
{
    private readonly MockFileSystem _fileSystem;

    public FilePathTests()
    {
        _fileSystem = new MockFileSystem();
        _fileSystem.AddDirectory(@"C:\test");
        _fileSystem.AddFile(@"C:\test\file.txt", new MockFileData("content"));
        // Set the mock file system for extension methods
        FilePathExtensions.DefaultFileSystem = _fileSystem;
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Constructor_HandlesEmptyPaths(string? input, string expected)
    {
        var path = new FilePath(input);
        Assert.Equal(expected, path.Value);
    }

    [Theory]
    [InlineData(@"C:\test", true)]
    [InlineData(@"test\file.txt", false)]
    [InlineData("", false)]
    public void IsAbsolute_ReturnsCorrectValue(string input, bool expected)
    {
        var path = new FilePath(input);
        Assert.Equal(expected, path.IsAbsolute);
    }

    [Fact]
    public void Combine_HandlesMultiplePaths()
    {
        var result = FilePath.Combine(
            new FilePath(@"C:\test"),
            new FilePath("folder"),
            new FilePath("file.txt")
        );
        Assert.Equal(@"C:\test\folder\file.txt", result.Value);
    }

    [Fact]
    public void SlashOperator_CombinesPaths()
    {
        var path1 = new FilePath(@"C:\test");
        var path2 = new FilePath("file.txt");
        var result = path1 / path2;
        Assert.Equal(@"C:\test\file.txt", result.Value);
    }

    [Fact]
    public void GetDirectory_ReturnsParentDirectory()
    {
        var path = new FilePath(@"C:\test\folder\file.txt");
        var directory = path.GetDirectory();
        Assert.Equal(@"C:\test\folder", directory.Value);
    }

    [Fact]
    public void GetFileName_ReturnsFileName()
    {
        var path = new FilePath(@"C:\test\file.txt");
        var fileName = path.GetFileName();
        Assert.Equal("file.txt", fileName);
    }

    [Fact]
    public void GetFileNameWithoutExtension_ReturnsNameWithoutExtension()
    {
        var path = new FilePath(@"C:\test\file.txt");
        var fileName = path.GetFileNameWithoutExtension();
        Assert.Equal("file", fileName);
    }

    [Fact]
    public void GetExtension_ReturnsExtension()
    {
        var path = new FilePath(@"C:\test\file.txt");
        var extension = path.GetExtension();
        Assert.Equal(".txt", extension);
    }

    [Fact]
    public void ChangeExtension_ChangesExtension()
    {
        var path = new FilePath(@"C:\test\file.txt");
        var newPath = path.ChangeExtension(".md");
        Assert.Equal(@"C:\test\file.md", newPath.Value);
    }

    [Fact]
    public void GetRelativeTo_ReturnsRelativePath()
    {
        var path = new FilePath(@"C:\test\folder\subfolder\file.txt");
        var basePath = new FilePath(@"C:\test");
        var relative = path.GetRelativeTo(basePath);
        Assert.Equal(@"folder\subfolder\file.txt", relative.Value);
    }

    [Fact]
    public void FileExists_ReturnsTrueForExistingFile()
    {
        var path = new FilePath(@"C:\test\file.txt");
        Assert.True(path.FileExists(_fileSystem));
    }

    [Fact]
    public void FileExists_ReturnsFalseForNonExistingFile()
    {
        var path = new FilePath(@"C:\test\nonexistent.txt");
        Assert.False(path.FileExists(_fileSystem));
    }

    [Fact]
    public void DirectoryExists_ReturnsTrueForExistingDirectory()
    {
        var path = new FilePath(@"C:\test");
        Assert.True(path.DirectoryExists(_fileSystem));
    }

    [Fact]
    public void DirectoryExists_ReturnsFalseForNonExistingDirectory()
    {
        var path = new FilePath(@"C:\nonexistent");
        Assert.False(path.DirectoryExists(_fileSystem));
    }

    [Fact]
    public void ToUrlPath_ConvertsToUrlPath()
    {
        var filePath = new FilePath(@"content\blog\post.md");
        var urlPath = filePath.ToUrlPath();
        Assert.Equal("content/blog/post.md", urlPath.Value);
    }

    [Fact]
    public void FromUrlPath_ConvertsFromUrlPath()
    {
        var urlPath = new UrlPath("content/blog/post.md");
        var filePath = FilePath.FromUrlPath(urlPath);
        Assert.Equal(@"content\blog\post.md", filePath.Value);
    }

    [Fact]
    public void EnsureDirectoryExists_CreatesDirectory()
    {
        var path = new FilePath(@"C:\newdir\file.txt");
        path.EnsureDirectoryExists(_fileSystem);
        Assert.True(_fileSystem.Directory.Exists(@"C:\newdir"));
    }

    [Fact]
    public void GetParent_ReturnsParentDirectory()
    {
        var path = new FilePath(@"C:\test\folder\file.txt");
        var parent = path.GetParent();
        Assert.Equal(@"C:\test\folder", parent.Value);
    }

    [Fact]
    public void IsValid_ReturnsTrueForValidPath()
    {
        var path = new FilePath(@"C:\test\file.txt");
        Assert.True(path.IsValid());
    }

    [Fact]
    public void Combine_WithStringSegments()
    {
        var basePath = new FilePath(@"C:\test");
        var result = basePath.Combine("folder", "subfolder", "file.txt");
        Assert.Equal(@"C:\test\folder\subfolder\file.txt", result.Value);
    }

    [Fact]
    public void Equality_ComparesValues()
    {
        var path1 = new FilePath(@"C:\test\file.txt");
        var path2 = new FilePath(@"C:\test\file.txt");
        var path3 = new FilePath(@"C:\test\other.txt");

        Assert.Equal(path1, path2);
        Assert.NotEqual(path1, path3);
        Assert.True(path1 == path2);
        Assert.True(path1 != path3);
    }

    [Fact]
    public void Equality_IsCaseInsensitiveOnWindows()
    {
        if (OperatingSystem.IsWindows())
        {
            var path1 = new FilePath(@"C:\Test\File.txt");
            var path2 = new FilePath(@"c:\test\file.txt");
            Assert.Equal(path1, path2);
        }
    }

    [Fact]
    public void ImplicitConversion_FromString()
    {
        FilePath path = @"C:\test\file.txt";
        Assert.Equal(@"C:\test\file.txt", path.Value);
    }

    [Fact]
    public void ImplicitConversion_ToString()
    {
        var path = new FilePath(@"C:\test\file.txt");
        string value = path;
        Assert.Equal(@"C:\test\file.txt", value);
    }

    [Fact]
    public void TryParse_ValidPath_ReturnsTrue()
    {
        var success = FilePath.TryParse(@"C:\test\file.txt", out var path);
        Assert.True(success);
        Assert.Equal(@"C:\test\file.txt", path!.Value);
    }

    [Fact]
    public void TryParse_NullPath_ReturnsTrue()
    {
        var success = FilePath.TryParse(null, out var path);
        Assert.True(success);
        Assert.Equal("", path!.Value);
    }
}