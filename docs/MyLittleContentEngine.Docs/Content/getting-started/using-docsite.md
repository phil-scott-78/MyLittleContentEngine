---
title: "Using the DocSite Package"
description: "Learn how to create a documentation site using the DocSite package with customizable branding and styling"
order: 1002
---

The `MyLittleContentEngine.DocSite` package provides a complete documentation site solution with minimal setup. It includes all the components, layouts, and styling needed to create a professional documentation site with customizable branding.

## What You'll Build

You'll create a documentation site with:

- Professional documentation layout with navigation
- API documentation generation
- Search functionality
- Responsive design with dark/light mode
- Custom branding and styling

## Prerequisites

Before starting, ensure you have:

- .NET 9 SDK or later installed
- A code editor (Visual Studio, VS Code, or JetBrains Rider)
- Familiarity with command-line tools

<Steps>
<Step stepNumber="1">
## Create a New Blazor Project

Start by creating a new empty Blazor Server project:

```bash
dotnet new blazorserver-empty -n MyDocSite
cd MyDocSite
```
</Step>

<Step stepNumber="2">

## Add the DocSite Package

Add the DocSite package reference to your project:

```bash
dotnet add package MyLittleContentEngine.DocSite
```

This package includes all the dependencies you need:
- `MyLittleContentEngine` - Core content management functionality
- `MyLittleContentEngine.UI` - UI components for documentation
- `MyLittleContentEngine.MonorailCss` - CSS framework for styling
- `Mdazor` - Markdown rendering for Blazor
</Step>

<Step stepNumber="3">

## Configure the DocSite

Replace the content of `Program.cs` with the following minimal configuration:

```csharp
using MyLittleContentEngine.DocSite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDocSite(options =>
{
    options.SiteTitle = "My Documentation Site";
    options.Description = "Documentation for my project";
});

var app = builder.Build();

app.UseDocSite();

await app.RunDocSiteAsync(args);
```

This minimal setup provides a complete documentation site with default styling and layout.
</Step>

<Step stepNumber="4">

## Create the Content Structure

Create the content directory structure:

```bash
mkdir -p Content
```

The DocSite package expects your content to be in the `Content` directory by default.
</Step>

<Step stepNumber="5">

## Write Your First Documentation Page

Create your first documentation page at `Content/index.md`:

```markdown
---
title: "Welcome to My Documentation"
description: "Getting started with our documentation site"
---

# Welcome

This is the home page of our documentation site. You can write content using Markdown and it will be automatically rendered with a professional layout.

## Features

- **Responsive Design**: Looks great on all devices
- **Search**: Built-in search functionality
- **API Documentation**: Automatic API reference generation
- **Dark Mode**: Toggle between light and dark themes
```
</Step>

<Step stepNumber="6">

## Customize Your Site

You can customize various aspects of your documentation site by modifying the options in `Program.cs`:

```csharp
builder.Services.AddDocSite(options =>
{
    // Basic site information
    options.SiteTitle = "My Documentation Site";
    options.Description = "Comprehensive documentation for my project";
    options.CanonicalBaseUrl = "https://mydocs.example.com";
    
    // Styling and branding
    options.PrimaryHue = 235; // Blue theme (0-360)
    options.BaseColorName = "Zinc"; // Base color palette
    options.GitHubUrl = "https://github.com/myuser/myproject";
    
    // API Documentation (optional)
    options.SolutionPath = "../../MySolution.sln";
    options.IncludeNamespaces = ["MyProject", "MyProject.Core"];
    options.ExcludeNamespaces = ["MyProject.Tests"];
    
    // Advanced customization
    options.ExtraStyles = """
        .custom-header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }
        """;
});
```
</Step>

<Step stepNumber="7">

## Add Custom Branding (Optional)

For advanced customization, you can add custom header content or logos:

```csharp
builder.Services.AddDocSite(options =>
{
    options.SiteTitle = "My Documentation Site";
    
    // Custom header with logo
    options.HeaderContent = builder => builder.AddMarkupContent(0, """
        <div class="flex items-center gap-2">
            <img src="/logo.png" alt="Logo" class="h-8 w-8" />
            <span class="text-xl font-bold">My Docs</span>
        </div>
        """);
    
    // Custom footer
    options.FooterContent = builder => builder.AddMarkupContent(0, """
        <div class="text-center text-sm text-base-600 dark:text-base-400">
            © 2024 My Company. All rights reserved.
        </div>
        """);
});
```
</Step>

<Step stepNumber="8">

## Configure File Watching for Development

Add the following to your `.csproj` file to enable hot reload during development:

```xml
<ItemGroup>
    <Watch Include="Content\**\*.*"/>
</ItemGroup>
```
</Step>

<Step stepNumber="9">

## Test Your Documentation Site

Run your site in development mode:

```bash
dotnet watch
```

Navigate to `https://localhost:5001` to see your documentation site in action!

While the page is open, try editing the `Content/index.md` file. You should see the changes reflected immediately without needing to restart the server.
</Step>
</Steps>

## Available Configuration Options

The `DocSiteOptions` class provides many customization options:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `SiteTitle` | string | "Documentation Site" | The title displayed in the header |
| `Description` | string | "A documentation site..." | Site description for SEO |
| `PrimaryHue` | int | 235 | Primary color hue (0-360) |
| `BaseColorName` | string | "Zinc" | Base color palette name |
| `GitHubUrl` | string? | null | GitHub repository URL |
| `CanonicalBaseUrl` | string? | null | Canonical URL for SEO |
| `SolutionPath` | string? | null | Path to solution file for API docs |
| `IncludeNamespaces` | string[]? | null | Namespaces to include in API docs |
| `ExcludeNamespaces` | string[]? | null | Namespaces to exclude from API docs |
| `ContentRootPath` | string | "Content" | Path to content directory |
| `BaseUrl` | string | "/" | Base URL for routing |
| `ExtraStyles` | string? | null | Additional CSS styles |
| `HeaderIcon` | RenderFragment? | null | Custom header icon |
| `HeaderContent` | RenderFragment? | null | Custom header content |
| `FooterContent` | RenderFragment? | null | Custom footer content |

## Next Steps

Now that you have a basic documentation site running, you can:

- Add more content pages to the `Content` directory
- Organize content into subdirectories for better navigation
- Enable API documentation by setting `SolutionPath`
- Customize the styling with `ExtraStyles`
- Deploy your site to a hosting provider

The DocSite package handles all the complex setup for you, allowing you to focus on writing great documentation!