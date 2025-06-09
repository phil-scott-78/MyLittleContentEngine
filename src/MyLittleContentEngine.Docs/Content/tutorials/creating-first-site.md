---
title: "Creating Your First Site"
description: "Build a complete content site from scratch using MyLittleContentEngine"
order: 1
tags:
  - tutorial
  - getting-started
  - setup
---

In this tutorial, you'll create your first content site using MyLittleContentEngine. By the end, you'll have a working Blazor application that can serve markdown content and generate static HTML files.

## What You'll Build

You'll create a simple blog-style website with:
- A home page displaying recent posts
- Individual blog post pages
- Tag-based categorization
- Static HTML generation for deployment

## Prerequisites

Before starting, ensure you have:
- .NET 8 SDK or later installed
- A code editor (Visual Studio, VS Code, or JetBrains Rider)
- Basic knowledge of C# and web development
- Familiarity with command-line tools

## Step 1: Create a New Blazor Project

Start by creating a new Blazor Server project:

```bash
dotnet new blazor-server -n MyFirstContentSite
cd MyFirstContentSite
```

## Step 2: Add MyLittleContentEngine

Add the NuGet package reference to your project:

```bash
dotnet add package MyLittleContentEngine
```

## Step 3: Create Your Front Matter Model

Create a model to define the structure of your blog post metadata. Add a new file `BlogPost.cs`:

```csharp
public class BlogFrontMatter : IFrontMatter
{
    public string Title { get; init; } = "Empty title";
    public string Description { get; init; } = string.Empty;
    public string? Uid { get; init; } = null;

    public DateTime Date { get; init; } = DateTime.Now;
    public bool IsDraft { get; init; } = false;
    public string[] Tags { get; init; } = [];

    public Metadata AsMetadata()
    {
        return new Metadata()
        {
            Title = Title,
            Description = Description,
            LastMod = Date,
            RssItem = true
        };
    }
}
```

## Step 4: Configure the Content Engine

Open `Program.cs` and configure MyLittleContentEngine. Replace the existing content with:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

// configures site wide settings
builder.Services.AddContentEngineService(() => new ContentEngineOptions
{
    SiteTitle = "My First Blog",
    SiteDescription = "My first blog's description",
    BaseUrl =  Environment.GetEnvironmentVariable("BaseHref") ?? "/",
    ContentRootPath = "Content",
});

// configures individual sections of the blog. 
// BasePageUrl  should match the configured razor pages route,
// ContentPath should match the location on disk.
// you can have multiple of these per site.
builder.Services.AddContentEngineStaticContentService(() => new ContentEngineContentOptions<BlogFrontMatter>()
{
    ContentPath = "Content",
    BasePageUrl = string.Empty
});

// this handles the creation of the CSS at runtime
builder.Services.AddMonorailCss();

var app = builder.Build();
app.UseAntiforgery();
app.UseStaticFiles();
app.MapRazorComponents<App>();

// this serves the CSS at runtime
app.UseMonorailCss();

// this is the main entry point for the content engine
await app.RunOrBuildContent(args);
```

## Step 5: Create the Content Structure

Create the content directory structure to match what we defined in `AddContentEngineStaticContentService`:

```bash
mkdir -p Content
```

## Step 6: Write Your First Blog Post

Create your first blog post at `Content/welcome.md`:

`````````markdown
---
title: "Welcome to My Content Site"
description: "Getting started with MyLittleContentEngine"
date: 2025-01-15
tags:
  - introduction
  - welcome
isDraft: false
---

Welcome to my new content site! This is my first post using MyLittleContentEngine.

## What is MyLittleContentEngine?

MyLittleContentEngine is a static site generator built specifically for .NET Blazor applications. It allows you to:

- Write content in Markdown
- Use `dotnet watch` to develop your site
- Generate static HTML for fast loading at deployment

## Getting Started

Creating content is as simple as writing Markdown files with YAML front matter. The engine handles the rest! Except the
writing!

```csharp
// Example: Strongly-typed front matter
public class BlogPost : IFrontMatter
{
    public required string Title { get; set; }
    public DateTime Date { get; set; }
    // ... other properties
}
```

Stay tuned for more posts about building amazing content sites with .NET!
`````````

## Step 7: Create Your Layout

Create `Components/Layout/MainLayout.razor` to include basic styling. This uses Tailwind CSS like syntax for styling.
Here we are defining a simple layout for our blog posts, centered in the middle of the page. It uses a flexbox layout 
which we can later use to extend the design with a sidebar or other components. For now, it will just center the content.

```razor
@inherits LayoutComponentBase

<div>
    <div class="max-w-4xl mx-auto p-4">
        <div class="flex flex-col">
            <main class="flex-1 w-full">
                @Body
            </main>
        </div>
    </div>
</div>
```

## Step 8: Create a Home Page

Create `Pages/Index.razor` to display your blog posts:

```razor
@page "/{*fileName:nonfile}"

@using System.Diagnostics.CodeAnalysis
@using MyLittleContentEngine.Models
@using MyLittleContentEngine.Services.Content
@using MyLittleContentEngine
@inject ContentEngineOptions ContentEngineOptions
@inject MarkdownContentService<BlogFrontMatter> MarkdownContentService

@if (IsLoaded)
{
    <PageTitle>@ContentEngineOptions.SiteTitle - @_post.FrontMatter.Title</PageTitle>
        <article>
        <header>
            <h1 class="text-4xl font-bold"> @_post.FrontMatter.Title</h1>
        </header>
        
        <div class="prose max-w-full">
            @((MarkupString)_postContent)
        </div>
    </article>
}
else
{
    <PageTitle>@ContentEngineOptions.SiteTitle</PageTitle>
    <p>Not found</p>
}

@code {
    private MarkdownContentPage<BlogFrontMatter>? _post;
    private string? _postContent;

    [MemberNotNull(nameof(_postContent))]
    [MemberNotNull(nameof(_post))]
    bool IsLoaded { get; set; }

    [Parameter] public required string FileName { get; init; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        var fileName = FileName;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "index";
        }

        var page = await MarkdownContentService.GetRenderedContentPageByUrlOrDefault(fileName);
        if (page == null)
        {
            return;
        }

        _post = page.Value.Page;
        _postContent = page.Value.HtmlContent;
        IsLoaded = true;
    }
}
```

## Step 9: Test Your Site

Run your site in development mode:

```bash
dotnet watch
```

Navigate to `https://localhost:5001` (or the URL shown in your terminal) to see your site in action!
