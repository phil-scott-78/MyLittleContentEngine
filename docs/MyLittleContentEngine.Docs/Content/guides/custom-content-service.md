---
title: "Create Custom Content Service"
description: "Implement a custom IContentService to handle specialized content sources and processing requirements"
order: 2100
---

This guide shows you how to create a custom `IContentService` implementation to integrate specialized content sources
with MyLittleContentEngine. Custom content services are useful when you need to pull content from databases, APIs, or
other non-file sources.

## Understanding IContentService

The `IContentService` interface defines how MyLittleContentEngine discovers and processes content. It provides four key
methods:

- `GetPagesToGenerateAsync()` - Returns all pages that should be generated
- `GetTocEntriesToGenerateAsync()` - Returns pages that should appear in navigation
- `GetContentToCopyAsync()` - Returns static assets to copy
- `GetCrossReferencesAsync()` - Returns cross-references for linking

`MarkdownContentService<TFrontMatter>` and `ApiReferenceContentService` are both built-in implementations of `IContentService` 
that handle Markdown files and API references, respectively. You can create your own implementation to handle content
from other sources, such as a database or an external API. Keep in mind that during development, the content service might
look dynamic, but it is designed to be static for production builds. 

## Basic Implementation

Here's a minimal custom content service that loads content from a database:

```csharp
using System.Collections.Immutable;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;

public class DatabaseContentService : IContentService
{
    private readonly IDbContext _dbContext;
    private readonly IMarkdownProcessor _markdownProcessor;

    public DatabaseContentService(IDbContext dbContext, IMarkdownProcessor markdownProcessor)
    {
        _dbContext = dbContext;
        _markdownProcessor = markdownProcessor;
    }

    public async Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync()
    {
        var articles = await _dbContext.Articles
            .Where(a => a.IsPublished)
            .OrderByDescending(a => a.PublishedDate)
            .ToListAsync();

        var pages = articles.Select(article => new PageToGenerate
        {
            Url = $"/articles/{article.Slug}",
            Title = article.Title,
            Content = _markdownProcessor.ToHtml(article.Content),
            FrontMatter = new ArticleFrontMatter
            {
                Title = article.Title,
                Description = article.Summary,
                Tags = article.Tags?.Split(',') ?? [],
                PublishedDate = article.PublishedDate
            }
        }).ToImmutableList();

        return pages;
    }

    public async Task<ImmutableList<PageToGenerate>> GetTocEntriesToGenerateAsync()
    {
        // Only show the articles index page in navigation
        return [new PageToGenerate
        {
            Url = "/articles",
            Title = "Articles",
            Content = "<p>Browse our articles</p>",
            FrontMatter = new ArticleFrontMatter
            {
                Title = "Articles",
                Description = "Browse our collection of articles"
            }
        }];
    }

    public async Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync()
    {
        // Copy any uploaded images from the database to the output
        var images = await _dbContext.ArticleImages.ToListAsync();
        
        return images.Select(img => new ContentToCopy
        {
            SourcePath = img.FilePath,
            DestinationPath = $"images/{img.FileName}"
        }).ToImmutableList();
    }

    public async Task<ImmutableList<CrossReference>> GetCrossReferencesAsync()
    {
        var articles = await _dbContext.Articles.ToListAsync();
        
        return articles.Select(article => new CrossReference
        {
            Id = article.Id.ToString(),
            Title = article.Title,
            Url = $"/articles/{article.Slug}",
            Type = "article"
        }).ToImmutableList();
    }
}
```

## Service Registration

Register your custom content service in `Program.cs`, both when the concrete type is used and when it is registered as
an `IContentService`. You can use this pattern to ensure the same instance is used throughout the application:

```csharp
builder.Services.AddSingleton<DatabaseContentService>();

// Register as IContentService (this allows multiple IContentService implementations)
builder.Services.AddSingleton<IContentService>(provider => provider.GetRequiredService<DatabaseContentService>());
```

For multiple content services, the framework will combine results from all registered services for site generation and 
the table of contents.

## Advanced Implementation Features

### Caching for Performance

For content services that make expensive operations (API calls, database queries),
consider implementing caching using [`LazyAndForgetful<T>`](../under-the-hood/hot-reload-architecture). The built in
`IContentService` implementations use this pattern to cache results until a trigger occurs that requires a refresh.

### Content Transformation

Transform external content formats into HTML on demand rather than at loading time. For larger sites, the development
experience can be improved by only gathering the data needed to return the data for the `IContentService` methods. These
are needed for things such as site-wide navigation, cross-references, and static assets. If there is work that is only
presentation, wait until the user requests that to speed up the initial load time.

## Performance Considerations

- **Lazy Loading**: Only load content when needed
- **Parallel Processing**: Use `Task.WhenAll()` for independent operations
- **Memory Management**: Dispose of resources properly

