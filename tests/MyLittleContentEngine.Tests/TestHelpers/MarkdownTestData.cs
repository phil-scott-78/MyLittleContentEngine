namespace MyLittleContentEngine.Tests.TestHelpers;

/// <summary>
/// Provides pre-built markdown content samples for testing scenarios.
/// </summary>
/// <remarks>
/// This class contains various markdown samples that cover common content patterns,  
/// front matter configurations, and edge cases useful for testing content processing.
/// </remarks>
public static class MarkdownTestData
{
    /// <summary>
    /// Simple markdown with basic front matter.
    /// </summary>
    public static readonly string SimplePost = """
        ---
        title: Simple Test Post
        order: 1
        ---
        # Simple Post
        
        This is a basic markdown post with minimal front matter.
        
        ## Subsection
        
        Some content here.
        """;

    /// <summary>
    /// Markdown with comprehensive front matter including tags and dates.
    /// </summary>
    public static readonly string RichPost = """
        ---
        title: Rich Content Post
        order: 5
        tags: [testing, markdown, sample]
        publishDate: 2024-01-15
        isDraft: false
        ---
        # Rich Content Example
        
        This post demonstrates various markdown features and rich front matter.
        
        ## Code Blocks
        
        ```csharp
        public class Example
        {
            public string Property { get; set; } = "test";
        }
        ```
        
        ## Lists
        
        - Item 1
        - Item 2  
        - Item 3
        
        ## Links and Images
        
        Check out [this link](https://example.com) for more info.
        """;

    /// <summary>
    /// Markdown with complex nested content and multiple headings.
    /// </summary>
    public static readonly string ComplexPost = """
        ---
        title: Complex Document Structure
        order: 10
        tags: [complex, structure, headings]
        ---
        # Main Title
        
        Introduction paragraph with some content.
        
        ## Section 1: Getting Started
        
        Content for the first section.
        
        ### Subsection 1.1
        
        Nested content here.
        
        ### Subsection 1.2
        
        More nested content.
        
        ## Section 2: Advanced Topics
        
        Advanced content discussion.
        
        ### Code Examples
        
        ```typescript
        interface TestInterface {
          prop: string;
          method(): void;
        }
        ```
        
        ### Best Practices
        
        1. Always validate input
        2. Handle errors gracefully
        3. Write comprehensive tests
        
        ## Conclusion
        
        Final thoughts and summary.
        """;

    /// <summary>
    /// Markdown with minimal front matter (edge case).
    /// </summary>
    public static readonly string MinimalPost = """
        ---
        title: Minimal
        ---
        # Minimal Content
        
        Just the basics.
        """;

    /// <summary>
    /// Markdown without any front matter (edge case).
    /// </summary>
    public static readonly string NoFrontMatterPost = """
        # Post Without Front Matter
        
        This markdown has no YAML front matter block.
        
        It should still be processed correctly.
        """;

    /// <summary>
    /// Draft post that should be excluded from generation.
    /// </summary>
    public static readonly string DraftPost = """
        ---
        title: Draft Post  
        order: 999
        isDraft: true
        ---
        # Work in Progress
        
        This is a draft post that shouldn't be published.
        """;

    /// <summary>
    /// Post with special characters and formatting.
    /// </summary>
    public static readonly string SpecialCharactersPost = """
        ---
        title: "Special Characters & Formatting"
        order: 3
        tags: ["special-chars", "formatting"]  
        ---
        # Special Characters Test
        
        Testing various special characters:
        
        - Quotes: "double" and 'single'
        - Symbols: @#$%^&*()
        - Unicode: 📝 🚀 ✨
        
        ## Code with Special Chars
        
        ```javascript
        const obj = { 
          "key-with-dashes": "value",
          'single-quotes': `template ${literal}`
        };
        ```
        
        **Bold** and *italic* and ***both***.
        """;

    /// <summary>
    /// API documentation style post.
    /// </summary>
    public static readonly string ApiDocPost = """
        ---
        title: API Reference
        order: 100
        tags: [api, reference, documentation]
        ---
        # ContentService API
        
        ## Methods
        
        ### GetPagesToGenerateAsync()
        
        Returns a list of all pages to be generated.
        
        **Parameters:** None
        
        **Returns:** `Task<ImmutableList<PageToGenerate>>`
        
        **Example:**
        
        ```csharp
        var service = new ContentService();
        var pages = await service.GetPagesToGenerateAsync();
        ```
        
        ### GetContentToCopyAsync()
        
        Returns static content files to copy.
        
        **Returns:** `Task<ImmutableList<ContentToCopy>>`
        """;

    /// <summary>
    /// Returns a collection of sample markdown files with their intended paths.
    /// </summary>
    public static readonly IReadOnlyList<(string path, string content)> SampleFiles = new[]
    {
        ("/content/index.md", SimplePost),
        ("/content/blog/rich-example.md", RichPost),
        ("/content/docs/complex-guide.md", ComplexPost),
        ("/content/about.md", MinimalPost),
        ("/content/drafts/work-in-progress.md", DraftPost),
        ("/content/blog/special-chars.md", SpecialCharactersPost),
        ("/content/api/reference.md", ApiDocPost)
    };

    /// <summary>
    /// Returns sample files filtered to only published content (non-drafts).
    /// </summary>
    public static IEnumerable<(string path, string content)> PublishedFiles =>
        SampleFiles.Where(f => !f.content.Contains("isDraft: true"));

    /// <summary>
    /// Returns sample files that should be considered drafts.
    /// </summary>
    public static IEnumerable<(string path, string content)> DraftFiles =>
        SampleFiles.Where(f => f.content.Contains("isDraft: true"));

    /// <summary>
    /// Creates a custom markdown post with specified parameters.
    /// </summary>
    /// <param name="title">The post title.</param>
    /// <param name="order">The post order.</param>
    /// <param name="content">The markdown content body.</param>
    /// <param name="tags">Optional tags array.</param>
    /// <param name="isDraft">Whether the post is a draft.</param>
    /// <returns>Complete markdown with front matter.</returns>
    public static string CreatePost(
        string title, 
        int order, 
        string content, 
        string[]? tags = null,
        bool isDraft = false)
    {
        var frontMatter = $"""
            ---
            title: {title}
            order: {order}
            """;

        if (tags is { Length: > 0 })
        {
            frontMatter += $"""

                            tags: [{string.Join(", ", tags.Select(t => $"""
                                                                        "{t}"
                                                                        """))}]
                            """;
        }

        if (isDraft)
        {
            frontMatter += """

                           isDraft: true
                           """;
        }

        frontMatter += """

                       ---

                       """;

        return frontMatter + content;
    }
}