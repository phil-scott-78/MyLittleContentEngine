---
title: "Generate API Documentation"
description: "Automatically generate interactive API documentation from .NET assemblies using ApiReferenceContentService"
uid: "docs.guides.generate-api-documentation"
order: 2100
---

This guide shows you how to automatically generate comprehensive API documentation from your .NET assemblies using the
**ApiReferenceContentService**. You'll learn to configure namespace filtering, integrate with existing sites, and create
interactive documentation pages that update automatically with your code.

## Prerequisites

Before generating API documentation, ensure you have the core MyLittleContentEngine configured in your application and [Roslyn connected](../getting-started/connecting-to-roslyn)

The ApiReferenceContentService is included in the core package and doesn't require additional dependencies.

## Understanding API Documentation Generation

The ApiReferenceContentService automatically scans referenced assemblies to discover public types, methods, properties,
and other members. It generates structured documentation that includes:

- **Namespace Organization**: Groups types by namespace for logical browsing
- **Type Details**: Classes, interfaces, enums with full signatures
- **Member Documentation**: Methods, properties, fields with parameters and return types
- **XML Documentation**: Integrates XML documentation comments when available
- **Syntax Highlighting**: Uses Roslyn for accurate C# syntax highlighting

### How It Works

The service operates during application startup and during hot reload, performing the following steps:

1. **Assembly Discovery**: Scans referenced assemblies for public types
2. **Filtering**: Applies namespace include/exclude rules
3. **Documentation Extraction**: Reads XML documentation files if available

## Basic Configuration

Add the ApiReferenceContentService to your application in `Program.cs`:

### Simple Setup

```csharp
// Add API reference content service 
builder.Services.AddApiReferenceContentService(_ => new ApiReferenceContentOptions()
{
    IncludeNamespace = ["MyLittleContentEngine"],
    ExcludedNamespace = ["MyLittleContentEngine.Tests"],
});
```

This will register a `ApiReferenceContentService` that can be used to generate API documentation for the specified
namespaces.

## URL Configuration

The `ApiReferenceContentService` provides flexible URL configuration through the `ApiReferenceUrlOptions` class. This
allows you to customize how API documentation URLs are structured and which types of pages are generated during static
site generation. These URLs must match the routing configuration of your razor pages for proper static site generation.

### Basic URL Configuration

```csharp
builder.Services.AddApiReferenceContentService(_ => new ApiReferenceContentOptions()
{
    BasePageUrl = "docs", // Changes base from "api" to "docs"
    IncludeNamespace = ["MyLittleContentEngine"],
    UrlOptions = new ApiReferenceUrlOptions
    {
        // Enable/disable specific page types
        GenerateNamespacePages = true,  // Default: true
        GenerateTypePages = true,       // Default: true
        
        // Customize URL templates
        NamespaceUrlTemplate = "/{BasePageUrl}/ns/{Slug}",
        TypeUrlTemplate = "/{BasePageUrl}/types/{Slug}",
        
        // Customize output file paths
        NamespaceOutputTemplate = "{BasePageUrl}/ns/{Slug}/index.html",
        TypeOutputTemplate = "{BasePageUrl}/types/{Slug}/index.html"
    }
});
```

### URL Template Placeholders

URL templates support the following placeholders:

- **`{BasePageUrl}`** - The base URL path (e.g., "api", "docs")
- **`{Slug}`** - URL-friendly identifier (e.g., "system.collections.generic.list-1")
- **`{Name}`** - Display name of the item
- **`{Namespace}`** - Namespace name (types only)
- **`{TypeName}`** - Type name (types only)

### Common URL Patterns

#### Microsoft Docs Style

```csharp
UrlOptions = new ApiReferenceUrlOptions
{
    NamespaceUrlTemplate = "/{BasePageUrl}/namespaces/{Slug}",
    TypeUrlTemplate = "/{BasePageUrl}/types/{Slug}",
    NamespaceOutputTemplate = "{BasePageUrl}/namespaces/{Slug}.html",
    TypeOutputTemplate = "{BasePageUrl}/types/{Slug}.html"
}
```

#### Flat Structure

```csharp
UrlOptions = new ApiReferenceUrlOptions
{
    NamespaceUrlTemplate = "/{BasePageUrl}/{Slug}",
    TypeUrlTemplate = "/{BasePageUrl}/{Slug}",
    NamespaceOutputTemplate = "{BasePageUrl}/{Slug}.html",
    TypeOutputTemplate = "{BasePageUrl}/{Slug}.html"
}
```

#### Hierarchical Structure

```csharp
UrlOptions = new ApiReferenceUrlOptions
{
    NamespaceUrlTemplate = "/{BasePageUrl}/reference/namespaces/{Slug}",
    TypeUrlTemplate = "/{BasePageUrl}/reference/types/{Slug}",
    NamespaceOutputTemplate = "{BasePageUrl}/reference/namespaces/{Slug}/index.html",
    TypeOutputTemplate = "{BasePageUrl}/reference/types/{Slug}/index.html"
}
```

### Disabling Page Types

You can disable specific types of documentation pages:

```csharp
UrlOptions = new ApiReferenceUrlOptions
{
    // Only generate type pages, skip namespace overview pages
    GenerateNamespacePages = false,
    GenerateTypePages = true
}
```

This is useful when you want a minimal documentation structure or when integrating with existing documentation systems.

### Slug Format

The `Slug` property provides URL-friendly identifiers:

- **Namespaces**: `system.collections.generic`
- **Types**: `system.collections.generic.list-1` (generic parameters become `-1`, `-2`, etc.)
- **Members**: `system.collections.generic.list-1.add`

These slugs are automatically generated and ensure consistent, predictable URLs for your API documentation.


## ApiReferenceContentService Methods Reference

The `ApiReferenceContentService` provides several methods for accessing API documentation data programmatically.
Individual symbols can be retrieved by their XmlDocId or Microsoft-style identifier (e.g.,
`MyNamespace.MyType.MyMethod`).

These methods allow you to build custom documentation pages or integrate API information into your application's UI.

Each Symbol (namespace, type, member) has a unique identifier that can be used to retrieve it named `Slug`. This
identifier
is typically in the format `Namespace.Type.Member` for members, and `Namespace.Type` for types. These are appropriate
for
use in URLs for navigation.

### Namespace Methods

- **`GetNamespacesAsync()`** - Returns all discovered API namespaces
- **`GetNamespaceByNameAsync(string name)`** - Gets a specific namespace by its name
- **`GetNamespaceByXmlDocIdAsync(string xmlDocId)`** - Gets a namespace by its XML documentation ID
- **`GetNamespaceBySlugAsync(string slug)`** - Gets a namespace by its Microsoft-style
  identifier

### Type Methods

- **`GetTypesAsync()`** - Returns all discovered API types across all namespaces
- **`GetTypeByNameAsync(string namespaceName, string typeName)`** - Gets a specific type by namespace and type name
- **`GetTypeByXmlDocIdAsync(string xmlDocId)`** - Gets a type by its XML documentation ID
- **`GetTypeBySlugAsync(string slug)`** - Gets a type by its Microsoft-style identifier

### Member Methods

- **`GetMembersAsync()`** - Returns all discovered API members across all types
- **`GetMembersBySlugAsync(string slug)`** - Gets members by Microsoft-style identifier (can
  return multiple for overloads)

These methods allow you to build custom documentation pages or integrate API information into your application's UI
components.
