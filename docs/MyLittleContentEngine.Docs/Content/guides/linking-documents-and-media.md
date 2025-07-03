---
title: "Linking Documents and Media"
description: "Master the complexities of BaseUrl configuration and linking strategies for different deployment scenarios"
order: 2010
---

The most challenging aspect of building static sites is creating links that work consistently across different
deployment scenarios. MyLittleContentEngine handles this complexity through a sophisticated URL rewriting system built
around `BaseUrl` configuration and context-aware linking strategies.

## The Deployment Challenge

Static sites often need to work in multiple deployment contexts:

- **Local development**: `http://localhost:5000/`
- **Production at root**: `https://mydomain.com/`
- **Production in subdirectory**: `https://mydomain.github.io/my-repo/`
- **Versioned in subdirectory**: `https://mydomain.github.io/my-repo/v4/`

The same site must generate correct links regardless of where it's deployed. This is where Base Url becomes critical.

## Understanding BaseUrl

The [base HTML element](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/base) is a key part of HTML that
allows you to set a base URL for **all** relative URLs in a document.
It ensures that links and resources are resolved correctly based on the deployment context.

Once the base url is set, all relative links in the document will be resolved against it. For example, if you are in the
`/blog/post/my-latest-post` page and the base URL is set to `/blog`, a link like `my-image.jpg` will resolve to
`/blog/my-image.jpg`.

Because these rules can be complex, we recommend using the following guidelines:

- Always configure the base url in your razor page and in the `ContentEngineOptions` configuration.
- Never use leading slashes to create absolute paths. A link like `/blog/post` will work locally but break in production
  if deployed in a
  subdirectory.

### BaseUrl Format Rules

- **Local development**: `"/"` (empty path)
- **Root domain**: `"/"` (empty path)
- **Subdirectory**: `"/repository-name"` (no trailing slash)

### BaseUrl Configuration

Configuring the `BaseUrl` is done in the `ContentEngineOptions` when setting up the service in your `Program.cs`.
We recommend using an environment variable to set the base URL dynamically based on the deployment context.

```csharp
builder.Services.AddContentEngineService(_ => new ContentEngineOptions
{
    SiteTitle = "My Documentation Site",
    SiteDescription = "Technical documentation",
    BaseUrl = Environment.GetEnvironmentVariable("BaseHref") ?? "/",
    ContentRootPath = "Content",
});
```

### Configuring BaseUrl in Razor Pages

In your Razor pages, you can set the base URL using the `<base>` HTML element in your `App.razor` file
by injecting the `ContentEngineOptions` and using the `BaseUrl` property:

```razor
@inject ContentEngineOptions Options

<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="utf-8"/>
    <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
    <base href="@Options.BaseUrl"/>

    <HeadOutlet/>
</head>
```

### Environment-Based Configuration

The recommended pattern uses environment variables for flexible deployment:

**Local Development:**

```bash
# No BaseHref set, defaults to "/"
dotnet watch
```

**GitHub Pages Deployment:**

```bash
# Set via environment variable or CI/CD
export BaseHref="/MyLittleContentEngine"
dotnet run --build-static
```

## Linking Strategies by Context

### Within Markdown Content

**Use relative links** - MyLittleContentEngine automatically rewrites them based on the page context:

```markdown
<!-- Relative links - automatically rewritten -->
[Another Article](../guides/advanced-configuration)
[Media File](./images/diagram.png)
[Root Page](index)
```

**How the rewriting works:**

1. `LinkRewriter` processes all URLs during HTML generation
2. Relative paths get resolved relative to the current page and rewritten to be relative links from the base of the site
3. External URLs and anchor links remain unchanged

### In Razor Pages and Components

Use relative paths from the site root:

```html
<a href="blog"">Blog</a>
```


## Static Files in `ContentRootPath`

Static files like images, CSS, and JavaScript are served from the `ContentRootPath` directory.
Use relative paths in your Razor components or HTML files from the base of the site:

```html
<img src="images/logo.png" alt="Logo" />
```

When working within markdown files, rememer that we will rewrite the links to be relative to the current page

```markdown
[My Vacation](vacation.png) <!-- This will be rewritten to a path relative to the base url -->
```

## Testing Static Deployment

It is crucial to test your site in the same environment it will be deployed to. We can use the 
[https://github.com/natemcmaster/dotnet-serve](dotnet-serve) tool to simulate a production environment
by serving the static files with the correct base URL.

<Steps>
<Step stepNumber="1">
### Install the `dotnet-serve` tool globally 
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
### Build the Site with BaseHref Set
```bash
dotnet run --environment baseHref="/mybase/" -- build 
```
</Step>

<Step stepNumber="4">
### Serve the Output with Correct Base URL

Use the dotnet-serve tool to serve the output directory with the specified base path. We will also
set the default extensions to `.html` so that we can access the pages without needing to specify the extension in the URL.

```bash
dotnet serve -d output --path-base mybase --default-extensions:.html
```
</Step>
</Steps>

Once dotnet-serve is installed, you can create a script to automate the build and serve process. 
Here’s an example script for Windows:

```bash
dotnet clean
dotnet run --environment baseHref="/mybase/" -- build 
dotnet serve -d output --path-base mybase --default-extensions:.html
```

## Summary

Getting a static site to work correctly across different deployment contexts can be complex, but MyLittleContentEngine's
link rewriting and BaseUrl configuration make it manageable. By following the guidelines for BaseUrl setup and using
relative links in your content, you can ensure your site works seamlessly whether deployed at the root or in a subdirectory.