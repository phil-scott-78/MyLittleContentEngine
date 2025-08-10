using System.IO.Abstractions.TestingHelpers;
using MyLittleContentEngine.Services;
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
        var filePath = new FilePath("/content/my-blog-post.md");
        var baseContentPath = new FilePath("/content");

        var result = pathUtilities.FilePathToUrlPath(filePath, baseContentPath);

        result.Value.ShouldBe("my-blog-post");
    }

    [Fact]
    public void FilePathToUrlPath_FileInSubdirectory_PreservesDirectoryStructure()
    {
        var fileSystem = new MockFileSystem();
        var pathUtilities = new FileSystemUtilities(fileSystem);
        var filePath = new FilePath("/content/blog/my-first-post.md");
        var baseContentPath = new FilePath("/content");

        var result = pathUtilities.FilePathToUrlPath(filePath, baseContentPath);

        result.Value.ShouldBe("blog/my-first-post");
    }

    [Fact]
    public void FilePathToUrlPath_NestedSubdirectories_PreservesFullPath()
    {
        var fileSystem = new MockFileSystem();
        var pathUtilities = new FileSystemUtilities(fileSystem);
        var filePath = new FilePath("/content/docs/guides/getting-started.md");
        var baseContentPath = new FilePath("/content");

        var result = pathUtilities.FilePathToUrlPath(filePath, baseContentPath);

        result.Value.ShouldBe("docs/guides/getting-started");
    }

    [Fact]
    public void FilePathToUrlPath_FileWithSpecialCharacters_SlugifiesFilename()
    {
        var fileSystem = new MockFileSystem();
        var pathUtilities = new FileSystemUtilities(fileSystem);
        var filePath = new FilePath("/content/My Blog Post With Spaces!.md");
        var baseContentPath = new FilePath("/content");

        var result = pathUtilities.FilePathToUrlPath(filePath, baseContentPath);

        result.Value.ShouldBe("my-blog-post-with-spaces");
    }

    [Fact]
    public void FilePathToUrlPath_WindowsPathSeparators_ConvertsToForwardSlashes()
    {
        var fileSystem = new MockFileSystem();
        var pathUtilities = new FileSystemUtilities(fileSystem);
        var filePath = new FilePath("/content/blog/posts/article.md");
        var baseContentPath = new FilePath("/content");

        var result = pathUtilities.FilePathToUrlPath(filePath, baseContentPath);

        result.Value.ShouldBe("blog/posts/article");
    }

    [Theory]
    [InlineData("https://example.com", "", "https://example.com")]
    [InlineData("https://example.com", "   ", "https://example.com")]
    public void CombineUrl_EmptyRelativePath_ReturnsBaseUrl(string baseUrl, string relativePath, string expected)
    {
        var result = FileSystemUtilities.CombineUrl(new UrlPath(baseUrl), new UrlPath(relativePath));
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public void CombineUrl_FragmentPath_AppendsDirectlyToBaseUrl()
    {
        var result = FileSystemUtilities.CombineUrl(new UrlPath("https://example.com/page"), new UrlPath("#section"));
        result.Value.ShouldBe("https://example.com/page#section");
    }

    [Fact]
    public void CombineUrl_QueryPath_AppendsDirectlyToBaseUrl()
    {
        var result = FileSystemUtilities.CombineUrl(new UrlPath("https://example.com/page"), new UrlPath("?param=value"));
        result.Value.ShouldBe("https://example.com/page?param=value");
    }

    [Fact]
    public void CombineUrl_BaseUrlWithTrailingSlash_CombinesCorrectly()
    {
        var result = FileSystemUtilities.CombineUrl(new UrlPath("https://example.com/"), new UrlPath("path/to/page"));
        result.Value.ShouldBe("https://example.com/path/to/page");
    }

    [Fact]
    public void CombineUrl_BaseUrlWithoutTrailingSlash_CombinesCorrectly()
    {
        var result = FileSystemUtilities.CombineUrl(new UrlPath("https://example.com"), new UrlPath("path/to/page"));
        result.Value.ShouldBe("https://example.com/path/to/page");
    }

    [Fact]
    public void CombineUrl_RelativePathWithLeadingSlash_CombinesCorrectly()
    {
        var result = FileSystemUtilities.CombineUrl(new UrlPath("https://example.com"), new UrlPath("/path/to/page"));
        result.Value.ShouldBe("https://example.com/path/to/page");
    }

    [Fact]
    public void CombineUrl_BothPathsHaveSlashes_CombinesCorrectly()
    {
        var result = FileSystemUtilities.CombineUrl(new UrlPath("https://example.com/"), new UrlPath("/path/to/page"));
        result.Value.ShouldBe("https://example.com/path/to/page");
    }

    [Fact]
    public void CombineUrl_RelativePathWithTrailingSlashes_TrimsCorrectly()
    {
        var result = FileSystemUtilities.CombineUrl(new UrlPath("https://example.com"), new UrlPath("path/to/page/"));
        result.Value.ShouldBe("https://example.com/path/to/page");
    }


    [Fact]
    public void ValidateDirectoryPath_NonExistingDirectory_ThrowsDirectoryNotFoundException()
    {
        var fileSystem = new MockFileSystem();
        var pathUtilities = new FileSystemUtilities(fileSystem);

        Should.Throw<DirectoryNotFoundException>(() => 
            pathUtilities.ValidateDirectoryPath(new FilePath("/nonexistent")));
    }

    [Fact]
    public void Combine_EmptyPaths_HandlesGracefully()
    {
        var fileSystem = new MockFileSystem();
        var pathUtilities = new FileSystemUtilities(fileSystem);

        var result = pathUtilities.Combine(new FilePath(""), new FilePath("relative/path"));

        result.Value.ShouldBe("relative/path");
    }

    [Fact]
    public void FilePathToUrlPath_FileInRootDirectory_ReturnsJustFilename()
    {
        var fileSystem = new MockFileSystem();
        var pathUtilities = new FileSystemUtilities(fileSystem);
        var filePath = new FilePath("/content/index.md");
        var baseContentPath = new FilePath("/content");

        var result = pathUtilities.FilePathToUrlPath(filePath, baseContentPath);

        result.Value.ShouldBe("index");
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

        var (files, absolutePath) = pathUtilities.GetFilesInDirectory(new FilePath("/content"), "*.md", recursive: false);

        files.ShouldContain(s => s.Value.Contains("file1.md"));
        files.ShouldContain(s => s.Value.Contains("file2.md"));
        files.ShouldNotContain(s => s.Value.Contains("file3.md"));
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

        var (files, absolutePath) = pathUtilities.GetFilesInDirectory(new FilePath("/content"), "*.md", recursive: true);

        files.ShouldContain(s => s.Value.Contains("file1.md"));
        files.ShouldContain(s => s.Value.Contains("file2.md"));
        files.ShouldContain(s => s.Value.Contains("file3.md"));
    }
    
    [Fact]
    public void FilePathToUrlPath_ComplexPath_HandlesCorrectly()
    {
        var fileSystem = new MockFileSystem();
        var pathUtilities = new FileSystemUtilities(fileSystem);
        var filePath = new FilePath("/var/www/content/blog/2023/my-awesome-post.md");
        var baseContentPath = new FilePath("/var/www/content");

        var result = pathUtilities.FilePathToUrlPath(filePath, baseContentPath);

        result.Value.ShouldBe("blog/2023/my-awesome-post");
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

        var (files, absolutePath) = pathUtilities.GetFilesInDirectory(new FilePath("/content"), "*.md;*.mdx", recursive: true);

        files.Length.ShouldBe(4);
        files.ShouldContain(s => s.Value.Contains("file1.md"));
        files.ShouldContain(s => s.Value.Contains("file2.mdx"));
        files.ShouldContain(s => s.Value.Contains("file4.md"));
        files.ShouldContain(s => s.Value.Contains("file5.mdx"));
        files.ShouldNotContain(s => s.Value.Contains("file3.txt"));
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

        var (files, absolutePath) = pathUtilities.GetFilesInDirectory(new FilePath("/content"), "*.md;*.md;*.mdx", recursive: true);

        files.Length.ShouldBe(2);
        files.ShouldContain(s => s.Value.Contains("file1.md"));
        files.ShouldContain(s => s.Value.Contains("file2.mdx"));
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

        var (files, absolutePath) = pathUtilities.GetFilesInDirectory(new FilePath("/content"), " *.md ; *.mdx ", recursive: true);

        files.Length.ShouldBe(2);
        files.ShouldContain(s => s.Value.Contains("file1.md"));
        files.ShouldContain(s => s.Value.Contains("file2.mdx"));
        files.ShouldNotContain(s => s.Value.Contains("file3.txt"));
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

        var (files, absolutePath) = pathUtilities.GetFilesInDirectory(new FilePath("/content"), "*.md", recursive: true);

        files.Length.ShouldBe(1);
        files.ShouldContain(s => s.Value.Contains("file1.md"));
        files.ShouldNotContain(s => s.Value.Contains("file2.mdx"));
    }
}