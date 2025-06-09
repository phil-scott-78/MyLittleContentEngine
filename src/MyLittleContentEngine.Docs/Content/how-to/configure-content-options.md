---
title: "Configure Content Options"
description: "Customize content processing behavior, routing, and generation settings"
order: 1
tags:
  - configuration
  - customization
  - setup
---

MyLittleContentEngine provides extensive configuration options to customize how your content is processed, routed, and generated. This guide shows you how to configure the most common options for different scenarios.

## Problem

You need to customize how MyLittleContentEngine processes your content, such as:
- Changing URL patterns
- Setting custom content paths
- Controlling which content is included
- Configuring output generation

## Prerequisites

- A working MyLittleContentEngine project
- Basic understanding of dependency injection in .NET
- Familiarity with the content pipeline

## Core Configuration Options

### ContentEngineOptions

The main engine configuration controls global settings:

```csharp
builder.Services.AddContentEngineService(() => new ContentEngineOptions
{
    BlogTitle = "My Content Site",
    BlogDescription = "Powered by MyLittleContentEngine",
    BaseUrl = "https://mysite.com",
    ContentRootPath = "Content",              // Where to find content files
    OutputPath = "wwwroot",                   // Where to generate static files
    DefaultCulture = "en-US",                 // Default language/culture
    EnableHotReload = true,                   // Enable file watching in dev
    GenerateSitemap = true,                   // Create sitemap.xml
    GenerateRssFeeds = true                   // Create RSS feeds
});
```

### ContentEngineContentOptions<T>

Content-specific options control how each content type is processed:

```csharp
builder.Services.AddContentEngineStaticContentService(() => 
    new ContentEngineContentOptions<BlogPost>()
    {
        ContentPath = "Content/Blog",         // Content directory
        BasePageUrl = "blog",                 // URL prefix
        RouteTemplate = "{year}/{month}/{slug}", // URL pattern
        IncludeDrafts = false,                // Show draft content
        SortByDate = true,                    // Default sort order
        GenerateTagPages = true,              // Create tag listing pages
        GenerateArchivePages = true,          // Create date archives
        PageSize = 10                         // Items per page
    });
```

## Common Configuration Scenarios

### Scenario 1: Documentation Site

For a documentation site with hierarchical organization:

```csharp
// Main engine configuration
builder.Services.AddContentEngineService(() => new ContentEngineOptions
{
    BlogTitle = "API Documentation",
    BlogDescription = "Complete API reference and guides",
    BaseUrl = "https://docs.myapi.com",
    ContentRootPath = "Content"
});

// Documentation content (root level)
builder.Services.AddContentEngineStaticContentService(() => 
    new ContentEngineContentOptions<DocPage>()
    {
        ContentPath = "Content/Docs",
        BasePageUrl = "",                     // Root level URLs
        RouteTemplate = "{slug}",             // Simple slug-based URLs
        IncludeDrafts = Environment.IsDevelopment(),
        SortByDate = false,                   // Sort by order, not date
        GenerateTagPages = true,
        GenerateArchivePages = false          // No date archives for docs
    });
```

### Scenario 2: Multi-Author Blog

For a blog with multiple content types and authors:

```csharp
// Blog posts
builder.Services.AddContentEngineStaticContentService(() => 
    new ContentEngineContentOptions<BlogPost>()
    {
        ContentPath = "Content/Blog",
        BasePageUrl = "blog",
        RouteTemplate = "{year}/{slug}",       // Year-based URLs
        IncludeDrafts = false,
        SortByDate = true,
        GenerateTagPages = true,
        GenerateArchivePages = true,
        GenerateAuthorPages = true            // Author listing pages
    });

// News/announcements
builder.Services.AddContentEngineStaticContentService(() => 
    new ContentEngineContentOptions<NewsPost>()
    {
        ContentPath = "Content/News",
        BasePageUrl = "news",
        RouteTemplate = "{slug}",              // Simple URLs
        IncludeDrafts = false,
        SortByDate = true,
        GenerateTagPages = false,             // No tags for news
        GenerateArchivePages = false
    });
```

### Scenario 3: Portfolio Site

For a portfolio with projects and case studies:

```csharp
builder.Services.AddContentEngineStaticContentService(() => 
    new ContentEngineContentOptions<Project>()
    {
        ContentPath = "Content/Projects",
        BasePageUrl = "work",
        RouteTemplate = "{category}/{slug}",   // Category-based URLs
        IncludeDrafts = false,
        SortByDate = false,                   // Sort by featured/order
        GenerateTagPages = true,              // Technologies used
        GenerateArchivePages = false,
        CustomSortFunction = projects => 
            projects.OrderByDescending(p => p.FrontMatter.IsFeatured)
                   .ThenByDescending(p => p.FrontMatter.Date)
    });
```

## Advanced Configuration

### Custom Route Templates

Route templates support various placeholders:

```csharp
RouteTemplate = "{year}/{month}/{day}/{slug}"     // Full date path
RouteTemplate = "{category}/{subcategory}/{slug}"  // Category hierarchy  
RouteTemplate = "{author}/{year}/{slug}"           // Author-based organization
RouteTemplate = "{slug}"                           // Simple slug only
```

Available placeholders:
- `{slug}` - Generated from filename or title
- `{year}`, `{month}`, `{day}` - From content date
- `{category}`, `{subcategory}` - From directory structure
- `{author}` - From front matter (if available)
- Any custom front matter property

### Environment-Specific Configuration

Use different settings for development vs. production:

```csharp
builder.Services.AddContentEngineService(() => new ContentEngineOptions
{
    BlogTitle = "My Site",
    BaseUrl = builder.Environment.IsDevelopment() 
        ? "https://localhost:5001" 
        : "https://mysite.com",
    IncludeDrafts = builder.Environment.IsDevelopment(),
    EnableHotReload = builder.Environment.IsDevelopment(),
    OutputPath = builder.Environment.IsDevelopment()
        ? "wwwroot/dev"
        : "wwwroot"
});
```

### Configuration from appsettings.json

Store configuration in settings files:

```json
{
  "ContentEngine": {
    "BlogTitle": "My Content Site",
    "BlogDescription": "A site powered by MyLittleContentEngine",
    "BaseUrl": "https://mysite.com",
    "ContentRootPath": "Content",
    "GenerateSitemap": true
  }
}
```

Bind configuration in Program.cs:

```csharp
var contentConfig = builder.Configuration.GetSection("ContentEngine").Get<ContentEngineOptions>();
builder.Services.AddContentEngineService(() => contentConfig);
```

## Content Processing Options

### Custom Content Filtering

Filter content based on custom criteria:

```csharp
builder.Services.AddContentEngineStaticContentService(() => 
    new ContentEngineContentOptions<BlogPost>()
    {
        ContentPath = "Content/Blog",
        ContentFilter = post => 
        {
            // Only include published posts from this year
            return !post.FrontMatter.IsDraft && 
                   post.FrontMatter.Date.Year == DateTime.Now.Year;
        }
    });
```

### Custom Sorting

Define how content should be sorted:

```csharp
builder.Services.AddContentEngineStaticContentService(() => 
    new ContentEngineContentOptions<BlogPost>()
    {
        CustomSortFunction = posts => posts
            .OrderByDescending(p => p.FrontMatter.IsPinned)    // Pinned first
            .ThenByDescending(p => p.FrontMatter.Date)         // Then by date
            .ThenBy(p => p.FrontMatter.Title)                  // Then alphabetically
    });
```

### Content Transformation

Apply custom transformations to content:

```csharp
builder.Services.AddContentEngineStaticContentService(() => 
    new ContentEngineContentOptions<BlogPost>()
    {
        ContentTransformer = (content, frontMatter) => 
        {
            // Add reading time calculation
            var wordCount = content.Split(' ').Length;
            var readingTime = TimeSpan.FromMinutes(wordCount / 200.0);
            
            // Add reading time to front matter
            frontMatter.ReadingTime = readingTime;
            
            return content;
        }
    });
```

## Troubleshooting

### Configuration not taking effect?
- Ensure services are registered before `AddContentEngineStaticContentService`
- Check that configuration is applied in the correct order
- Verify environment-specific conditions are correct

### URLs not generating correctly?
- Check `RouteTemplate` syntax and available placeholders
- Ensure `BasePageUrl` doesn't conflict with other routes
- Verify front matter contains required fields for placeholders

### Content not appearing?
- Check `ContentPath` points to correct directory
- Verify `IncludeDrafts` setting matches your needs
- Ensure `ContentFilter` isn't excluding your content

### Build performance issues?
- Disable `EnableHotReload` in production
- Consider limiting `PageSize` for large content collections
- Use `ContentFilter` to exclude unnecessary content

## Summary

Configuration in MyLittleContentEngine allows you to:

1. ✅ Customize global engine behavior with `ContentEngineOptions`
2. ✅ Control content-specific processing with `ContentEngineContentOptions<T>`
3. ✅ Create custom URL patterns with route templates
4. ✅ Filter and sort content based on your needs
5. ✅ Apply environment-specific settings
6. ✅ Transform content during processing

With these configuration options, you can adapt MyLittleContentEngine to virtually any content management scenario while maintaining the benefits of strong typing and excellent performance.