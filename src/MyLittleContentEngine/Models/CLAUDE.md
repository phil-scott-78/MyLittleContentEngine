# Models

Core data models and domain entities for MyLittleContentEngine. Contains all DTOs, records, and interfaces that represent content, metadata, navigation, and API reference structures.

## Files

### MarkdownContentPage.cs
- **MarkdownContentPage&lt;TFrontMatter&gt;** - Generic page model containing rendered Markdown content with front matter, URL, and table of contents
- **OutlineEntry** - Record representing a table of contents entry with hierarchical children

### CrossReference.cs
- **CrossReference** - Represents cross-references for xref links and ToC navigation with UID, title, and URL

### Tag.cs
- **Tag** - Content tag model with display name, URL-encoded name, and navigation URL

### SearchIndex.cs
- **SearchIndexDocument** - Single searchable document with URL, title, content, headings, description, and priority
- **SearchIndex** - Complete search index container with documents and generation timestamp

### ApiReference.cs
- **ApiReferenceItem** - Abstract base record for all API reference items (namespaces, types, members)
- **ApiNamespace** - Namespace documentation with contained types
- **ApiType** - Type documentation (class, interface, struct, enum) with members
- **ApiMember** - Member documentation (method, property, field) with parameters
- **ApiParameter** - Method parameter with type, default value, and documentation

### IFrontMatter.cs
- **IFrontMatter** - Interface defining front matter contract for Markdown pages (title, tags, draft status, redirects, sections)

### Metadata.cs
- **Metadata** - Additional computed page metadata for RSS/sitemap (last modified, description, order, section)

### PageToGenerate.cs
- **PageToGenerate** - Record defining a page to generate during static build with URL, output file, and metadata

### ContentToCopy.cs
- **ContentToCopy** - Record defining content to copy from source to target during build with optional extension exclusions

### ContentToCreate.cs
- **ContentToCreate** - Record defining content to write during build with target path and raw bytes

### BrokenLink.cs
- **BrokenLink** - Record representing a broken internal link found during static site generation validation (source page, broken URL, link type, element type)
- **LinkType** - Enum categorizing link attribute types (Href, Src)

### SearchIndexJsonContext.cs
- **SearchIndexJsonContext** - JSON source generator context for SearchIndex serialization with camelCase naming
