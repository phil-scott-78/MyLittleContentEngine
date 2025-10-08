# Services/Content

Content processing services for Markdown, Razor pages, and API reference documentation. Handles content parsing, rendering, tagging, and file operations.

## Files

### ApiReferenceContentService.cs
- **ApiReferenceContentService** - Content service for generating API reference documentation from .NET assemblies using Roslyn
- **ApiReferenceData** - Internal data structure for holding API reference data (namespaces, types, and members)

### ContentFilesService.cs
- **ContentFilesService&lt;TFrontMatter&gt;** - Service for handling content file operations including file discovery, URL creation, and content copying

### IContentService.cs
- **IContentService** - Interface defining the contract for content services responsible for parsing and handling content (pages, TOC, cross-references, etc.)

### LinkRewriter.cs
- **LinkRewriter** - Static service for handling URL rewriting in Markdown content when converted to HTML with special handling for different link types

### MarkdownContentProcessor.cs
- **MarkdownContentProcessor&lt;TFrontMatter&gt;** - Processes markdown content into HTML with front matter extraction and file watching support
- **UrlComparer** - Equality comparer for URLs that normalizes them by removing base paths and trailing slashes

### MarkdownContentService.cs
- **IMarkdownContentService&lt;TFrontMatter&gt;** - Interface extending IContentService with markdown-specific operations (content retrieval, rendering, tag handling)
- **MarkdownContentService&lt;TFrontMatter&gt;** - Content service responsible for managing Markdown-based content including parsing, rendering, and tag tracking

### RazorPageContentService.cs
- **RazorPageContentService** - Content service responsible for discovering Razor pages and their associated metadata from sidecar .yml files

### TagService.cs
- **TagService&lt;TFrontMatter&gt;** - Service for managing and processing tags in content pages including extraction, encoding, and tag-based content filtering
