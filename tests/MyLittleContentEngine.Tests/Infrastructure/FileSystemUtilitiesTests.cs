using System.IO.Abstractions.TestingHelpers;
using MyLittleContentEngine.Services.Infrastructure;
using Shouldly;

namespace MyLittleContentEngine.Tests.Infrastructure;

public class FileSystemUtilitiesTests
{
    [Fact]
    public void FilePathToUrlPath_SimpleFile_ReturnsSlugifiedName()
    {
        var fileSystem = new MockFileSystem();
        var pathUtilities = new FileSystemUtilities(fileSystem);
        var filePath = "/content/my-blog-post.md";
        var baseContentPath = "/content";

        var result = pathUtilities.FilePathToUrlPath(filePath, baseContentPath);

        result.ShouldBe("my-blog-post");
    }

    [Fact]
    public void FilePathToUrlPath_FileInSubdirectory_PreservesDirectoryStructure()
    {
        var fileSystem = new MockFileSystem();
        var pathUtilities = new FileSystemUtilities(fileSystem);
        var filePath = "/content/blog/my-first-post.md";
        var baseContentPath = "/content";

        var result = pathUtilities.FilePathToUrlPath(filePath, baseContentPath);

        result.ShouldBe("blog/my-first-post");
    }

    [Fact]
    public void FilePathToUrlPath_NestedSubdirectories_PreservesFullPath()
    {
        var fileSystem = new MockFileSystem();
        var pathUtilities = new FileSystemUtilities(fileSystem);
        var filePath = "/content/docs/guides/getting-started.md";
        var baseContentPath = "/content";

        var result = pathUtilities.FilePathToUrlPath(filePath, baseContentPath);

        result.ShouldBe("docs/guides/getting-started");
    }

    [Fact]
    public void FilePathToUrlPath_FileWithSpecialCharacters_SlugifiesFilename()
    {
        var fileSystem = new MockFileSystem();
        var pathUtilities = new FileSystemUtilities(fileSystem);
        var filePath = "/content/My Blog Post With Spaces!.md";
        var baseContentPath = "/content";

        var result = pathUtilities.FilePathToUrlPath(filePath, baseContentPath);

        result.ShouldBe("my-blog-post-with-spaces");
    }

    [Fact]
    public void FilePathToUrlPath_WindowsPathSeparators_ConvertsToForwardSlashes()
    {
        var fileSystem = new MockFileSystem();
        var pathUtilities = new FileSystemUtilities(fileSystem);
        var filePath = "/content/blog/posts/article.md";
        var baseContentPath = "/content";

        var result = pathUtilities.FilePathToUrlPath(filePath, baseContentPath);

        result.ShouldBe("blog/posts/article");
    }

    [Theory]
    [InlineData("https://example.com", "", "https://example.com")]
    [InlineData("https://example.com", "   ", "https://example.com")]
    public void CombineUrl_EmptyRelativePath_ReturnsBaseUrl(string baseUrl, string relativePath, string expected)
    {
        var result = FileSystemUtilities.CombineUrl(baseUrl, relativePath);
        result.ShouldBe(expected);
    }

    [Fact]
    public void CombineUrl_FragmentPath_AppendsDirectlyToBaseUrl()
    {
        var result = FileSystemUtilities.CombineUrl("https://example.com/page", "#section");
        result.ShouldBe("https://example.com/page#section");
    }

    [Fact]
    public void CombineUrl_QueryPath_AppendsDirectlyToBaseUrl()
    {
        var result = FileSystemUtilities.CombineUrl("https://example.com/page", "?param=value");
        result.ShouldBe("https://example.com/page?param=value");
    }

    [Fact]
    public void CombineUrl_BaseUrlWithTrailingSlash_CombinesCorrectly()
    {
        var result = FileSystemUtilities.CombineUrl("https://example.com/", "path/to/page");
        result.ShouldBe("https://example.com/path/to/page");
    }

    [Fact]
    public void CombineUrl_BaseUrlWithoutTrailingSlash_CombinesCorrectly()
    {
        var result = FileSystemUtilities.CombineUrl("https://example.com", "path/to/page");
        result.ShouldBe("https://example.com/path/to/page");
    }

    [Fact]
    public void CombineUrl_RelativePathWithLeadingSlash_CombinesCorrectly()
    {
        var result = FileSystemUtilities.CombineUrl("https://example.com", "/path/to/page");
        result.ShouldBe("https://example.com/path/to/page");
    }

    [Fact]
    public void CombineUrl_BothPathsHaveSlashes_CombinesCorrectly()
    {
        var result = FileSystemUtilities.CombineUrl("https://example.com/", "/path/to/page");
        result.ShouldBe("https://example.com/path/to/page");
    }

    [Fact]
    public void CombineUrl_RelativePathWithTrailingSlashes_TrimsCorrectly()
    {
        var result = FileSystemUtilities.CombineUrl("https://example.com", "path/to/page/");
        result.ShouldBe("https://example.com/path/to/page");
    }


    [Fact]
    public void ValidateDirectoryPath_NonExistingDirectory_ThrowsDirectoryNotFoundException()
    {
        var fileSystem = new MockFileSystem();
        var pathUtilities = new FileSystemUtilities(fileSystem);

        Should.Throw<DirectoryNotFoundException>(() => 
            pathUtilities.ValidateDirectoryPath("/nonexistent"));
    }

    [Fact]
    public void Combine_EmptyPaths_HandlesGracefully()
    {
        var fileSystem = new MockFileSystem();
        var pathUtilities = new FileSystemUtilities(fileSystem);

        var result = pathUtilities.Combine("", "relative/path");

        result.ShouldBe("relative/path");
    }

    [Fact]
    public void FilePathToUrlPath_FileInRootDirectory_ReturnsJustFilename()
    {
        var fileSystem = new MockFileSystem();
        var pathUtilities = new FileSystemUtilities(fileSystem);
        var filePath = "/content/index.md";
        var baseContentPath = "/content";

        var result = pathUtilities.FilePathToUrlPath(filePath, baseContentPath);

        result.ShouldBe("index");
    }

    
    [Fact]
    public void GetFilesInDirectory_NonRecursive_OnlyReturnsTopLevelFiles()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/content/file1.md", new MockFileData("content1") },
            { "/content/file2.md", new MockFileData("content2") },
            { "/content/subdirectory/file3.md", new MockFileData("content3") }
        });
        var pathUtilities = new FileSystemUtilities(fileSystem);

        var (files, absolutePath) = pathUtilities.GetFilesInDirectory("/content", "*.md", recursive: false);

        files.ShouldContain(s => s.Contains("file1.md"));
        files.ShouldContain(s => s.Contains("file2.md"));
        files.ShouldNotContain(s => s.Contains("file3.md"));
    }
    
    [Fact]
    public void GetFilesInDirectory_Recursive_OnlyReturnsAllFiles()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/content/file1.md", new MockFileData("content1") },
            { "/content/file2.md", new MockFileData("content2") },
            { "/content/subdirectory/file3.md", new MockFileData("content3") }
        });
        var pathUtilities = new FileSystemUtilities(fileSystem);

        var (files, absolutePath) = pathUtilities.GetFilesInDirectory("/content", "*.md", recursive: true);

        files.ShouldContain(s => s.Contains("file1.md"));
        files.ShouldContain(s => s.Contains("file2.md"));
        files.ShouldContain(s => s.Contains("file3.md"));
    }
    
    [Fact]
    public void FilePathToUrlPath_ComplexPath_HandlesCorrectly()
    {
        var fileSystem = new MockFileSystem();
        var pathUtilities = new FileSystemUtilities(fileSystem);
        var filePath = "/var/www/content/blog/2023/my-awesome-post.md";
        var baseContentPath = "/var/www/content";

        var result = pathUtilities.FilePathToUrlPath(filePath, baseContentPath);

        result.ShouldBe("blog/2023/my-awesome-post");
    }

    [Fact]
    public void GetFilesInDirectory_MultiplePatterns_ReturnsAllMatchingFiles()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/content/file1.md", new MockFileData("content1") },
            { "/content/file2.mdx", new MockFileData("content2") },
            { "/content/file3.txt", new MockFileData("content3") },
            { "/content/subdirectory/file4.md", new MockFileData("content4") },
            { "/content/subdirectory/file5.mdx", new MockFileData("content5") }
        });
        var pathUtilities = new FileSystemUtilities(fileSystem);

        var (files, absolutePath) = pathUtilities.GetFilesInDirectory("/content", "*.md;*.mdx", recursive: true);

        files.Length.ShouldBe(4);
        files.ShouldContain(s => s.Contains("file1.md"));
        files.ShouldContain(s => s.Contains("file2.mdx"));
        files.ShouldContain(s => s.Contains("file4.md"));
        files.ShouldContain(s => s.Contains("file5.mdx"));
        files.ShouldNotContain(s => s.Contains("file3.txt"));
    }

    [Fact]
    public void GetFilesInDirectory_MultiplePatterns_RemovesDuplicates()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/content/file1.md", new MockFileData("content1") },
            { "/content/file2.mdx", new MockFileData("content2") }
        });
        var pathUtilities = new FileSystemUtilities(fileSystem);

        var (files, absolutePath) = pathUtilities.GetFilesInDirectory("/content", "*.md;*.md;*.mdx", recursive: true);

        files.Length.ShouldBe(2);
        files.ShouldContain(s => s.Contains("file1.md"));
        files.ShouldContain(s => s.Contains("file2.mdx"));
    }

    [Fact]
    public void GetFilesInDirectory_MultiplePatternsWithSpaces_HandlesCorrectly()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/content/file1.md", new MockFileData("content1") },
            { "/content/file2.mdx", new MockFileData("content2") },
            { "/content/file3.txt", new MockFileData("content3") }
        });
        var pathUtilities = new FileSystemUtilities(fileSystem);

        var (files, absolutePath) = pathUtilities.GetFilesInDirectory("/content", " *.md ; *.mdx ", recursive: true);

        files.Length.ShouldBe(2);
        files.ShouldContain(s => s.Contains("file1.md"));
        files.ShouldContain(s => s.Contains("file2.mdx"));
        files.ShouldNotContain(s => s.Contains("file3.txt"));
    }

    [Fact]
    public void GetFilesInDirectory_SinglePattern_WorksAsExpected()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "/content/file1.md", new MockFileData("content1") },
            { "/content/file2.mdx", new MockFileData("content2") }
        });
        var pathUtilities = new FileSystemUtilities(fileSystem);

        var (files, absolutePath) = pathUtilities.GetFilesInDirectory("/content", "*.md", recursive: true);

        files.Length.ShouldBe(1);
        files.ShouldContain(s => s.Contains("file1.md"));
        files.ShouldNotContain(s => s.Contains("file2.mdx"));
    }
}