---
title: "Project Structure and Configuration"
description: "Understanding the anatomy of a MyLittleContentEngine project and how components work together"
uid: "docs.under-the-hood.project-structure-and-configuration"
order: 3010
---

Understanding the structure and configuration of a MyLittleContentEngine project is essential for both developers and AI
systems implementing the framework. This guide explains the architectural decisions, required components, and how
everything fits together.

## Project Anatomy

A typical MyLittleContentEngine project follows this structure:

```
MyContentSite/
├── Program.cs                          # Application entry point and configuration
├── MyContentSite.csproj                # Project file with dependencies and build configuration
├── Components/                         # Blazor components and layouts
│   ├── App.razor                       # Root application component
│   ├── Routes.razor                    # Application routing configuration
│   ├── _Imports.razor                  # Global component imports
│   └── Layout/                         # Layout components
│       ├── MainLayout.razor            # Primary site layout
│       ├── Home.razor                  # Homepage component
│       └── Pages.razor                 # Content page display component
├── Content/                            # Markdown content files
│   ├── index.md                        # Homepage content
│   ├── about.md                        # About page
│   ├── logo.png                        # Content image asset
│   └── blog/                           # Blog posts subdirectory
│       ├── image.jpg                   # Example image asset
│       ├── post-one.md
│       └── post-two.md
└── appsettings.json                    # Application configuration
```

## Core Components Explained

### Program.cs - The Foundation

`Program.cs` is the heart of your MyLittleContentEngine application. It serves two critical functions:

1. **Development Mode**: Runs as a Blazor Server application with hot reload
2. **Build Mode**: Generates static HTML files for deployment

```csharp:path
examples/MinimalExample/Program.cs
```

### Required Blazor Components

MyLittleContentEngine requires specific Blazor components to function properly:

#### App.razor - Application Root

This is the main entry point for your Blazor application. It sets up the root component and includes the main layout. In
this file we are loading the styles and scripts required for the application, using the `LinkService` to ensure
[links resolve correctly during both development and production modes](../guides/linking-documents-and-media).

```razor:path
examples/MinimalExample/Components/App.razor
```

#### Routes.razor - Routing Configuration

This generally does not need to be modified, as it sets up the routing for your application. It includes the main layout
and handles navigation between pages.

```razor:path
examples/MinimalExample/Components/Routes.razor

```

#### _Imports.razor - Global Imports

The namespaces `MyLittleContentEngine`, `MyLittleContentEngine.Services.Content`, `MyLittleContentEngine.UI.Components`,
and `MyLittleContentEngine.Models` are used frequently across components, so it's convenient to import them globally.

```razor:path
examples/MinimalExample/Components/_Imports.razor
```

## Front Matter Models

Every MyLittleContentEngine project requires a front matter model that implements `IFrontMatter`:

```csharp:path
examples/MinimalExample/BlogFrontMatter.cs

```

This model defines what metadata can be extracted from YAML front matter in your markdown files.

## Project File Requirements

### Minimum .csproj Configuration

A typical csproj file for a MyLittleContentEngine project includes the necessary SDK and package references.

Make sure to include a `Watch` for your content directory to enable hot reloading during development. 
Below is an example

```xml

<Project Sdk="Microsoft.NET.Sdk.Web">
    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="MyLittleContentEngine" Version="[latest]"/>
        <PackageReference Include="MyLittleContentEngine.UI" Version="[latest]"/>
        <PackageReference Include="MyLittleContentEngine.MonorailCss" Version="[latest]"/>
    </ItemGroup>

    <ItemGroup>
        <Watch Include="Content\**\*.*"/>
    </ItemGroup>
</Project>
```
