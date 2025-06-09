---
title: "Getting Started Guide"
description: "Learn the fundamental concepts and workflow of MyLittleContentEngine"
order: 2
tags:
  - tutorial
  - fundamentals
  - concepts
---

Now that you've created your first site, let's dive deeper into MyLittleContentEngine's core concepts and workflow. This guide will help you understand how the framework works and how to use it effectively.

## Core Concepts

### Content Pipeline

MyLittleContentEngine processes your content through a well-defined pipeline:

1. **Discovery** - The engine scans your content directory for markdown files
2. **Parsing** - YAML front matter and markdown content are extracted
3. **Processing** - Markdown is converted to HTML with enhancements
4. **Routing** - URLs are generated based on file paths and configuration
5. **Generation** - Static HTML files are created (in build mode)

### Front Matter

Front matter is YAML metadata at the top of your markdown files. It provides structured data about your content:

```yaml
---
title: "My Blog Post"
description: "A short description"
date: 2025-01-15
tags:
  - example
  - tutorial
isDraft: false
---

Your markdown content starts here...
```

### Content Models

Content models are C# classes that define the structure of your front matter. They must implement `IFrontMatter`:

```csharp
public class BlogPost : IFrontMatter
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsDraft { get; set; }
}
```

### Content Services

The `IContentService<T>` interface provides methods to retrieve and work with your content:

```csharp
@inject IContentService<BlogPost> ContentService

// Get all content
var allPosts = await ContentService.GetAllAsync();

// Get content by tag
var taggedPosts = await ContentService.GetByTagAsync("tutorial");

// Get a specific post
var post = await ContentService.GetByUrlAsync("/blog/my-post");
```

## Directory Structure

MyLittleContentEngine uses convention-based directory organization:

```
Content/
├── Blog/                    # Your content root
│   ├── 2025/
│   │   ├── 01/
│   │   │   ├── welcome.md
│   │   │   └── getting-started.md
│   │   └── 02/
│   │       └── advanced-topics.md
│   └── media/               # Static assets
│       ├── images/
│       └── files/
```

### URL Generation

URLs are generated based on your directory structure:

- `Content/Blog/2025/01/welcome.md` → `/blog/2025/01/welcome`
- `Content/Docs/getting-started.md` → `/docs/getting-started`

You can customize URL generation through configuration:

```csharp
builder.Services.AddContentEngineStaticContentService(() => 
    new ContentEngineContentOptions<BlogPost>()
    {
        ContentPath = "Content/Blog",
        BasePageUrl = "posts",  // Changes URL prefix
        RouteTemplate = "{year}/{month}/{slug}"  // Custom URL pattern
    });
```

## Configuration Options

### Content Engine Options

The main configuration is done through `ContentEngineOptions`:

```csharp
builder.Services.AddContentEngineService(() => new ContentEngineOptions
{
    BlogTitle = "My Site",
    BlogDescription = "A description of my site",
    BaseUrl = "https://mysite.com",
    ContentRootPath = "Content",     // Where to find content
    OutputPath = "wwwroot",          // Where to generate static files
    DefaultCulture = "en-US"
});
```

### Content-Specific Options

Each content type can have its own configuration:

```csharp
builder.Services.AddContentEngineStaticContentService(() => 
    new ContentEngineContentOptions<BlogPost>()
    {
        ContentPath = "Content/Blog",
        BasePageUrl = "blog",
        IncludeDrafts = false,          // Hide draft content
        SortByDate = true,              // Sort posts by date
        GenerateTagPages = true,        // Create tag listing pages
        GenerateArchivePages = true     // Create date-based archives
    });
```

## Working with Tags

Tags provide a powerful way to categorize and cross-reference your content.

### Adding Tags

Add tags to your front matter:

```yaml
---
title: "Learning C#"
tags:
  - csharp
  - programming
  - tutorial
---
```

### Tag Pages

MyLittleContentEngine automatically generates tag pages that list all content with a specific tag. Access them at `/tags/{tag-name}`.

### Using the Tag Service

The `TagService<T>` provides methods to work with tags:

```csharp
@inject TagService<BlogPost> TagService

// Get all tags with post counts
var allTags = await TagService.GetAllTagsAsync();

// Get posts for a specific tag
var csharpPosts = await TagService.GetContentByTagAsync("csharp");

// Get related posts (posts sharing tags)
var relatedPosts = await TagService.GetRelatedContentAsync(currentPost);
```

## Development Workflow

### Development Mode

During development, run your site with hot reload:

```bash
dotnet run
```

This starts a development server that:
- Watches for file changes
- Automatically reloads content
- Provides detailed error messages
- Serves content dynamically

### Build Mode

For production deployment, generate static files:

```bash
dotnet run build
```

This creates:
- Static HTML for all pages
- Copies all assets
- Generates sitemap.xml
- Creates RSS feeds
- Optimizes for performance

### Content Authoring

1. **Create** markdown files in your content directory
2. **Add** proper YAML front matter
3. **Write** content using standard markdown syntax
4. **Preview** in development mode
5. **Build** static files for deployment

## Advanced Features

### Code Highlighting

MyLittleContentEngine supports advanced code highlighting:

```markdown
```csharp
public class Example
{
    public string Property { get; set; }
}
```
```

### Table of Contents

Automatic table of contents generation from your markdown headers:

```csharp
@inject TableOfContentService TocService

var toc = await TocService.GetTableOfContentsAsync(currentPage);
```

### Cross-References

Link between content using relative paths:

```markdown
See also: [Getting Started](../getting-started.md)
```

## Best Practices

### Front Matter

- Always include required fields like `title`
- Use consistent date formats: `2025-01-15`
- Keep descriptions concise but descriptive
- Use lowercase, hyphenated tags: `web-development`

### Content Organization

- Use date-based folders for time-sensitive content: `2025/01/`
- Group related content in logical directories
- Keep media files close to content that uses them
- Use descriptive file names: `getting-started-with-blazor.md`

### Performance

- Use the `isDraft: true` flag during content creation
- Optimize images before adding to your content
- Consider using CDNs for static assets in production
- Monitor build times as your content grows

## Next Steps

Now that you understand the fundamentals:

1. **[Add more blog posts](adding-blog-posts.md)** - Practice content creation
2. **[Explore how-to guides](../how-to/index.md)** - Learn specific techniques
3. **[Understand the architecture](../explanation/core-concepts.md)** - Deeper insights
4. **[Reference documentation](../reference/index.md)** - Detailed API information

## Common Questions

### Can I use custom markdown extensions?
Yes! MyLittleContentEngine is built on Markdig and supports custom extensions.

### How do I handle multiple content types?
Register multiple content services with different front matter types and content paths.

### Can I customize the URL structure?
Absolutely! Use the `RouteTemplate` option in your content configuration.

### Is there a content preview feature?
Yes, development mode provides live preview with hot reload.

### How do I add search functionality?
Search isn't built-in but can be added through client-side search libraries or external services.

You now have a solid understanding of MyLittleContentEngine's core concepts and workflow. Let's move on to creating more content!