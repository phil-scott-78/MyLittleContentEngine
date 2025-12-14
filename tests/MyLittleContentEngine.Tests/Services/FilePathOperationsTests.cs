using MyLittleContentEngine.Services;
using Shouldly;
using Testably.Abstractions.Testing;

namespace MyLittleContentEngine.Tests.Services;

public class FilePathOperationsTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly FilePathOperations _filePathOps;

    public FilePathOperationsTests()
    {
        _fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
        _fileSystem
            .Initialize()
            .WithSubdirectory("test")
            .Initialized(s =>
                s.WithFile(@"C:\test\file.txt").Which(f => f.HasStringContent("content"))
            );

        _filePathOps = new FilePathOperations(_fileSystem);
    }

    [Theory]
    [InlineData(@"C:\test\file.txt", true)]
    [InlineData(@"test\file.txt", false)]
    [InlineData("", false)]
    public void IsAbsolute_ReturnsCorrectValue(string input, bool expected)
    {
        var path = new FilePath(input);
        _filePathOps.IsAbsolute(path).ShouldBe(expected);
    }

    [Theory]
    [InlineData(@"C:\test\file.txt", false)]
    [InlineData(@"test\file.txt", true)]
    [InlineData("", true)]
    public void IsRelative_ReturnsCorrectValue(string input, bool expected)
    {
        var path = new FilePath(input);
        _filePathOps.IsRelative(path).ShouldBe(expected);
    }

    [Fact]
    public void Combine_HandlesMultiplePaths()
    {
        var result = _filePathOps.Combine(
            new FilePath("folder"),
            new FilePath("sub-folder"),
            new FilePath("file.txt")
        );
        result.Value.ShouldBe(@"folder\sub-folder\file.txt");
    }

    [Fact]
    public void Combine_WithStringSegments()
    {
        var basePath = new FilePath(@"test");
        var result = _filePathOps.Combine(basePath, "folder", "subfolder", "file.txt");
        result.Value.ShouldBe(@"test\folder\subfolder\file.txt");
    }

    [Fact]
    public void GetDirectory_ReturnsParentDirectory()
    {
        var path = new FilePath(@"test\folder\file.txt");
        var directory = _filePathOps.GetDirectory(path);
        directory.Value.ShouldBe(@"test\folder");
    }

    [Fact]
    public void GetFileName_ReturnsFileName()
    {
        var path = new FilePath(@"test\file.txt");
        var fileName = _filePathOps.GetFileName(path);
        fileName.ShouldBe("file.txt");
    }

    [Fact]
    public void GetFileNameWithoutExtension_ReturnsNameWithoutExtension()
    {
        var path = new FilePath(@"test\file.txt");
        var fileName = _filePathOps.GetFileNameWithoutExtension(path);
        fileName.ShouldBe("file");
    }

    [Fact]
    public void GetExtension_ReturnsExtension()
    {
        var path = new FilePath(@"test\file.txt");
        var extension = _filePathOps.GetExtension(path);
        extension.ShouldBe(".txt");
    }

    [Fact]
    public void ChangeExtension_ChangesExtension()
    {
        var path = new FilePath(@"test\file.txt");
        var newPath = _filePathOps.ChangeExtension(path, ".md");
        newPath.Value.ShouldBe(@"test\file.md");
    }

    [Fact]
    public void GetRelativeTo_ReturnsRelativePath()
    {
        var path = new FilePath(@"test\folder\subfolder\file.txt");
        var basePath = new FilePath(@"test");
        var relative = _filePathOps.GetRelativeTo(path, basePath);
        relative.Value.ShouldBe(@"folder\subfolder\file.txt");
    }

    [Fact]
    public void GetFullPath_ReturnsAbsolutePath()
    {
        var path = new FilePath(@"test\file.txt");
        var fullPath = _filePathOps.GetFullPath(path);
        fullPath.Value.ShouldContain(@"\test\file.txt");
    }

    [Fact]
    public void FromUrlPath_ConvertsFromUrlPath()
    {
        var urlPath = new UrlPath("content/blog/post.md");
        var filePath = _filePathOps.FromUrlPath(urlPath);
        filePath.Value.ShouldBe(@"content\blog\post.md");
    }

    [Fact]
    public void GetParent_ReturnsParentDirectory()
    {
        var path = new FilePath(@"test\folder\file.txt");
        var parent = _filePathOps.GetParent(path);
        parent.Value.ShouldBe(@"test\folder");
    }

    [Fact]
    public void FileExists_ReturnsTrueForExistingFile()
    {
        var path = new FilePath(@"test\file.txt");
        _filePathOps.FileExists(path).ShouldBeTrue();
    }

    [Fact]
    public void FileExists_ReturnsFalseForNonExistingFile()
    {
        var path = new FilePath(@"test\nonexistent.txt");
        _filePathOps.FileExists(path).ShouldBeFalse();
    }

    [Fact]
    public void DirectoryExists_ReturnsTrueForExistingDirectory()
    {
        var path = new FilePath(@"test");
        _filePathOps.DirectoryExists(path).ShouldBeTrue();
    }

    [Fact]
    public void DirectoryExists_ReturnsFalseForNonExistingDirectory()
    {
        var path = new FilePath(@"nonexistent");
        _filePathOps.DirectoryExists(path).ShouldBeFalse();
    }

    [Fact]
    public void EnsureDirectoryExists_CreatesDirectory()
    {
        var path = new FilePath(@"C:\newdir\file.txt");
        _filePathOps.EnsureDirectoryExists(path);
        _fileSystem.Directory.Exists(@"C:\newdir").ShouldBeTrue();
    }

    [Fact]
    public void GetParentDirectory_ReturnsParentDirectory()
    {
        var path = new FilePath(@"C:\test\folder\file.txt");
        var parent = _filePathOps.GetParentDirectory(path);
        parent.Value.ShouldBe(@"C:\test\folder");
    }

    [Fact]
    public void IsValid_ReturnsTrueForValidPath()
    {
        var path = new FilePath(@"test\file.txt");
        _filePathOps.IsValid(path).ShouldBeTrue();
    }
}
