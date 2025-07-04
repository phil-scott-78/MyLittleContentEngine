---
title: "Hot Reload Architecture"
description: "Understanding the lazy caching and file watching system that powers MyLittleContentEngine's hot reload functionality"
order: 3001
---

MyLittleContentEngine provides seamless hot reload functionality during development, allowing content changes to be
reflected immediately without restarting the application. This capability is built on two key infrastructure components
working together: **LazyAndForgetful** for intelligent caching and **ContentEngineFileWatcher** for file system
monitoring.

## Architecture Overview

The hot reload system operates on a simple but powerful principle: expensive operations (like parsing all markdown
files) are cached until invalidated by file changes, at which point they are recomputed with debouncing to prevent
excessive recalculation.

```mermaid
graph TB
    A[File System Changes] --> B[ContentEngineFileWatcher]
    B --> C[LazyAndForgetful Refresh]
    C --> D[Debounced Refresh]
    D --> E[Content Reprocessing]
    E --> F[Updated Cache]
    F --> G[UI Updates]
```

## LazyAndForgetful: Smart Caching

The `LazyAndForgetful<T>` class is a thread-safe, lazy-loading cache that can "forget" its value and reload it on
demand. It's designed specifically for expensive operations that need to be invalidated when dependencies change.

For example, it is used to cache the results of processing markdown files into a dictionary of content pages. It is also
used in the caching of the MSBuild workspace that drives the Roslyn interactions. This allows for these expensive
operations
to be performed once with the ability to refresh the cache when the underlying files change.

### Key Features

- **Lazy Loading**: Values are computed only when first accessed
- **Thread Safety**: Safe for concurrent access from multiple threads
- **Debounced Refresh**: Multiple refresh requests are coalesced to prevent excessive recomputation
- **Async-First**: Built for async operations throughout

### Usage Pattern

```csharp
// Create a lazy cache with expensive factory operation
var contentCache = new LazyAndForgetful<ConcurrentDictionary<string, MarkdownContentPage<TFrontMatter>>>(
    async () => await ProcessAllMarkdownFiles()
);

// Access cached value (computed on first access)
var content = await contentCache.Value;

// Invalidate cache when files change
contentCache.Refresh(); // Triggers debounced recomputation
```

### Debouncing Logic

The debouncing mechanism prevents excessive recomputation during rapid file changes:

- **Default Delay**: 50ms (configurable)
- **Coalescing**: Multiple refresh calls within the debounce window are combined
- **Cancellation**: New refresh requests cancel pending ones

This is particularly important during development when editors might save files multiple times in quick succession or
when batch operations modify many files.

## ContentEngineFileWatcher: File System Monitoring

The `ContentEngineFileWatcher` monitors specified directories for file changes and triggers refresh operations. It
supports both specific file pattern watching and general directory monitoring.

### Capabilities

- **Pattern Matching**: Watch specific file types (e.g., `*.md`, `*.razor`)
- **Directory Monitoring**: Watch entire directories for any changes
- **Subdirectory Support**: Optionally include subdirectories in monitoring
- **Path Validation**: Automatically handles non-existent directories gracefully

### Hot Reload Integration

The watcher includes special support for Blazor's hot reload mechanism through the
[
`MetadataUpdateHandler`](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.metadata.metadataupdatehandlerattribute?view=net-9.0)
attribute, enabling integration with IDE and `dotnet watch` tooling.

## Integration in Content Services

The hot reload architecture is seamlessly integrated into content services. Here's how it works in practice:

### MarkdownContentService Example

```csharp
public class MarkdownContentService<TFrontMatter> : IMarkdownContentService<TFrontMatter>
{
    private readonly LazyAndForgetful<ConcurrentDictionary<string, MarkdownContentPage<TFrontMatter>>> _contentCache;

    public MarkdownContentService(
        ContentEngineContentOptions<TFrontMatter> engineContentOptions,
        IContentEngineFileWatcher fileWatcher,
        // ... other services
    )
    {
        // Set up lazy cache with expensive content processing operation
        _contentCache = new LazyAndForgetful<ConcurrentDictionary<string, MarkdownContentPage<TFrontMatter>>>(
            async () => await _contentProcessor.ProcessContentFiles()
        );

        // Set up file watching to trigger cache refresh
        fileWatcher.AddPathsWatch([engineContentOptions.ContentPath], NeedsRefresh);
    }

    private void NeedsRefresh() => _contentCache.Refresh();

    public async Task<MarkdownContentPage<TFrontMatter>?> GetContentPageByUrlOrDefault(string url)
    {
        var data = await _contentCache.Value; // May trigger recomputation if cache was invalidated
        return data.GetValueOrDefault(url);
    }
}
```

### Workflow

1. **Initial Load**: First content access triggers expensive processing operation
2. **File Change**: Developer modifies a markdown file
3. **Detection**: `ContentEngineFileWatcher` detects the change
4. **Invalidation**: Calls `NeedsRefresh()` which triggers `_contentCache.Refresh()`
5. **Debouncing**: If multiple changes occur rapidly, they're coalesced
6. **Reprocessing**: After debounce delay, content is reprocessed
7. **Cache Update**: New content replaces cached values
8. **UI Refresh**: Next page request gets updated content

## Performance Characteristics

### Memory Usage

- **Lazy Loading**: Content is only loaded when accessed
- **Single Instance**: Only one copy of processed content exists in memory
- **Efficient Updates**: Only changed content is reprocessed

### CPU Usage

- **Debouncing**: Prevents excessive recomputation during rapid changes
- **Incremental**: Only affected content is reprocessed
- **Background**: Refresh operations don't block UI threads

### I/O Optimization

- **Minimal File Access**: Files are read only when necessary
- **Efficient Watching**: File system watchers are lightweight
- **Batch Processing**: Multiple file changes are processed together

## Development Benefits

The hot reload architecture provides several advantages for developers:

### Immediate Feedback

Changes to markdown files, templates, or other content are reflected immediately in the browser without restarting the
application.

### Efficient Development Workflow

- **No Manual Restarts**: Content changes don't require application restarts
- **Rapid Iteration**: Quick edit-and-refresh cycles
- **Consistent State**: Application state is preserved across content updates

### Robust Error Handling

- **Graceful Degradation**: File system errors don't crash the application
- **Logging**: Comprehensive logging for debugging file watching issues
- **Recovery**: Automatic recovery from transient file system issues

## Configuration and Customization

### Debounce Timing

The debounce delay can be customized based on your development environment:

```csharp
// Custom debounce delay for slower file systems
var cache = new LazyAndForgetful<T>(factory, TimeSpan.FromMilliseconds(200));
```

### File Patterns

Customize which files trigger refreshes:

```csharp
// Watch specific file types
fileWatcher.AddPathWatch(contentPath, "*.md", OnMarkdownChanged);
fileWatcher.AddPathWatch(templatePath, "*.razor", OnTemplateChanged);

// Watch entire directories
fileWatcher.AddPathsWatch([contentPath, templatePath], OnAnyContentChanged);
```

### Logging

Enable detailed logging to troubleshoot file watching issues:

```csharp
// File watcher logs at Debug level for normal operations
// and Warning/Error levels for issues
services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
```

## Best Practices

### Content Organization

- **Logical Grouping**: Organize content in logical directory structures
- **Minimal Nesting**: Avoid deeply nested directory structures that might impact watching performance
- **Clear Naming**: Use descriptive file names that make debugging easier

### Development Workflow

- **Save Frequency**: The debouncing handles rapid saves gracefully
- **Batch Operations**: Large batch file operations are handled efficiently
- **IDE Integration**: Works seamlessly with popular IDEs and their file watching

### Performance Monitoring

- **Log Analysis**: Monitor file watcher logs for performance issues
- **Memory Usage**: Track memory usage during long development sessions
- **Response Times**: Monitor content refresh response times

The hot reload architecture in MyLittleContentEngine provides a robust, efficient, and developer-friendly foundation for
content management, enabling rapid iteration and a smooth development experience.