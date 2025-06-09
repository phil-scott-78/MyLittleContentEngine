---
title: "Using UI Elements"
description: "Learn how to enhance your site with pre-built UI components from MyLittleContentEngine.UI"
order: 1050
---

MyLittleContentEngine includes a set of pre-built UI components that make it easy to create professional, responsive layouts for your content sites. These components handle common patterns like navigation, page outlines, and headers.

## Setting Up UI Components

### Add the UI Package

First, add a reference to the MyLittleContentEngine.UI project in your `.csproj` file:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/MyLittleContentEngine.UI/MyLittleContentEngine.UI.csproj" />
</ItemGroup>
```

### Import Components

Add the UI components to your `_Imports.razor` file:

```razor
@using MyLittleContentEngine.UI.Components
```

### Include Required Scripts

Add the UI scripts to your `App.razor`:

```razor
<script src="_content/MyLittleContentEngine.UI/scripts.js" defer></script>
```

## Available Components

MyLittleContentEngine.UI provides five main components that work together to create rich content experiences:

- **TableOfContentsNavigation** - Site-wide navigation based on your content structure
- **OutlineNavigation** - Page outline showing headings within the current page
- **BaseHeader** - Site header with title, theme toggle, and custom actions
- **PageHeader** - Styled page title and description component
- **ThemeToggle** - Dark/light mode toggle button

## TableOfContentsNavigation

The `TableOfContentsNavigation` component automatically generates navigation based on your content structure and front matter.

### Basic Usage

```razor
@using MyLittleContentEngine.Services.Content.TableOfContents
@using System.Collections.Immutable
@inject TableOfContentService TableOfContentService

<TableOfContentsNavigation TableOfContents="@_tableOfContents" />

@code {
    private ImmutableList<TableOfContentEntry>? _tableOfContents;

    protected override async Task OnInitializedAsync()
    {
        _tableOfContents = await TableOfContentService.GetTableOfContentsAsync();
    }
}
```

### Responsive Sidebar Layout

Create a sidebar navigation layout like the UserInterfaceExample:

```razor
<div class="min-h-screen flex">
    <!-- Navigation Sidebar -->
    <div class="w-80 bg-neutral-50 border-r border-neutral-200 flex-shrink-0">
        <div class="sticky top-0 h-screen overflow-y-auto p-6">
            <TableOfContentsNavigation TableOfContents="@_tableOfContents" />
        </div>
    </div>
    
    <!-- Main Content -->
    <div class="flex-1 min-w-0">
        <main class="w-full">
            @Body
        </main>
    </div>
</div>
```

### Customization Options

The component accepts several styling parameters:

```razor
<TableOfContentsNavigation 
    TableOfContents="@_tableOfContents"
    SectionHeaderStructureClass="font-bold text-lg pt-4 pb-2"
    SectionHeaderColorClass="text-primary-800 dark:text-primary-200"
    LinkStructureClass="block py-2 px-3 text-sm"
    LinkColorClass="text-gray-700 hover:text-primary-600" />
```

## OutlineNavigation

The `OutlineNavigation` component shows the outline of the current page based on its headings (h2, h3, etc.).

### Basic Implementation

```razor
@using MyLittleContentEngine.Models
@inject MarkdownContentService<YourFrontMatter> ContentService

<OutlineNavigation Outline="@_outline" BaseUrl="@_currentUrl" />

@code {
    private OutlineEntry[]? _outline;
    private string _currentUrl = "";

    protected override async Task OnInitializedAsync()
    {
        var page = await ContentService.GetRenderedContentPageByUrlOrDefault(fileName);
        if (page != null)
        {
            _outline = page.Value.Outline;
            _currentUrl = page.Value.Page.NavigateUrl;
        }
    }
}
```

### Three-Column Layout

Combine TableOfContents and Outline for a comprehensive layout:

```razor
<div class="flex">
    <!-- Table of Contents -->
    <div class="w-80 bg-neutral-50 border-r border-neutral-200 flex-shrink-0">
        <div class="sticky top-0 h-screen overflow-y-auto p-6">
            <TableOfContentsNavigation TableOfContents="@_tableOfContents" />
        </div>
    </div>
    
    <!-- Main Content -->
    <div class="flex-1 min-w-0">
        <article class="p-8 max-w-4xl">
            <h1>@title</h1>
            <div class="prose max-w-none">
                @((MarkupString)content)
            </div>
        </article>
    </div>
    
    <!-- Page Outline -->
    <div class="w-64 flex-shrink-0 border-l border-neutral-200">
        <div class="sticky top-0 h-screen overflow-y-auto p-6">
            <OutlineNavigation Outline="@_outline" BaseUrl="@_currentUrl" />
        </div>
    </div>
</div>
```

## BaseHeader

The `BaseHeader` component provides a complete site header with title, navigation, and theme toggle.

### Simple Header

```razor
<BaseHeader />
```

This creates a header with your site title (from `ContentEngineOptions.SiteTitle`) and a theme toggle button.

### Custom Header with Actions

```razor
<BaseHeader 
    HeaderColorClass="bg-primary-100 dark:bg-primary-900"
    TitleColorClass="text-primary-800 dark:text-primary-100"
    ExtraButtons="@customButtons" />

@code {
    private readonly RenderFragment customButtons = 
        @<div>
            <a href="/docs" class="text-sm font-medium">Documentation</a>
            <a href="/blog" class="text-sm font-medium">Blog</a>
        </div>;
}
```

### Header with Icon

```razor
<BaseHeader HeaderIcon="@siteIcon" />

@code {
    private readonly RenderFragment siteIcon = 
        @<svg class="w-8 h-8 mr-3" viewBox="0 0 24 24">
            <!-- Your site icon SVG -->
        </svg>;
}
```

## PageHeader

Use `PageHeader` for consistent page titles and descriptions:

```razor
<PageHeader Title="@pageTitle">
    @pageDescription
</PageHeader>
```

### With Custom Styling

```razor
<PageHeader 
    Title="@pageTitle"
    TitleStructureClass="text-4xl font-bold"
    TitleColorClass="text-primary-900 dark:text-primary-100"
    ContentStructureClass="text-lg mt-4"
    ContentColorClass="text-gray-600 dark:text-gray-300">
    @pageDescription
</PageHeader>
```

## ThemeToggle

The `ThemeToggle` component is automatically included in `BaseHeader`, but you can use it standalone:

```razor
<ThemeToggle 
    ButtonStructureClass="p-2 rounded-md"
    ButtonColorClass="bg-gray-100 hover:bg-gray-200 dark:bg-gray-800 dark:hover:bg-gray-700" />
```

## Building a Complete Layout

Here's how to combine all components to create a layout like the UserInterfaceExample:

### MainLayout.razor

```razor
@using MyLittleContentEngine.Services.Content.TableOfContents
@using System.Collections.Immutable
@inherits LayoutComponentBase
@inject TableOfContentService TableOfContentService

<div class="min-h-screen flex">
    <!-- Navigation Sidebar -->
    <div class="w-80 bg-neutral-50 dark:bg-neutral-900 border-r border-neutral-200 dark:border-neutral-700 flex-shrink-0">
        <div class="sticky top-0 h-screen overflow-y-auto p-6">
            <div class="mb-8">
                <h1 class="text-xl font-bold text-neutral-900 dark:text-neutral-100">Your Site Title</h1>
                <p class="text-sm text-neutral-600 dark:text-neutral-400 mt-1">Site description</p>
            </div>
            <TableOfContentsNavigation TableOfContents="@_tableOfContents" />
        </div>
    </div>
    
    <!-- Main Content Area -->
    <div class="flex-1 min-w-0">
        <main class="w-full">
            @Body
        </main>
    </div>
</div>

@code {
    private ImmutableList<TableOfContentEntry>? _tableOfContents;

    protected override async Task OnInitializedAsync()
    {
        _tableOfContents = await TableOfContentService.GetTableOfContentsAsync();
    }
}
```

### Pages.razor

```razor
@page "/{*fileName:nonfile}"
@using MyLittleContentEngine.Models
@using MyLittleContentEngine.Services.Content
@inject MarkdownContentService<YourFrontMatter> ContentService

<div class="flex">
    <!-- Content -->
    <div class="flex-1 min-w-0">
        <article class="p-8 max-w-4xl">
            <header class="mb-8">
                <h1 class="text-4xl font-bold">@_title</h1>
                @if (!string.IsNullOrEmpty(_description))
                {
                    <p class="text-lg text-gray-600 mt-2">@_description</p>
                }
            </header>
            <div class="prose max-w-none">
                @((MarkupString)_content)
            </div>
        </article>
    </div>
    
    <!-- Page Outline -->
    <div class="w-64 flex-shrink-0 border-l border-neutral-200">
        <div class="sticky top-0 h-screen overflow-y-auto p-6">
            <OutlineNavigation Outline="@_outline" BaseUrl="@_currentUrl" />
        </div>
    </div>
</div>

@code {
    private string? _content;
    private string _title = "";
    private string _description = "";
    private OutlineEntry[]? _outline;
    private string _currentUrl = "";

    [Parameter] public required string FileName { get; init; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        var fileName = string.IsNullOrWhiteSpace(FileName) ? "index" : FileName;
        _currentUrl = "/" + fileName;

        var page = await ContentService.GetRenderedContentPageByUrlOrDefault(fileName);
        if (page != null)
        {
            _content = page.Value.HtmlContent;
            _title = page.Value.Page.FrontMatter.Title;
            _description = page.Value.Page.FrontMatter.Description;
            _outline = page.Value.Outline;
        }
    }
}
```

## Styling and Customization

All UI components use Tailwind CSS classes and provide extensive customization options through parameters. Each component accepts separate structure and color class parameters, allowing you to:

- **Structure Classes**: Control layout, spacing, and typography
- **Color Classes**: Customize colors for light and dark themes

### Example Customization

```razor
<TableOfContentsNavigation 
    TableOfContents="@_toc"
    SectionHeaderStructureClass="font-semibold text-base pb-3 border-b"
    SectionHeaderColorClass="text-blue-800 dark:text-blue-200 border-blue-200 dark:border-blue-700"
    LinkStructureClass="block py-2 pl-4 border-l-2 text-sm"
    LinkColorClass="border-transparent hover:border-blue-400 text-gray-700 hover:text-blue-600 dark:text-gray-300 dark:hover:text-blue-400" />
```

This systematic approach allows you to create professional, responsive layouts that work well with your content structure and provide excellent user experience across devices.