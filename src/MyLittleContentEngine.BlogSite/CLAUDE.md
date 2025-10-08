# CLAUDE.md

MyLittleContentEngine.BlogSite is a ready-to-use blog site package built on MyLittleContentEngine.

## What It Is
A complete Blazor-based blog site with integrated:
- **Content engine** - Markdown/MDX content processing with tagging and series support
- **UI components** - Badge, Card, CardGrid, LinkCard, Steps
- **MonorailCSS** - Styling and theming
- **Social media** - Open Graph and Twitter Card meta tags
- **RSS/Sitemap** - Optional feed generation

## Usage
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBlogSite(sp => new BlogSiteOptions
{
    SiteTitle = "My Blog",
    Description = "My awesome blog",
    ContentRootPath = "Content",
    BlogContentPath = "Blog",
    CanonicalBaseUrl = "https://example.com",
    AuthorName = "John Doe",
    AuthorBio = "Software developer",
});

var app = builder.Build();
app.UseBlogSite();
await app.RunBlogSiteAsync(args);
```

## Key Files
- `BlogSiteServiceExtensions.cs` - Configuration extensions
- `BlogSiteOptions.cs` - Site configuration options
- `BlogSiteFrontMatter.cs` - Blog post metadata schema

## Razor Components

### Core
- `Components/App.razor` - Root HTML document with RSS feed link and theme setup
- `Components/Routes.razor` - Router configuration with MainLayout
- `Components/_Imports.razor` - Global using directives
- `Components/SocialIcons.razor` - Collection of social media icon SVGs

### Layout
- `Components/Layout/MainLayout.razor` - Main site layout with header, navigation, search, and theme toggle
- `Components/Layout/ContentLayout.razor` - Nested layout for constraining content width
- `Components/Layout/ContentWithProseLayout.razor` - Content layout with prose typography styles
- `Components/Layout/BlogPost.razor` - Single blog post renderer with series navigation, tags, and metadata
- `Components/Layout/BlogSummary.razor` - Wrapper for rendering multiple blog article cards
- `Components/Layout/BlogArticleCard.razor` - Individual blog post card with title, date, and description
- `Components/Layout/BlogPostsList.razor` - Blog posts list with dividers for archive/tag pages

### Pages
- `Components/Pages/Home.razor` - Home page with recent posts, hero content, and sidebar
- `Components/Pages/Blog.razor` - Individual blog post page with social media meta tags
- `Components/Pages/Archive.razor` - Complete archive of all blog posts
- `Components/Pages/Tags.razor` - All tags page with post counts
- `Components/Pages/Tag.razor` - Tag-filtered blog posts page
