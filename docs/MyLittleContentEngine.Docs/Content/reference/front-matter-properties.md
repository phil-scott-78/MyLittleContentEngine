---
title: "Front Matter Properties"
description: "Reference guide for all available front matter properties and their usage"
order: 4002
---

All front matter implementations must implement the `IFrontMatter` interface, which defines the base contract for
content metadata.

```csharp:xmldocid
T:MyLittleContentEngine.Models.IFrontMatter
```

## Required Properties

### Title

- **Type**: `string`
- **Purpose**: The title of the content page
- **Usage**: Used in page headers, metadata, RSS feeds, and navigation
- **Example**: `title: "Getting Started with Blazor"`

## Optional Properties

### Tags

- **Type**: `string[]` (array of strings)
- **Purpose**: Content categorization and tagging
- **Usage**: Used for tag-based navigation, filtering, and content organization
- **Example**:
  ```yaml
  tags:
    - Blazor
    - .NET
    - Web Development
  ```

### Uid

- **Type**: `string?` (nullable string)
- **Purpose**: Unique identifier for the content page
- **Usage**: Used for cross-referencing and unique identification
- **Default**: `null`
- **Example**: `uid: "getting-started-blazor"`


### IsDraft

- **Type**: `bool`
- **Purpose**: Controls whether the content page will be generated
- **Usage**: When `true`, the page is excluded from static generation
- **Default**: `false`
- **Example**: `isDraft: true`

## Required Methods

## Metadata Properties

The `Metadata` class provides additional computed information for RSS feeds and sitemaps.

### Metadata Properties

#### LastMod

- **Type**: `DateTime?`
- **Purpose**: Date when the page was last modified
- **Usage**: Used in XML sitemaps and RSS feeds

#### RssItem

- **Type**: `bool`
- **Purpose**: Controls whether page should be included in RSS feed
- **Usage**: RSS feed filtering
- **Default**: `true`

#### Order

- **Type**: `int`
- **Purpose**: Controls page order in navigation or table of contents
- **Usage**: Navigation ordering, TOC generation
- **Default**: `int.MaxValue`


## YAML Front Matter Examples

### Blog Post Example

```csharp
public class BlogFrontMatter : IFrontMatter
{
    public string Title { get; init; } = "Empty title";
    public string? Uid { get; init; } = null;
    public bool IsDraft { get; init; } = false;
    public string[] Tags { get; init; } = [];

    // custom properties for blog posts
    public string Description { get; init; } = string.Empty;
    public DateTime Date { get; init; } = DateTime.Now;
    
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

Each post would have a front matter like this:

```yaml
---
title: "Getting Started with Blazor"
description: "A comprehensive guide to building your first Blazor application"
date: 2025-01-15
tags:
  - Blazor
  - .NET
  - Tutorial
isDraft: false
uid: "blazor-getting-started"
---
```

### Documentation Page Example

```csharp
internal class DocsFrontMatter : IFrontMatter
{
    public string Title { get; init; } = "Empty title";
    public bool IsDraft { get; init; } = false;
    public string[] Tags { get; init; } = [];
    public string? Uid { get; init; } = null;
    
    // custom properties for documentation pages
    public string Description { get; init; } = string.Empty;
    public int Order { get; init; } = int.MaxValue;
    
    public Metadata AsMetadata()
    {
        return new Metadata()
        {
            Title = Title,
            Description = Description,
            LastMod = DateTime.MinValue,
            RssItem = false,
            Order = Order
        };
    }

}
```

```yaml
---
title: "API Reference"
description: "Complete API documentation for MyLittleContentEngine"
order: 4001
tags:
  - Reference
  - API
isDraft: false
---
```

## Best Practices

1. **Consistent Naming**: Use consistent property names across your content
2. **Meaningful Defaults**: Provide sensible defaults for optional properties
3. **Type Safety**: Leverage C# type system for validation
4. **Required Properties**: Mark essential properties as required