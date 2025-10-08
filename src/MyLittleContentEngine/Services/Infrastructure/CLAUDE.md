# Services/Infrastructure

Infrastructure services including file watching, URL rewriting middleware, cross-reference resolution, HTTP client abstractions, and file system utilities.

## Files

### BaseUrlRewritingMiddleware.cs
- **BaseUrlRewritingMiddleware** - ASP.NET middleware that rewrites URLs in HTML responses to resolve xref cross-references and apply base URL prefixes

### BaseUrlRewritingMiddlewareExtensions.cs
- **BaseUrlRewritingMiddlewareExtensions** - Extension methods for adding BaseUrlRewritingMiddleware to the ASP.NET pipeline

### ContentEngineFileWatcher.cs
- **IContentEngineFileWatcher** - Interface for file watching with path-specific watches and change subscriptions
- **ContentEngineFileWatcher** - File system watcher that monitors directories for changes and supports hot-reloading in Blazor static sites

### FileSystemUtilities.cs
- **FileSystemUtilities** - Utilities for working with paths and URLs including file-to-URL conversion and directory enumeration

### FileWatchDependencyFactory.cs
- **FileWatchDependencyFactory&lt;T&gt;** - Dependency factory managing service instance lifetime with automatic cache invalidation on file changes

### ILocalHttpClient.cs
- **ILocalHttpClient** - Abstraction for HTTP client operations to enable testing without requiring a running server

### LocalHttpClient.cs
- **LocalHttpClient** - Production implementation of ILocalHttpClient wrapping HttpClient for making local HTTP requests

### XrefResolver.cs
- **IXrefResolver** - Interface for resolving cross-references (xref) by UID to URLs or CrossReference objects
- **XrefResolver** - Implementation of IXrefResolver that caches cross-references from all content services using AsyncLazy
