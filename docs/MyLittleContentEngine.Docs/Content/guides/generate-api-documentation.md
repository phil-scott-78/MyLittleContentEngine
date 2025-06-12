---
title: "Generate API Documentation"
description: "Automatically generate interactive API documentation from .NET assemblies using ApiReferenceContentService"
order: 2100
---

This guide shows you how to automatically generate comprehensive API documentation from your .NET assemblies using the **ApiReferenceContentService**. You'll learn to configure namespace filtering, integrate with existing sites, and create interactive documentation pages that update automatically with your code.

## Prerequisites

Before generating API documentation, ensure you have the core MyLittleContentEngine configured in your application:

```bash
dotnet add package MyLittleContentEngine
```

The ApiReferenceContentService is included in the core package and doesn't require additional dependencies.

## Understanding API Documentation Generation

The ApiReferenceContentService automatically scans referenced assemblies to discover public types, methods, properties, and other members. It generates structured documentation that includes:

- **Namespace Organization**: Groups types by namespace for logical browsing
- **Type Details**: Classes, interfaces, enums with full signatures
- **Member Documentation**: Methods, properties, fields with parameters and return types
- **XML Documentation**: Integrates XML documentation comments when available
- **Syntax Highlighting**: Uses Roslyn for accurate C# syntax highlighting

### How It Works

The service operates during application startup:

1. **Assembly Discovery**: Scans referenced assemblies for public types
2. **Filtering**: Applies namespace include/exclude rules
3. **Documentation Extraction**: Reads XML documentation files if available
4. **Page Generation**: Creates browseable documentation structure
5. **Route Registration**: Registers API documentation routes automatically

## Basic Configuration

Add the ApiReferenceContentService to your application in `Program.cs`:

### Simple Setup

```csharp
// Program.cs
using MyLittleContentEngine;

var builder = WebApplication.CreateBuilder(args);

// Add your existing services
builder.Services.AddRazorComponents();
builder.Services.AddContentEngineService(_ => new ContentEngineOptions
{
    SiteTitle = "My Documentation Site",
    SiteDescription = "API Documentation and Guides",
    BaseUrl = Environment.GetEnvironmentVariable("BaseHref") ?? "/",
    ContentRootPath = "Content",
});

// Add API reference documentation
builder.Services.AddApiReferenceContentService(_ => new ApiReferenceContentOptions()
{
    IncludeNamespace = ["MyCompany.MyLibrary"],
    ExcludedNamespace = ["MyCompany.MyLibrary.Internal"]
});

var app = builder.Build();
// ... rest of configuration
```

### Advanced Configuration

```csharp
// More detailed configuration with multiple namespaces
builder.Services.AddApiReferenceContentService(_ => new ApiReferenceContentOptions()
{
    // Include multiple namespaces
    IncludeNamespace = [
        "MyCompany.Core", 
        "MyCompany.Extensions", 
        "MyCompany.Utilities"
    ],
    
    // Exclude internal/private namespaces
    ExcludedNamespace = [
        "MyCompany.Core.Internal",
        "MyCompany.Tests",
        "MyCompany.Benchmarks"
    ]
});
```

## Creating API Documentation Pages

The service automatically registers routes, but you need to create the corresponding Razor components to display the documentation.

### Required Components

Create these components in your `Components/Api/` directory:

#### 1. API Index Page (`ApiIndexPage.razor`)

```razor
@page "/api/"
@using MyLittleContentEngine.Services.Content
@inject ApiReferenceContentService ApiService
@inject ContentEngineOptions ContentEngineOptions

<PageTitle>@ContentEngineOptions.SiteTitle - API Reference</PageTitle>

@if (IsLoaded)
{
    <article>
        <header>
            <h1 class="text-4xl font-bold text-gray-900 dark:text-gray-50">API Reference</h1>
            <p class="text-gray-700 dark:text-gray-300 mt-4">Browse the API documentation by namespace.</p>
        </header>

        <div class="prose dark:prose-invert max-w-none mt-8">
            <h2>Namespaces</h2>
            <div class="grid gap-4 not-prose">
                @foreach (var ns in _namespaces.OrderBy(n => n.Name))
                {
                    <div class="border border-gray-200 dark:border-gray-800 rounded-lg p-4">
                        <a href="@ns.Url" class="text-lg font-semibold text-blue-600 dark:text-blue-400 hover:underline">
                            @ns.Name
                        </a>
                        <p class="text-gray-600 dark:text-gray-400 mt-1">
                            @ns.Summary
                        </p>
                        <p class="text-sm text-gray-500 mt-2">
                            @ns.Types.Count type(s)
                        </p>
                    </div>
                }
            </div>
        </div>
    </article>
}
else
{
    <p>Loading API documentation...</p>
}

@code {
    private ImmutableList<ApiNamespace> _namespaces = [];
    bool IsLoaded { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _namespaces = await ApiService.GetNamespacesAsync();
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading API data: {ex.Message}");
            IsLoaded = true;
        }
    }
}
```

#### 2. Namespace Page (`ApiNamespacePage.razor`)

```razor
@page "/api/namespace/{namespaceName}"
@using MyLittleContentEngine.Services.Content
@inject ApiReferenceContentService ApiService

@if (Namespace != null)
{
    <article>
        <header>
            <h1 class="text-4xl font-bold text-gray-900 dark:text-gray-50">@Namespace.Name</h1>
            @if (!string.IsNullOrEmpty(Namespace.Summary))
            {
                <p class="text-gray-700 dark:text-gray-300 mt-4">@Namespace.Summary</p>
            }
        </header>

        <div class="mt-8">
            <h2 class="text-2xl font-semibold mb-4">Types</h2>
            <div class="grid gap-4">
                @foreach (var type in Namespace.Types.OrderBy(t => t.Name))
                {
                    <div class="border border-gray-200 dark:border-gray-800 rounded-lg p-4">
                        <a href="@type.Url" class="text-lg font-semibold text-blue-600 dark:text-blue-400 hover:underline">
                            @type.Name
                        </a>
                        <span class="ml-2 text-sm bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 px-2 py-1 rounded">
                            @type.Kind
                        </span>
                        @if (!string.IsNullOrEmpty(type.Summary))
                        {
                            <p class="text-gray-600 dark:text-gray-400 mt-2">@type.Summary</p>
                        }
                    </div>
                }
            </div>
        </div>
    </article>
}

@code {
    [Parameter] public string NamespaceName { get; set; } = string.Empty;
    
    private ApiNamespace? Namespace;

    protected override async Task OnInitializedAsync()
    {
        var namespaces = await ApiService.GetNamespacesAsync();
        Namespace = namespaces.FirstOrDefault(n => n.Name == NamespaceName);
    }
}
```

#### 3. Type Detail Page (`ApiTypePage.razor`)

```razor
@page "/api/type/{typeId}"
@using MyLittleContentEngine.Services.Content
@inject ApiReferenceContentService ApiService

@if (Type != null)
{
    <article>
        <header>
            <div class="flex items-center gap-2 mb-2">
                <span class="text-sm bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 px-2 py-1 rounded">
                    @Type.Kind
                </span>
            </div>
            <h1 class="text-4xl font-bold text-gray-900 dark:text-gray-50">@Type.Name</h1>
            @if (!string.IsNullOrEmpty(Type.Summary))
            {
                <p class="text-gray-700 dark:text-gray-300 mt-4">@Type.Summary</p>
            }
        </header>

        @if (!string.IsNullOrEmpty(Type.Syntax))
        {
            <div class="mt-8">
                <h2 class="text-2xl font-semibold mb-4">Declaration</h2>
                <Components.Api.CodeSnippet Language="csharp" Code="@Type.Syntax" />
            </div>
        }

        @if (Type.Properties.Any())
        {
            <div class="mt-8">
                <h2 class="text-2xl font-semibold mb-4">Properties</h2>
                <div class="space-y-4">
                    @foreach (var property in Type.Properties)
                    {
                        <div class="border-l-4 border-blue-200 dark:border-blue-800 pl-4">
                            <h3 class="font-semibold text-lg">@property.Name</h3>
                            @if (!string.IsNullOrEmpty(property.Summary))
                            {
                                <p class="text-gray-600 dark:text-gray-400">@property.Summary</p>
                            }
                            <code class="text-sm bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded">@property.Type</code>
                        </div>
                    }
                </div>
            </div>
        }

        @if (Type.Methods.Any())
        {
            <div class="mt-8">
                <h2 class="text-2xl font-semibold mb-4">Methods</h2>
                <div class="space-y-6">
                    @foreach (var method in Type.Methods)
                    {
                        <div class="border-l-4 border-green-200 dark:border-green-800 pl-4">
                            <h3 class="font-semibold text-lg">@method.Name</h3>
                            @if (!string.IsNullOrEmpty(method.Summary))
                            {
                                <p class="text-gray-600 dark:text-gray-400 mb-2">@method.Summary</p>
                            }
                            <Components.Api.CodeSnippet Language="csharp" Code="@method.Syntax" />
                        </div>
                    }
                </div>
            </div>
        }
    </article>
}

@code {
    [Parameter] public string TypeId { get; set; } = string.Empty;
    
    private ApiType? Type;

    protected override async Task OnInitializedAsync()
    {
        Type = await ApiService.GetTypeAsync(TypeId);
    }
}
```

#### 4. Code Snippet Component (`CodeSnippet.razor`)

```razor
@using MyLittleContentEngine.Services.Content.Roslyn
@inject RoslynHighlighterService RoslynService

<div class="my-4">
    <pre class="bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg p-4 overflow-x-auto text-sm"><code>@((MarkupString)_highlightedCode)</code></pre>
</div>

@code {
    [Parameter] public string Language { get; set; } = "csharp";
    [Parameter] public string Code { get; set; } = string.Empty;
    
    private string _highlightedCode = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrEmpty(Code))
        {
            _highlightedCode = await RoslynService.HighlightAsync(Code);
        }
    }
}
```

## Integrating with Router

Update your application's router to include the API documentation routes:

```razor
<!-- Components/Routes.razor -->
<Router AppAssembly="@typeof(Program).Assembly" 
        AdditionalAssemblies="new[] { typeof(Components.Api.ApiIndexPage).Assembly }">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(Layout.MainLayout)"/>
    </Found>
</Router>
```

## Navigation Integration

Add navigation links to your main layout:

```razor
<!-- Components/Layout/MainLayout.razor -->
<nav class="mb-6 border-b border-gray-200 dark:border-gray-800 pb-4">
    <div class="flex gap-4">
        <a href="/" class="text-blue-600 dark:text-blue-400 hover:underline">Home</a>
        <a href="/api/" class="text-blue-600 dark:text-blue-400 hover:underline">API Reference</a>
        <!-- other navigation links -->
    </div>
</nav>
```

## Working Example

You can see a complete working example in the [`examples/ApiReferenceExample`](https://github.com/your-repo/examples/ApiReferenceExample) directory, which demonstrates:

- **Complete Configuration**: Shows how to set up the service with proper filtering
- **All Required Components**: Includes all the Razor components needed for browsing
- **Navigation Integration**: Demonstrates how to integrate with your site navigation
- **Styling**: Uses TailwindCSS classes for a clean, professional appearance

To run the example:

```bash
cd examples/ApiReferenceExample
dotnet watch
```

Then browse to `/api/` to see the generated API documentation.

## Configuration Options

### Namespace Filtering

Control which parts of your codebase appear in the documentation:

```csharp
builder.Services.AddApiReferenceContentService(_ => new ApiReferenceContentOptions()
{
    // Only include these namespaces
    IncludeNamespace = [
        "MyCompany.PublicApi",
        "MyCompany.Extensions"
    ],
    
    // Exclude these even if they match includes
    ExcludedNamespace = [
        "MyCompany.PublicApi.Internal", 
        "MyCompany.Tests"
    ]
});
```

### XML Documentation Integration

To include XML documentation comments in your API documentation:

1. **Enable XML generation** in your project file:

```xml
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

2. **The service automatically discovers** and includes XML documentation when available

## Styling and Customization

The API documentation components use standard HTML and CSS classes, making them easy to customize:

- **TailwindCSS Compatible**: Uses utility classes that work with TailwindCSS or MonorailCSS
- **Dark Mode Support**: Includes dark mode variants for all styling
- **Responsive Design**: Works well on mobile and desktop
- **Customizable Components**: Override any component to match your site design

## Troubleshooting

### No API Documentation Appears

- **Check Namespace Configuration**: Ensure your `IncludeNamespace` settings match your actual namespaces
- **Verify Assembly References**: The service only scans assemblies that are referenced by your application
- **Check Route Registration**: Ensure your router includes the API components assembly

### Missing XML Documentation

- **Enable XML Generation**: Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to your project file
- **Check Output Directory**: Ensure XML files are being generated alongside your assemblies
- **Rebuild Project**: XML documentation is generated at build time

### Styling Issues  

- **Include CSS Framework**: Ensure TailwindCSS, MonorailCSS, or similar utility framework is configured
- **Check Component Imports**: Verify all required `@using` statements are in `_Imports.razor`
