---
title: "Linking Documents and Media"
description: "Master BaseUrl configuration and automatic link rewriting for consistent links across deployment scenarios"
order: 2010
---

The most challenging aspect of building static sites is creating links that work consistently across different
deployment scenarios. MyLittleContentEngine solves this through automatic BaseUrl-aware link rewriting that ensures
your links work seamlessly whether deployed at the root domain or in a subdirectory.

## The Deployment Challenge

Static sites often need to work in multiple deployment contexts:

- **Local development**: `http://localhost:5000/`
- **Production at root**: `https://mydomain.com/`
- **Production in subdirectory**: `https://mydomain.github.io/my-repo/`
- **Versioned in subdirectory**: `https://mydomain.github.io/my-repo/v4/`

The same site must generate correct links regardless of where it's deployed. MyLittleContentEngine's automatic link
rewriting handles this complexity for you.

## Understanding BaseUrl

The `BaseUrl` setting in `ContentEngineOptions` tells MyLittleContentEngine where your site will be deployed.

### BaseUrl Format Rules

- **Local development**: `"/"` (root)
- **Root domain**: `"/"` (root)
- **Subdirectory**: `"/repository-name/"`

### BaseUrl Configuration

Configure the `BaseUrl` in your `Program.cs` using `ContentEngineOptions`. We recommend using an environment variable
for flexible deployment:

```csharp
builder.Services.AddContentEngineService(_ => new ContentEngineOptions
{
    SiteTitle = "My Documentation Site",
    SiteDescription = "Technical documentation",
    BaseUrl = Environment.GetEnvironmentVariable("BaseUrl") ?? "/",
    ContentRootPath = "Content",
});
```

### Environment-Based Configuration

The recommended pattern uses environment variables for flexible deployment:

**Local Development:**

```bash
# No BaseUrl set, defaults to "/"
dotnet watch
```

**GitHub Pages Deployment:**

```bash
# Set via environment variable or CI/CD
export BaseUrl="/MyLittleContentEngine/"
dotnet run --build-static
```

## Automatic Link Rewriting

MyLittleContentEngine automatically processes all links in your content:

### In Markdown Content

**Use relative or absolute links** - both are automatically processed:

```markdown
<!-- Relative links - automatically rewritten -->
[Another Article](../guides/advanced-configuration)
[Media File](./images/diagram.png)
[Root Page](../../index)

<!-- Absolute links - BaseUrl automatically prepended -->
[Documentation](/docs/guide)
[API Reference](/api)
[Home Page](/)
```

**How the rewriting works:**

1. `LinkRewriter` processes all URLs during HTML generation
2. Relative paths get resolved relative to the current page location
3. Absolute paths get the `BaseUrl` automatically prepended
4. External URLs and anchor links remain unchanged

**Example transformations** (with `BaseUrl = "/MyLittleContentEngine"`):

| Original Link         | Current Page  | Result                                  |
|-----------------------|---------------|-----------------------------------------|
| `../api`              | `/docs/guide` | `/MyLittleContentEngine/docs/api`       |
| `/docs/guide`         | Any page      | `/MyLittleContentEngine/docs/guide`     |
| `image.png`           | `/docs/guide` | `/MyLittleContentEngine/docs/image.png` |
| `https://example.com` | Any page      | `https://example.com` (unchanged)       |

### In Razor Pages and Components

Use the `<RefLink>` component for consistent link handling. Not we swap the `class` 
attribute to `CssClass`:

```razor
@* Basic usage *@
<RefLink Href="/docs/guide">Documentation</RefLink>
<RefLink Href="../api">API Reference</RefLink>

@* With additional attributes *@
<RefLink Href="https://example.com" Target="_blank" CssClass="external-link">
    External Site
</RefLink>

@* For CSS/JS resources, inject LinkService *@
@inject LinkService LinkService

<link rel="stylesheet" href="@LinkService.GetLink("/styles.css")" />
<script src="@LinkService.GetLink("/scripts/app.js")"></script>
```

### Manual Link Processing

For advanced scenarios, you can use `LinkService` directly:

```csharp
@inject LinkService LinkService

@{
    var processedUrl = LinkService.GetLink("/docs/guide");
    var relativeUrl = LinkService.GetLink("../api", "/docs/current");
}
```

## Static Files in `ContentRootPath`

Static files like images, CSS, and JavaScript are served from the `ContentRootPath` directory and automatically
processed:

### In Markdown

```markdown
![Logo](/images/logo.png)
[Download PDF](/documents/guide.pdf)
```

### In Razor Components

```razor
<img src="@LinkService.GetLink("/images/logo.png")" alt="Logo" />
<RefLink Href="/documents/guide.pdf">Download Guide</RefLink>
```

## Testing Static Deployment

Test your site in the same environment it will be deployed to using the `dotnet-serve` tool:

<Steps>
<Step stepNumber="1">
### Install dotnet-serve
```bash
dotnet tool install --global dotnet-serve
```
</Step>

<Step stepNumber="2">
### Clean Your Project
```bash
dotnet clean
```
</Step>

<Step stepNumber="3">
### Build with BaseHref
```bash
BaseUrl="/mybase" dotnet run -- build
```
</Step>

<Step stepNumber="4">
### Serve with Correct Base URL
```bash
dotnet serve -d output --path-base mybase --default-extensions:.html
```
</Step>
</Steps>

Create a script to automate testing:

```bash
#!/bin/bash
dotnet clean
BaseUrl="/mybase" dotnet run -- build 
dotnet serve -d output --path-base mybase --default-extensions:.html
```

## Link Validation

MyLittleContentEngine provides built-in link validation:

- **Relative links** are resolved and validated against your content structure
- **Absolute internal links** are checked for valid routes
- **External links** are preserved without validation
- **Missing targets** generate build warnings

## Best Practices

1. **Prefer absolute paths** (`/docs/guide`) over relative paths (`../guide`) for cleaner, more predictable links
2. **Use the `RefLink` component** in Razor pages for consistent behavior
3. **Test with different BaseUrl values** to ensure deployment compatibility
4. **Inject LinkService** for programmatic link generation
5. **Avoid manual BaseUrl concatenation** - let the system handle it automatically

## Summary

MyLittleContentEngine's automatic link rewriting eliminates the complexity of managing links across different deployment
scenarios. By configuring `BaseUrl` appropriately and using the provided `RefLink` component and `LinkService`, your site
will generate correct links whether deployed at the root or in a subdirectory, without requiring HTML `<base>` tags that
can interfere with crawlers and SEO.