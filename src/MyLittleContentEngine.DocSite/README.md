# MyLittleContentEngine.DocSite

A pre-configured documentation site package for MyLittleContentEngine that provides a complete documentation website with minimal setup.

## Features

- **Pre-configured UI components and layout** - Complete documentation site with responsive design
- **Customizable branding** - Colors, logos, titles, and styling options
- **Built-in search functionality** - Ready for Algolia DocSearch integration
- **Dark mode support** - Automatic theme switching with user preference detection
- **API documentation generation** - Automatic generation from .NET assemblies
- **Roslyn integration** - Enhanced code highlighting and live code examples
- **Font integration** - Includes Lexend font family for optimal readability
- **Static site generation** - Deploy anywhere with `--build` flag

## Quick Start

1. **Create a new web application:**

```bash
dotnet new web -n MyDocSite
cd MyDocSite
```

2. **Add the package reference:**

```bash
dotnet add package MyLittleContentEngine.DocSite
```

3. **Replace Program.cs:**

```csharp
using MyLittleContentEngine.DocSite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDocSite(options =>
{
    options.SiteTitle = "My Documentation";
    options.Description = "Documentation for my awesome project";
    options.PrimaryHue = 235; // Blue hue
    options.BaseColorName = "Zinc";
    options.GitHubUrl = "https://github.com/myuser/myproject";
    options.CanonicalBaseUrl = "https://myuser.github.io/myproject/";
    
    // Optional: Enable API documentation
    options.SolutionPath = "../../MyProject.sln";
    options.IncludeNamespaces = new[] { "MyProject" };
    options.ExcludeNamespaces = new[] { "MyProject.Tests" };
});

var app = builder.Build();

app.UseDocSite();

await app.RunDocSiteAsync(args);
```

4. **Create a Content directory with an index.md file:**

```markdown
---
title: Welcome
description: Welcome to my documentation site
---

# Welcome to My Documentation

This is the home page of my documentation site built with MyLittleContentEngine.DocSite!
```

5. **Run the application:**

```bash
dotnet watch run
```

## Configuration Options

### Basic Settings

- `SiteTitle`: The name of your documentation site
- `Description`: Description used in meta tags and RSS
- `BaseUrl`: Base URL for the site (default: "/")
- `ContentRootPath`: Path to markdown content (default: "Content")
- `CanonicalBaseUrl`: Full URL for sitemap/RSS generation

### Branding

- `PrimaryHue`: Primary color hue (0-360, default: 235)
- `BaseColorName`: Base color scheme (default: "Zinc")
- `HeaderIcon`: Custom logo/icon (RenderFragment)
- `HeaderContent`: Custom header content (RenderFragment)
- `GitHubUrl`: Link to GitHub repository (optional)
- `ExtraStyles`: Additional CSS styles

### Advanced Features

- `SolutionPath`: Path to .NET solution for API docs
- `ApiReferenceContentOptions`: Detailed API doc configuration
- `IncludeNamespaces`: Namespaces to include in API docs
- `ExcludeNamespaces`: Namespaces to exclude from API docs

## Custom Branding Example

```csharp
builder.Services.AddDocSite(options =>
{
    options.SiteTitle = "Acme Corp Docs";
    options.PrimaryHue = 160; // Green hue
    options.BaseColorName = "Slate";
    options.GitHubUrl = "https://github.com/acmecorp/product";
    
    // Custom header icon
    options.HeaderIcon = @<svg class="h-5 w-5 text-primary-600 dark:text-primary-400" viewBox="0 0 24 24">
        <path fill="currentColor" d="M12 2L2 7v10c0 5.55 3.84 9.95 9 11 5.16-1.05 9-5.45 9-11V7l-10-5z"/>
    </svg>;
    
    // Custom styles
    options.ExtraStyles = """
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
        body { font-family: 'Inter', sans-serif; }
        """;
});
```

## Static Site Generation

To generate a static site for deployment:

```bash
dotnet run -- --build
```

This creates an `output` directory with all static files ready for deployment to GitHub Pages, Netlify, or any static hosting service.

## File Structure

```
MyDocSite/
├── Content/
│   ├── index.md
│   ├── getting-started/
│   │   └── installation.md
│   └── guides/
│       └── configuration.md
├── wwwroot/
│   └── favicon.ico
├── Program.cs
└── MyDocSite.csproj
```

## Built-in Components

The package includes several UI components that can be used in your markdown:

### Steps Component

```markdown
<Steps>
<Step title="Install Package">
Add the NuGet package to your project.
</Step>
<Step title="Configure Services">
Set up the documentation site in Program.cs.
</Step>
</Steps>
```

### Card Components

```markdown
<CardGrid>
<LinkCard title="Getting Started" href="/getting-started">
Learn the basics of setting up your documentation site.
</LinkCard>
<LinkCard title="API Reference" href="/api">
Detailed API documentation for all components.
</LinkCard>
</CardGrid>
```

For more information on available components and features, see the [MyLittleContentEngine documentation](https://phil-scott-78.github.io/MyLittleContentEngine/).