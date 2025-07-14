---
title: "Linking Documents and Media"
description: "Master BaseUrl configuration and automatic link rewriting for consistent links across deployment scenarios"
uid: "docs.guides.linking-documents-and-media"
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

MyLittleContentEngine automatically processes all links in your content using the `BaseUrlRewritingMiddleware`:

### In Markdown Content

**Use relative or absolute links** - both are automatically processed:

```markdown
<!-- Relative links - work as expected -->
[Another Article](../guides/advanced-configuration)
[Media File](./images/diagram.png)
[Root Page](../../index)

<!-- Absolute links - BaseUrl automatically prepended -->
[Documentation](/docs/guide)
[API Reference](/api)
[Home Page](/)
```

**How the rewriting works:**

1. `BaseUrlRewritingMiddleware` intercepts HTML responses before they're sent to the browser
2. Root-relative URLs (starting with `/`) get the `BaseUrl` automatically prepended
3. Relative paths work naturally without modification
4. External URLs and anchor links remain unchanged

**Example transformations** (with `BaseUrl = "/MyLittleContentEngine"`):

| Original Link         | Result                                  |
|-----------------------|-----------------------------------------|
| `/docs/guide`         | `/MyLittleContentEngine/docs/guide`     |
| `/api`                | `/MyLittleContentEngine/api`            |
| `/`                   | `/MyLittleContentEngine/`               |
| `https://example.com` | `https://example.com` (unchanged)       |
| `../api` (relative)   | `../api` (unchanged)                    |

### In Razor Pages and Components

Links in Razor pages and components are automatically processed by the middleware. Simply use standard HTML elements:

```razor
@* Basic usage - automatically rewritten *@
<a href="/docs/guide">Documentation</a>
<a href="../api">API Reference</a>

@* External links remain unchanged *@
<a href="https://example.com" target="_blank" class="external-link">
    External Site
</a>

@* Asset references are also automatically rewritten *@
<link rel="stylesheet" href="/styles.css" />
<script src="/scripts/app.js"></script>
<img src="/images/logo.png" alt="Logo" />
```

### What Gets Rewritten

The `BaseUrlRewritingMiddleware` automatically processes the following HTML elements and attributes:

- **Links**: `<a href="">` and `<link href="">`
- **Resources**: `<img src="">`, `<script src="">`, `<iframe src="">`, `<embed src="">`, `<source src="">`, `<track src="">`
- **Forms**: `<form action="">`
- **Data attributes**: Any `data-*` attributes containing URLs
- **CSS**: `url()` functions in style attributes and `@import` statements

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
<img src="/images/logo.png" alt="Logo" />
<a href="/documents/guide.pdf">Download Guide</a>
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

## Best Practices

1. **Prefer absolute paths** (`/docs/guide`) over relative paths (`../guide`) for automatic BaseUrl rewriting
2. **Use standard HTML elements** (`<a>`, `<img>`, `<link>`) - the middleware handles them automatically
3. **Test with different BaseUrl values** to ensure deployment compatibility
4. **Avoid manual BaseUrl concatenation** - let the middleware handle it automatically
5. **Use environment variables** for BaseUrl configuration to support different deployment scenarios

## Summary

MyLittleContentEngine's `BaseUrlRewritingMiddleware` eliminates the complexity of managing links across different deployment
scenarios. By configuring `BaseUrl` in `ContentEngineOptions` and using standard HTML elements, your site will automatically
generate correct links whether deployed at the root or in a subdirectory, without requiring HTML `<base>` tags that
can interfere with crawlers and SEO.