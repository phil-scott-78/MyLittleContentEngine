using System.Collections.Immutable;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;
using System.IO.Abstractions.TestingHelpers;

namespace MyLittleContentEngine.Tests.TestHelpers;

/// <summary>
/// Fluent builder for creating content engine test scenarios with mock file systems and content services.
/// </summary>
/// <remarks>
/// This builder simplifies setting up integration tests by providing a fluent interface
/// for configuring mock file systems, content options, and content services.
/// </remarks>
public class ContentEngineTestBuilder
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly List<(string path, string content)> _markdownFiles = [];
    private readonly List<PageToGenerate> _pages = [];
    private MarkdownContentOptions<TestFrontMatter>? _contentOptions;

    /// <summary>
    /// Adds markdown files to the mock file system.
    /// </summary>
    /// <param name="files">Array of (path, content) tuples representing markdown files.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ContentEngineTestBuilder WithMarkdownFiles(params (string path, string content)[] files)
    {
        foreach (var (path, content) in files)
        {
            _markdownFiles.Add((path, content));
            _fileSystem.AddFile(path, new MockFileData(content));
        }
        return this;
    }

    /// <summary>
    /// Configures the content options for the test scenario.
    /// </summary>
    /// <param name="configure">Action to configure the ContentOptions.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ContentEngineTestBuilder WithContentOptions(Action<MarkdownContentOptions<TestFrontMatter>> configure)
    {
        _contentOptions = new MarkdownContentOptions<TestFrontMatter>
        {
            ContentPath = "/content"
        };
        configure(_contentOptions);
        return this;
    }

    /// <summary>
    /// Adds pre-built pages to the content service.
    /// </summary>
    /// <param name="pages">Array of PageToGenerate instances.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ContentEngineTestBuilder WithPages(params PageToGenerate[] pages)
    {
        _pages.AddRange(pages);
        return this;
    }

    /// <summary>
    /// Builds a mock IContentService with the configured test data.
    /// </summary>
    /// <returns>A mock IContentService for testing.</returns>
    public Task<IContentService> BuildContentServiceAsync()
    {
        var mockContentService = new TestContentService(_pages.ToArray());
        return Task.FromResult<IContentService>(mockContentService);
    }

    /// <summary>
    /// Gets the configured mock file system.
    /// </summary>
    /// <returns>The MockFileSystem instance with configured files.</returns>
    public MockFileSystem GetFileSystem() => _fileSystem;

    /// <summary>
    /// Gets the configured content options.
    /// </summary>
    /// <returns>The ContentOptions instance or null if not configured.</returns>
    public MarkdownContentOptions<TestFrontMatter>? GetContentOptions() => _contentOptions;

    /// <summary>
    /// Creates a builder pre-configured with common test markdown files and corresponding pages.
    /// </summary>
    /// <returns>A ContentEngineTestBuilder with sample markdown files and pages.</returns>
    public static ContentEngineTestBuilder WithSampleContent()
    {
        return new ContentEngineTestBuilder()
            .WithMarkdownFiles(
                ("/content/index.md", """
                    ---
                    title: Home Page
                    order: 1
                    ---
                    # Welcome
                    This is the home page content.
                    """),
                ("/content/about.md", """
                    ---
                    title: About Us
                    order: 2
                    ---
                    # About
                    Learn more about our organization.
                    """),
                ("/content/blog/first-post.md", """
                    ---
                    title: First Blog Post
                    order: 10
                    tags: [intro, welcome]
                    ---
                    # First Post
                    Welcome to our blog!
                    """)
            )
            .WithPages(
                new PageToGenerate("/", "/", new Metadata { Title = "Home Page", Order = 1 }),
                new PageToGenerate("/about", "/about", new Metadata { Title = "About Us", Order = 2 }),
                new PageToGenerate("/blog/first-post", "/blog/first-post", new Metadata { Title = "First Blog Post", Order = 10 })
            );
    }

    /// <summary>
    /// Test implementation of IContentService for use in unit tests.
    /// </summary>
    private class TestContentService(params PageToGenerate[] pages) : IContentService
    {
        private readonly ImmutableList<PageToGenerate> _pages = pages.ToImmutableList();

        public int SearchPriority { get; } = 0;
        public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync() => Task.FromResult(_pages);
        public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() => Task.FromResult(_pages.Where(p => p.Metadata?.Title != null).Select(p => new ContentTocItem(p.Metadata!.Title!, p.Url, p.Metadata.Order, p.Url.Trim('/').Split(['/'], StringSplitOptions.RemoveEmptyEntries))).ToImmutableList());
        public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() => Task.FromResult(ImmutableList<ContentToCopy>.Empty);
        public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() => Task.FromResult(ImmutableList<CrossReference>.Empty);
    }
}

/// <summary>
/// Simple test front matter class for use in test scenarios.
/// </summary>
public class TestFrontMatter : IFrontMatter
{
    public string Title { get; init; } = string.Empty;
    public string[] Tags { get; init; } = [];
    public bool IsDraft { get; init; }
    public string? Uid { get; init; }
    public int Order { get; set; }
    public DateTime? PublishDate { get; set; }
    public string? RedirectUrl { get; init; }
    public string? Section { get; init; }
    
    public Metadata AsMetadata()
    {
        return new Metadata
        {
            Title = Title,
            Order = Order
        };
    }
}