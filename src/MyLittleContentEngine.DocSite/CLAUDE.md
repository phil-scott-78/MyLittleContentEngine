# CLAUDE.md

MyLittleContentEngine.DocSite is a ready-to-use documentation site package built on MyLittleContentEngine.

## What It Is
A complete Blazor-based documentation site with integrated:
- **Content engine** - Markdown/MDX content processing
- **UI components** - Badge, Card, Steps, LinkCard, BigTable
- **MonorailCSS** - Styling and theming
- **API documentation** - Optional Roslyn-based API reference generation

## Usage
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDocSite(sp => new DocSiteOptions
{
    SiteTitle = "My Docs",
    ContentRootPath = "content",
    SolutionPath = "path/to/solution.sln", // Optional
});

var app = builder.Build();
app.UseDocSite();
await app.RunDocSiteAsync(args);
```

## Key Files
- `DocSiteServiceExtensions.cs` - Configuration extensions
- `DocSiteOptions.cs` - Site configuration options
- `DocSiteFrontMatter.cs` - Content metadata schema

## Razor Components

### Core
- `Components/App.razor` - Root HTML document with head/body setup
- `Components/Routes.razor` - Router configuration with MainLayout
- `Components/_Imports.razor` - Global using directives

### Layout
- `Components/Layout/MainLayout.razor` - Main site layout with sidebar, header, search, and theme toggle
- `Components/Layout/Pages.razor` - Dynamic content page renderer with navigation and outline

### API Documentation
- `Components/Api/ApiIndexPage.razor` - API reference index listing all namespaces
- `Components/Api/ApiNamespacePage.razor` - Namespace detail page showing types
- `Components/Api/ApiTypePage.razor` - Type detail page with members, inheritance, and declarations
- `Components/Api/CodeSnippet.razor` - Syntax-highlighted code block renderer
- `Components/Api/Prose.razor` - Styled prose wrapper with typography classes
