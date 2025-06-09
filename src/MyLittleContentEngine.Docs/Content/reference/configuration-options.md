---
title: "Configuration Options"
description: "Complete reference for all configuration options and settings"
order: 1
tags:
  - reference
  - configuration
  - api
---

This reference provides comprehensive documentation for all configuration options available in MyLittleContentEngine. Configuration is handled through strongly-typed options classes that integrate with .NET's configuration system.

## ContentEngineOptions

The primary configuration class that controls global engine behavior.

### Properties

#### BlogTitle
**Type:** `string`  
**Required:** Yes  
**Description:** The title of your site, used in metadata and RSS feeds.

```csharp
BlogTitle = "My Awesome Blog"
```

#### BlogDescription
**Type:** `string?`  
**Required:** No  
**Description:** A description of your site, used in metadata and RSS feeds.

```csharp
BlogDescription = "A blog about web development and .NET"
```

#### BaseUrl
**Type:** `string`  
**Required:** Yes  
**Description:** The base URL of your site, used for generating absolute URLs in sitemaps and RSS feeds.

```csharp
BaseUrl = "https://mysite.com"
```

#### ContentRootPath
**Type:** `string`  
**Default:** `"Content"`  
**Description:** The root directory where content files are located, relative to the application root.

```csharp
ContentRootPath = "Content"
ContentRootPath = "src/content"  // Custom path
```

#### OutputPath
**Type:** `string`  
**Default:** `"wwwroot"`  
**Description:** The directory where static files are generated during build.

```csharp
OutputPath = "wwwroot"
OutputPath = "dist"  // Custom output directory
```

#### DefaultCulture
**Type:** `string?`  
**Default:** `"en-US"`  
**Description:** The default culture for content processing and date formatting.

```csharp
DefaultCulture = "en-US"
DefaultCulture = "de-DE"  // German culture
```

#### EnableHotReload
**Type:** `bool`  
**Default:** `true`  
**Description:** Whether to enable file watching and hot reload during development.

```csharp
EnableHotReload = true   // Enable for development
EnableHotReload = false  // Disable for production
```

#### GenerateSitemap
**Type:** `bool`  
**Default:** `true`  
**Description:** Whether to generate a sitemap.xml file during static generation.

```csharp
GenerateSitemap = true   // Generate sitemap
GenerateSitemap = false  // Skip sitemap generation
```

#### GenerateRssFeeds
**Type:** `bool`  
**Default:** `true`  
**Description:** Whether to generate RSS feeds for content.

```csharp
GenerateRssFeeds = true   // Generate RSS feeds
GenerateRssFeeds = false  // Skip RSS generation
```

### Usage Example

```csharp
builder.Services.AddContentEngineService(() => new ContentEngineOptions
{
    BlogTitle = "Tech Insights",
    BlogDescription = "Deep dives into modern web development",
    BaseUrl = "https://techinsights.dev",
    ContentRootPath = "Content",
    OutputPath = "wwwroot",
    DefaultCulture = "en-US",
    EnableHotReload = builder.Environment.IsDevelopment(),
    GenerateSitemap = true,
    GenerateRssFeeds = true
});
```

## ContentEngineContentOptions<T>

Configuration specific to a content type, where `T` implements `IFrontMatter`.

### Properties

#### ContentPath
**Type:** `string`  
**Required:** Yes  
**Description:** The directory path where this content type's files are located, relative to `ContentRootPath`.

```csharp
ContentPath = "Content/Blog"     // Blog posts
ContentPath = "Content/Docs"     // Documentation
ContentPath = "Content/Projects" // Portfolio projects
```

#### BasePageUrl
**Type:** `string`  
**Default:** `""`  
**Description:** The URL prefix for this content type.

```csharp
BasePageUrl = "blog"     // URLs: /blog/post-title
BasePageUrl = "docs"     // URLs: /docs/page-title
BasePageUrl = ""         // URLs: /page-title (root level)
```

#### RouteTemplate
**Type:** `string`  
**Default:** `"{slug}"`  
**Description:** Template for generating URLs from content metadata.

Available placeholders:
- `{slug}` - Generated from filename or title
- `{year}`, `{month}`, `{day}` - From content date
- `{category}`, `{subcategory}` - From directory structure
- `{author}` - From front matter (if available)
- Any custom front matter property

```csharp
RouteTemplate = "{slug}"                        // Simple: /blog/my-post
RouteTemplate = "{year}/{month}/{slug}"         // Date-based: /blog/2025/01/my-post
RouteTemplate = "{category}/{slug}"             // Category: /blog/tutorials/my-post
RouteTemplate = "{year}/{month}/{day}/{slug}"  // Full date: /blog/2025/01/15/my-post
```

#### IncludeDrafts
**Type:** `bool`  
**Default:** `false`  
**Description:** Whether to include content marked as drafts.

```csharp
IncludeDrafts = false                           // Hide drafts
IncludeDrafts = true                            // Show drafts
IncludeDrafts = builder.Environment.IsDevelopment() // Show in dev only
```

#### SortByDate
**Type:** `bool`  
**Default:** `true`  
**Description:** Whether to sort content by date (newest first) by default.

```csharp
SortByDate = true   // Sort by date descending
SortByDate = false  // Use custom sort or order property
```

#### GenerateTagPages
**Type:** `bool`  
**Default:** `true`  
**Description:** Whether to generate individual pages for each tag.

```csharp
GenerateTagPages = true   // Create /tags/csharp, /tags/blazor, etc.
GenerateTagPages = false  // No individual tag pages
```

#### GenerateArchivePages
**Type:** `bool`  
**Default:** `true`  
**Description:** Whether to generate date-based archive pages.

```csharp
GenerateArchivePages = true   // Create /2025/, /2025/01/, etc.
GenerateArchivePages = false  // No date archives
```

#### PageSize
**Type:** `int`  
**Default:** `10`  
**Description:** Number of items per page for paginated content lists.

```csharp
PageSize = 10  // 10 items per page
PageSize = 25  // 25 items per page
```

### Advanced Properties

#### ContentFilter
**Type:** `Func<MarkdownContentPage<T>, bool>?`  
**Default:** `null`  
**Description:** Custom function to filter which content should be included.

```csharp
ContentFilter = post => 
{
    // Only include posts from current year
    return post.FrontMatter.Date.Year == DateTime.Now.Year;
}

ContentFilter = page => 
{
    // Exclude content with specific tag
    return !page.FrontMatter.Tags.Contains("internal");
}
```

#### CustomSortFunction
**Type:** `Func<IEnumerable<MarkdownContentPage<T>>, IEnumerable<MarkdownContentPage<T>>>?`  
**Default:** `null`  
**Description:** Custom function to define content sorting order.

```csharp
CustomSortFunction = posts => posts
    .OrderByDescending(p => p.FrontMatter.IsPinned)  // Pinned first
    .ThenByDescending(p => p.FrontMatter.Date)       // Then by date
    .ThenBy(p => p.FrontMatter.Title)                // Then alphabetically

CustomSortFunction = projects => projects
    .OrderBy(p => p.FrontMatter.Category)            // Group by category
    .ThenByDescending(p => p.FrontMatter.Priority)   // Then by priority
```

#### ContentTransformer
**Type:** `Func<string, T, string>?`  
**Default:** `null`  
**Description:** Custom function to transform content during processing.

```csharp
ContentTransformer = (content, frontMatter) => 
{
    // Add reading time calculation
    var wordCount = content.Split(' ').Length;
    frontMatter.ReadingTime = TimeSpan.FromMinutes(wordCount / 200.0);
    
    // Transform content
    return content.Replace("{{READING_TIME}}", 
        $"{frontMatter.ReadingTime.TotalMinutes:F0} min read");
}
```

### Usage Example

```csharp
// Blog posts configuration
builder.Services.AddContentEngineStaticContentService(() => 
    new ContentEngineContentOptions<BlogPost>()
    {
        ContentPath = "Content/Blog",
        BasePageUrl = "blog",
        RouteTemplate = "{year}/{month}/{slug}",
        IncludeDrafts = builder.Environment.IsDevelopment(),
        SortByDate = true,
        GenerateTagPages = true,
        GenerateArchivePages = true,
        PageSize = 10,
        ContentFilter = post => !post.FrontMatter.Tags.Contains("private"),
        CustomSortFunction = posts => posts
            .OrderByDescending(p => p.FrontMatter.IsFeatured)
            .ThenByDescending(p => p.FrontMatter.Date)
    });

// Documentation configuration
builder.Services.AddContentEngineStaticContentService(() => 
    new ContentEngineContentOptions<DocPage>()
    {
        ContentPath = "Content/Docs",
        BasePageUrl = "docs",
        RouteTemplate = "{category}/{slug}",
        IncludeDrafts = false,
        SortByDate = false,
        GenerateTagPages = true,
        GenerateArchivePages = false,
        PageSize = 50,
        CustomSortFunction = docs => docs.OrderBy(d => d.FrontMatter.Order)
    });
```

## MonorailCssOptions

Configuration for MonorailCSS integration (if using the MonorailCSS package).

### Properties

#### PrimaryHue
**Type:** `Func<int>`  
**Description:** Function that returns the primary color hue (0-360).

```csharp
PrimaryHue = () => 250  // Blue-purple
PrimaryHue = () => 120  // Green
PrimaryHue = () => 0    // Red
```

#### AccentHue
**Type:** `Func<int>?`  
**Description:** Function that returns the accent color hue.

```csharp
AccentHue = () => 30   // Orange accent
AccentHue = () => 280  // Purple accent
```

#### EnableDarkMode
**Type:** `bool`  
**Default:** `true`  
**Description:** Whether to generate dark mode CSS classes.

```csharp
EnableDarkMode = true   // Generate dark mode classes
EnableDarkMode = false  // Light mode only
```

### Usage Example

```csharp
builder.Services.AddMonorailCss(new MonorailCssOptions 
{ 
    PrimaryHue = () => 250,
    AccentHue = () => 30,
    EnableDarkMode = true
});
```

## RoslynHighlighterOptions

Configuration for Roslyn-based code highlighting (if using Roslyn integration).

### Properties

#### ConnectedSolution
**Type:** `ConnectedDotNetSolution?`  
**Description:** Configuration for connecting to a .NET solution for live code highlighting.

```csharp
ConnectedSolution = new ConnectedDotNetSolution
{
    SolutionPath = "../../MyProject.sln",
    ProjectsPath = "../../src/"
}
```

#### EnableSemanticHighlighting
**Type:** `bool`  
**Default:** `true`  
**Description:** Whether to use semantic analysis for enhanced highlighting.

```csharp
EnableSemanticHighlighting = true   // Full semantic highlighting
EnableSemanticHighlighting = false  // Basic syntax highlighting only
```

### Usage Example

```csharp
builder.Services.AddRoslynService(() => new RoslynHighlighterOptions()
{
    ConnectedSolution = new ConnectedDotNetSolution
    {
        SolutionPath = "../../MyProject.sln",
        ProjectsPath = "../../src/"
    },
    EnableSemanticHighlighting = true
});
```

## Environment-Specific Configuration

### Development vs. Production

Common patterns for environment-specific settings:

```csharp
builder.Services.AddContentEngineService(() => new ContentEngineOptions
{
    BlogTitle = "My Site",
    BaseUrl = builder.Environment.IsDevelopment() 
        ? "https://localhost:5001" 
        : "https://mysite.com",
    EnableHotReload = builder.Environment.IsDevelopment(),
    OutputPath = builder.Environment.IsDevelopment()
        ? "wwwroot/dev"
        : "wwwroot"
});

builder.Services.AddContentEngineStaticContentService(() => 
    new ContentEngineContentOptions<BlogPost>()
    {
        IncludeDrafts = builder.Environment.IsDevelopment(),
        PageSize = builder.Environment.IsDevelopment() ? 5 : 10
    });
```

### Configuration from appsettings.json

Bind configuration from settings files:

```json
{
  "ContentEngine": {
    "BlogTitle": "My Site",
    "BaseUrl": "https://mysite.com",
    "GenerateSitemap": true
  },
  "BlogContent": {
    "ContentPath": "Content/Blog",
    "BasePageUrl": "blog",
    "PageSize": 10
  }
}
```

```csharp
// Bind main options
var engineOptions = builder.Configuration
    .GetSection("ContentEngine")
    .Get<ContentEngineOptions>();
builder.Services.AddContentEngineService(() => engineOptions);

// Bind content options
var blogOptions = builder.Configuration
    .GetSection("BlogContent")
    .Get<ContentEngineContentOptions<BlogPost>>();
builder.Services.AddContentEngineStaticContentService(() => blogOptions);
```

## Validation and Defaults

All configuration classes include validation and sensible defaults:

- **Required properties** will throw exceptions if not provided
- **Optional properties** have documented default values
- **Invalid configurations** are caught at startup
- **Type safety** prevents configuration errors

For complete examples and usage patterns, see the [Configure Content Options](../how-to/configure-content-options.md) how-to guide.