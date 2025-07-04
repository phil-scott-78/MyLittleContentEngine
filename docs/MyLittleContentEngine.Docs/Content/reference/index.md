---
title: "Reference"
description: "Complete technical reference for APIs, configuration options, and specifications"
order: 4000
---

The reference section provides comprehensive, information-oriented documentation for MyLittleContentEngine's APIs, configuration options, and technical specifications. This is your go-to resource when you need precise details about how something works.

## What You'll Find Here

This section contains:

- Complete API documentation for all public interfaces
- Detailed configuration option references  
- Content model specifications
- Service interface definitions
- Extension point documentation

## Reference Categories

### [Configuration Options](configuration-options)
Complete reference for all configuration options available in `ContentEngineOptions`, `ContentEngineContentOptions<T>`, and related configuration classes.

### [Content Models](content-models)
Detailed specifications for content models, front matter interfaces, and the built-in content types provided by the framework.

### [Services](services)
Comprehensive API documentation for all services provided by MyLittleContentEngine, including their methods, properties, and usage patterns.

## API Documentation

Coming soon:
- **IContentService Interface** - Core content management operations
- **TagService<T> API** - Tag processing and navigation
- **MarkdownContentProcessor** - Content transformation pipeline
- **OutputGenerationService** - Static site generation
- **Extension Interfaces** - How to extend framework functionality

## Configuration Reference

### Core Configuration

The framework uses a hierarchical configuration system:

```csharp
// Primary engine configuration
ContentEngineOptions
├── BlogTitle: string
├── BlogDescription: string  
├── BaseUrl: string
├── ContentRootPath: string
└── ...

// Content-specific configuration
ContentEngineContentOptions<T>
├── ContentPath: string
├── BasePageUrl: string
├── RouteTemplate: string
└── ProcessingOptions: ContentProcessingOptions
```

### Service Registration

All services are registered through extension methods:

- `AddContentEngineService()` - Core engine services
- `AddContentEngineStaticContentService<T>()` - Content management
- `AddMonorailCss()` - Styling integration
- `AddRoslynService()` - Code highlighting

## Data Structures

### Content Models

All content types implement `IFrontMatter`:

```csharp
public interface IFrontMatter
{
    string Title { get; }
    string? Description { get; }
    List<string> Tags { get; }
    // Additional properties defined by implementations
}
```

### Processing Pipeline

Content flows through a well-defined pipeline:

1. **Discovery** - Files are discovered and categorized
2. **Parsing** - Front matter and content are extracted
3. **Processing** - Markdown is transformed to HTML
4. **Enhancement** - Code highlighting, links, etc. are applied
5. **Generation** - Static files are created

## Version Compatibility

This reference documentation is for:
- MyLittleContentEngine 1.0+
- .NET 8.0+
- Blazor Server and WebAssembly

## How to Use This Reference

This documentation is designed for quick lookup and detailed investigation:

- **Quick Reference** - Use the section headers to jump to what you need
- **Detailed Investigation** - Each API includes examples and edge cases
- **Integration Guidance** - See how components work together
- **Troubleshooting** - Common issues and their resolutions

## Contributing

Found an error or missing information? Reference documentation improvements are always welcome, especially:

- Missing API details
- Unclear parameter descriptions
- Additional usage examples
- Edge case documentation