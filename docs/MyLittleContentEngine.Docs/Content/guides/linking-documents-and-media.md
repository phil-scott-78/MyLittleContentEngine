---
title: "Linking Documents and Media"
description: "Master the complexities of BaseUrl configuration and linking strategies for different deployment scenarios"
order: 2010
---

The most challenging aspect of building static sites is creating links that work consistently across different deployment scenarios. MyLittleContentEngine handles this complexity through a sophisticated URL rewriting system built around `BaseUrl` configuration and context-aware linking strategies.

## The Deployment Challenge

Static sites often need to work in multiple deployment contexts:

- **Local development**: `http://localhost:5000/`
- **Production at root**: `https://mydomain.com/`
- **Production in subdirectory**: `https://mydomain.github.io/my-repo/`

The same site must generate correct links regardless of where it's deployed. This is where `BaseUrl` becomes critical.

## Understanding BaseUrl

The `BaseUrl` property in `ContentEngineOptions` solves the subdirectory deployment problem by providing a path prefix that gets prepended to all internal links.

### BaseUrl Configuration

```csharp
builder.Services.AddContentEngineService(_ => new ContentEngineOptions
{
    SiteTitle = "My Documentation Site",
    SiteDescription = "Technical documentation",
    BaseUrl = Environment.GetEnvironmentVariable("BaseHref") ?? "/",
    ContentRootPath = "Content",
});
```

### BaseUrl Format Rules

- **Local development**: `"/"` (empty path)
- **Root domain**: `"/"` (empty path)  
- **Subdirectory**: `"/repository-name"` (no trailing slash)

### Environment-Based Configuration

The recommended pattern uses environment variables for flexible deployment:

**Local Development:**
```bash
# No BaseHref set, defaults to "/"
dotnet run
```

**GitHub Pages Deployment:**
```bash
# Set via environment variable or CI/CD
export BaseHref="/MyLittleContentEngine"
dotnet run --build-static
```

## Understanding BasePageUrl

`BasePageUrl` in `ContentEngineContentOptions` serves a different purpose - it defines the URL path segment for content sections and the generated folder structure.

### BasePageUrl Configuration

```csharp
// For blog content at /blog/*
builder.Services.AddContentEngineStaticContentService(_ => new ContentEngineContentOptions<BlogFrontMatter>()
{
    ContentPath = "Content/Blog",
    BasePageUrl = "blog"  // Creates /blog/ URLs
});

// For root-level content at /*
builder.Services.AddContentEngineStaticContentService(_ => new ContentEngineContentOptions<PageFrontMatter>()
{
    ContentPath = "Content/Pages", 
    BasePageUrl = string.Empty  // Creates root-level URLs
});
```

### BasePageUrl vs BaseUrl

| Property | Purpose | Example | Context |
|----------|---------|---------|---------|
| `BaseUrl` | Deployment path prefix | `"/my-repo"` | Site-wide deployment location |
| `BasePageUrl` | Content section path | `"blog"` | Content organization and routing |

Combined, they create URLs like: `{BaseUrl}/{BasePageUrl}/{content-slug}`

## Linking Strategies by Context

### Within Markdown Content

**Use relative links** - MyLittleContentEngine automatically rewrites them based on the page context:

```markdown
<!-- Relative links - automatically rewritten -->
[Another Article](../guides/advanced-configuration)
[Media File](./images/diagram.png)
[Root Page](/index)

<!-- These work regardless of BaseUrl configuration -->
```

**How the rewriting works:**
1. `LinkRewriter` processes all URLs during HTML generation
2. Relative paths get resolved relative to the current page
3. Absolute paths (starting with `/`) get `BaseUrl` prepended
4. External URLs and anchor links remain unchanged

### In Razor Pages and Components

**Use site-root relative links** with the `LinkService`:

```razor
@inject LinkService LinkService

<nav>
    <a href="@LinkService.GetLink("/")">Home</a>
    <a href="@LinkService.GetLink("/blog")">Blog</a>
    <a href="@LinkService.GetLink("/api")">API Docs</a>
</nav>
```

Or use relative paths from the site root:

```razor
<!-- This assumes you know the BaseUrl -->
<a href="@($"{BaseUrl}/blog")">Blog</a>
```

## Practical Deployment Examples

### GitHub Pages Configuration

**Program.cs:**
```csharp
builder.Services.AddContentEngineService(_ => new ContentEngineOptions
{
    SiteTitle = "My Project Docs",
    SiteDescription = "Documentation for my open source project", 
    BaseUrl = Environment.GetEnvironmentVariable("BaseHref") ?? "/",
    CanonicalBaseUrl = "https://username.github.io/repository-name",
    ContentRootPath = "Content",
});
```

**GitHub Actions workflow:**
```yaml
- name: Build static site
  run: dotnet run --project docs/MyProject.Docs --build-static
  env:
    BaseHref: /repository-name
```

**Result:** Links work both locally (`/`) and on GitHub Pages (`/repository-name`)

### Multi-Section Site

```csharp
// Main site content at root level
builder.Services.AddContentEngineStaticContentService(_ => new ContentEngineContentOptions<PageFrontMatter>()
{
    ContentPath = "Content/Pages",
    BasePageUrl = string.Empty  // Root: /about, /contact
});

// Blog content under /blog
builder.Services.AddContentEngineStaticContentService(_ => new ContentEngineContentOptions<BlogFrontMatter>()
{
    ContentPath = "Content/Blog", 
    BasePageUrl = "blog"  // Blog: /blog/post-title
});

// Docs content under /docs  
builder.Services.AddContentEngineStaticContentService(_ => new ContentEngineContentOptions<DocsFrontMatter>()
{
    ContentPath = "Content/Documentation",
    BasePageUrl = "docs"  // Docs: /docs/guide-name
});
```

## Link Patterns and Best Practices

### Markdown Content Linking

**✅ Recommended patterns:**

```markdown
<!-- Relative to current page -->
[Related Guide](./related-guide)
[Parent Section](../index)

<!-- Absolute from content root -->  
[Home Page](/index)
[Another Section](/blog/important-post)

<!-- Media files -->
![Diagram](./images/architecture.png)
![Shared Image](/media/logo.png)
```

**❌ Avoid these patterns:**

```markdown
<!-- Hardcoded base paths break deployment flexibility -->
[Bad Link](/my-repo/blog/post)
[Also Bad](https://example.com/my-repo/page)

<!-- Including file extensions confuses routing -->
[Will Download Instead of Render](/blog/post.md)
```

### Razor Component Linking

**✅ Recommended patterns:**

```razor
@inject LinkService LinkService

<!-- Use LinkService for dynamic BaseUrl -->
<a href="@LinkService.GetLink("/blog")">Blog</a>
<a href="@LinkService.GetLink("/api/reference")">API</a>

<!-- Or build URLs programmatically -->
@{
    var blogUrl = $"{BaseUrl}/blog";
}
<a href="@blogUrl">Blog</a>
```

## Media and Asset Linking

### Static Files in wwwroot

Files in `wwwroot` are served from the site root and need BaseUrl consideration:

```razor
<!-- CSS/JS files -->
<link href="@LinkService.GetLink("/css/site.css")" rel="stylesheet" />
<script src="@LinkService.GetLink("/js/site.js")"></script>

<!-- Images -->
<img src="@LinkService.GetLink("/images/logo.png")" alt="Logo" />
```

### Content-Relative Media

Media files alongside content are processed by the link rewriter:

```markdown
<!-- In Content/blog/my-post.md -->
![Local Image](./screenshot.png)
![Shared Asset](/media/shared-diagram.png)
```

## Troubleshooting Common Issues

### Links Work Locally But Break in Production

**Problem:** `BaseUrl` not configured for production deployment

**Solution:** Set `BaseHref` environment variable during build:
```bash
export BaseHref="/your-subdirectory"
dotnet run --build-static
```

### Markdown Files Download Instead of Rendering

**Problem:** Including `.md` extension in links

**Solution:** Remove file extensions from links:
```markdown
<!-- Wrong -->
[Guide](./setup-guide.md)

<!-- Correct -->  
[Guide](./setup-guide)
```

### Navigation Breaks After Content Reorganization

**Problem:** Using too many relative `../` paths

**Solution:** Use absolute paths from content root:
```markdown
<!-- Fragile -->
[Other Section](../../other/page)

<!-- Robust -->
[Other Section](/other/page)
```

### CSS/JS Not Loading in Production

**Problem:** Missing BaseUrl in asset references

**Solution:** Use `LinkService` for all asset URLs:
```razor
<link href="@LinkService.GetLink("/css/site.css")" rel="stylesheet" />
```

## Summary

Successful linking in MyLittleContentEngine requires understanding the distinction between:

- **BaseUrl**: Handles deployment location (subdirectory vs root)
- **BasePageUrl**: Organizes content sections and routing
- **Context**: Markdown content vs Razor components use different strategies

By following these patterns, your site will work consistently across all deployment scenarios from local development to GitHub Pages to custom domains.