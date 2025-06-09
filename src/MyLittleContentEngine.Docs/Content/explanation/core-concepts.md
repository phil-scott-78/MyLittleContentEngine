---
title: "Core Concepts"
description: "Understanding the fundamental building blocks and philosophy of MyLittleContentEngine"
order: 1
tags:
  - concepts
  - architecture
  - fundamentals
---

MyLittleContentEngine is built around several key concepts that work together to provide a powerful, type-safe content management system for Blazor applications. Understanding these concepts will help you use the framework effectively and extend it for your specific needs.

## Content-First Architecture

Unlike traditional CMS systems that store content in databases, MyLittleContentEngine embraces a content-first approach where:

- **Content is stored as files** in your source control alongside your code
- **Changes are tracked** through Git history
- **Deployment is simplified** with no database dependencies
- **Performance is optimized** through static generation

This approach aligns with modern development practices and provides several benefits:

### Version Control Integration
Every content change is tracked in Git, providing:
- Complete revision history
- Branching and merging for content workflows
- Collaboration through pull requests
- Rollback capabilities

### Developer-Friendly Workflow
Content creators work with familiar tools:
- Text editors with markdown support
- Git workflows for collaboration
- Local development and preview
- Automated deployment pipelines

## Strong Typing Throughout

One of MyLittleContentEngine's core principles is bringing the benefits of C#'s type system to content management.

### Type-Safe Front Matter

Instead of working with dictionaries or dynamic objects, you define strongly-typed models:

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

This provides:
- **Compile-time checking** - Catch errors before deployment
- **IntelliSense support** - Better developer experience
- **Refactoring safety** - Rename properties across your entire codebase
- **Documentation** - Self-documenting through type definitions

### Generic Services

All content services are generic, providing type safety throughout:

```csharp
IContentService<BlogPost>   // Strongly typed for blog posts
IContentService<DocPage>    // Strongly typed for documentation
TagService<BlogPost>        // Tag service specific to blog posts
```

## Content Models and Interfaces

### IFrontMatter Interface

All content types must implement `IFrontMatter`, which defines the minimal contract:

```csharp
public interface IFrontMatter
{
    string Title { get; }
    string? Description { get; }
    List<string> Tags { get; }
}
```

This ensures consistency while allowing flexibility in your custom properties.

### Content Page Model

The `MarkdownContentPage<T>` model represents a complete content item:

```csharp
public class MarkdownContentPage<T> where T : IFrontMatter
{
    public T FrontMatter { get; set; }      // Your typed front matter
    public string Content { get; set; }     // Rendered HTML content
    public string RawContent { get; set; }  // Original markdown
    public string Url { get; set; }         // Generated URL
    public string FilePath { get; set; }    // Source file path
    public DateTime LastModified { get; set; } // File modification time
}
```

This model bridges the gap between file-based content and object-oriented programming.

## Processing Pipeline

Content flows through a well-defined pipeline that transforms markdown files into rich, interactive content.

### Discovery Phase

1. **File Scanning** - The engine scans configured directories for markdown files
2. **Path Resolution** - File paths are mapped to URL structures
3. **Metadata Extraction** - File modification times and other metadata are captured

### Parsing Phase

1. **Front Matter Extraction** - YAML front matter is parsed and validated
2. **Content Separation** - Markdown content is separated from metadata
3. **Type Binding** - Front matter is bound to your strongly-typed models

### Processing Phase

1. **Markdown Transformation** - Markdown is converted to HTML using Markdig
2. **Code Highlighting** - Syntax highlighting is applied to code blocks
3. **Link Processing** - Internal links are resolved and validated
4. **Asset Handling** - Images and other assets are processed

### Enhancement Phase

1. **Table of Contents Generation** - Automatic TOC creation from headers
2. **Cross-Reference Resolution** - Related content is identified
3. **Tag Processing** - Tag relationships are established
4. **Metadata Enrichment** - Additional metadata is calculated

## Service Architecture

MyLittleContentEngine uses a service-oriented architecture with clear separation of concerns.

### Core Services

#### IContentService<T>
The primary interface for content operations:

```csharp
public interface IContentService<T> where T : IFrontMatter
{
    Task<IEnumerable<MarkdownContentPage<T>>> GetAllAsync();
    Task<MarkdownContentPage<T>?> GetByUrlAsync(string url);
    Task<IEnumerable<MarkdownContentPage<T>>> GetByTagAsync(string tag);
    Task<IEnumerable<MarkdownContentPage<T>>> SearchAsync(string query);
}
```

#### TagService<T>
Specialized service for tag-based operations:

```csharp
public class TagService<T> where T : IFrontMatter
{
    Task<IEnumerable<string>> GetAllTagsAsync();
    Task<IEnumerable<MarkdownContentPage<T>>> GetContentByTagAsync(string tag);
    Task<Dictionary<string, int>> GetTagCountsAsync();
}
```

### Dependency Injection Integration

All services are registered with the DI container, making them available throughout your Blazor application:

```csharp
// Registration
builder.Services.AddContentEngineService();
builder.Services.AddContentEngineStaticContentService<BlogPost>();

// Usage in components
@inject IContentService<BlogPost> ContentService
@inject TagService<BlogPost> TagService
```

## Static Generation Model

MyLittleContentEngine supports both dynamic serving and static generation.

### Development Mode

During development, content is served dynamically:
- File changes trigger automatic reloading
- Content is processed on-demand
- Full debugging capabilities are available
- Real-time preview of changes

### Static Generation Mode

For production deployment, content is pre-generated:
- All pages are rendered to static HTML
- Assets are copied and optimized
- SEO files (sitemap, RSS) are generated
- Maximum performance for end users

### Hybrid Approach

The framework seamlessly switches between modes based on context:

```csharp
// This single line handles both scenarios
await app.RunOrBuildContent(args);
```

## Extensibility Points

MyLittleContentEngine is designed to be extended at various points in the pipeline.

### Custom Front Matter Types

Create domain-specific content models:

```csharp
public class Product : IFrontMatter
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    
    // Product-specific properties
    public decimal Price { get; set; }
    public string SKU { get; set; } = string.Empty;
    public List<string> Images { get; set; } = new();
    public bool InStock { get; set; }
}
```

### Custom Processing

Extend the markdown processing pipeline:

```csharp
builder.Services.Configure<MarkdigOptions>(options =>
{
    options.Extensions.Add(new CustomExtension());
});
```

### Custom Routing

Implement custom URL generation logic:

```csharp
public class ProductUrlGenerator : IUrlGenerator<Product>
{
    public string GenerateUrl(MarkdownContentPage<Product> page)
    {
        return $"/products/{page.FrontMatter.SKU}";
    }
}
```

## Design Philosophy

### Convention Over Configuration

MyLittleContentEngine follows sensible defaults while allowing customization:

- **Default directory structures** work out of the box
- **Standard URL patterns** are generated automatically
- **Common workflows** require minimal configuration
- **Custom requirements** can override defaults

### Performance by Design

Performance is built into the architecture:

- **Aggressive caching** at multiple levels
- **Lazy loading** of content when appropriate
- **Static generation** for maximum speed
- **Efficient parsing** and processing

### Developer Experience Focus

Every decision prioritizes developer productivity:

- **Strong typing** prevents runtime errors
- **Clear error messages** aid debugging
- **Hot reload** provides immediate feedback
- **Familiar patterns** from .NET development

## Understanding vs. Implementation

These concepts form the foundation of how MyLittleContentEngine works. They explain:

- **Why** the framework is designed this way
- **How** different components relate to each other
- **What** makes this approach effective for content management

For specific implementation details, see:
- [Tutorials](../tutorials/index.md) for step-by-step learning
- [How-To Guides](../how-to/index.md) for specific tasks
- [Reference](../reference/index.md) for detailed API information

Understanding these core concepts will help you use MyLittleContentEngine effectively and make informed decisions about how to structure your content and extend the framework for your specific needs.