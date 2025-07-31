# Roslyn Services Refactoring Plan

## Overview
Complete refactoring of Roslyn services to a clean, modular architecture without backwards compatibility constraints.

## Phase 1: Core Infrastructure

### 1. Create base CodeAnalysis folder structure and move existing files
- [ ] Create folder structure under `src/MyLittleContentEngine/Services/Content/CodeAnalysis/`
- [ ] Move existing files to appropriate locations
- [ ] Update namespaces

### 2. Implement ISolutionWorkspaceService for MSBuild workspace management
- [ ] Create `ISolutionWorkspaceService` interface
- [ ] Implement `SolutionWorkspaceService` with MSBuildWorkspace
- [ ] Add solution loading and project filtering
- [ ] Implement compilation caching
- [ ] Add invalidation support

### 3. Create ISymbolExtractionService for symbol discovery and caching
- [ ] Create `ISymbolExtractionService` interface
- [ ] Implement symbol extraction from Solution
- [ ] Add XML documentation ID lookup
- [ ] Implement code fragment extraction
- [ ] Add symbol caching with `CachedSymbolInfo` model

## Phase 2: Execution & Highlighting

### 4. Refactor AssemblyLoadingService with proper abstraction
- [ ] Create `IAssemblyLoadingService` interface
- [ ] Refactor existing assembly loading logic
- [ ] Implement `LoadedAssembly` record type
- [ ] Add assembly unloading support
- [ ] Migrate `RoslynAssemblyLoadContext`

### 5. Implement ICodeExecutionService with isolated execution context
- [ ] Create `ICodeExecutionService` interface
- [ ] Implement code execution with output capture
- [ ] Add method execution by XML doc ID
- [ ] Implement `ExecutionResult` record type
- [ ] Add timeout and security constraints

### 6. Create ISyntaxHighlightingService as main highlighting interface
- [ ] Create `ISyntaxHighlightingService` interface
- [ ] Implement syntax highlighting orchestration
- [ ] Add file content highlighting
- [ ] Add symbol highlighting
- [ ] Integrate with other services

## Phase 3: Configuration & Integration

### 7. Build unified RoslynOptions configuration system
- [ ] Create `CodeAnalysisOptions` record
- [ ] Add `ProjectFilter` configuration
- [ ] Add `HighlightingOptions`
- [ ] Add `CachingOptions`
- [ ] Add `ExecutionOptions`
- [ ] Implement options validation

### 8. Implement caching layer with invalidation support
- [ ] Create caching abstractions
- [ ] Implement symbol cache
- [ ] Implement compilation cache
- [ ] Add cache invalidation logic
- [ ] Integrate with file watching

### 9. Add file watching integration for hot reload
- [ ] Create file watching extensions
- [ ] Integrate with `IContentEngineFileWatcher`
- [ ] Implement debounced cache invalidation
- [ ] Add project file monitoring

## Phase 4: Migration & Testing

### 10. Update all consumers to use new service interfaces
- [ ] Update `ContentEngineOptions.MarkdownPipelineBuilder`
- [ ] Update `CodeHighlightRenderer`
- [ ] Update `ApiReferenceContentService`
- [ ] Update `CodeSnippet.razor` component
- [ ] Update example applications

### 11. Remove old Roslyn services implementation
- [ ] Remove old service files
- [ ] Remove old interfaces
- [ ] Clean up obsolete dependencies
- [ ] Update documentation

### 12. Write comprehensive tests for all new services
- [ ] Unit tests for `SolutionWorkspaceService`
- [ ] Unit tests for `SymbolExtractionService`
- [ ] Unit tests for `AssemblyLoadingService`
- [ ] Unit tests for `CodeExecutionService`
- [ ] Unit tests for `SyntaxHighlightingService`
- [ ] Integration tests for complete workflow
- [ ] Performance tests for caching

## Key Design Principles

1. **Single Responsibility** - Each service has one clear purpose
2. **Dependency Inversion** - All dependencies flow through interfaces
3. **Explicit Boundaries** - No service knows about another's internals
4. **Testability** - Each service can be unit tested in isolation
5. **Configuration** - Centralized, validated options

## Service Dependencies

```
ISyntaxHighlightingService
    → ISymbolExtractionService
        → ISolutionWorkspaceService
    → IAssemblyLoadingService
    → ICodeExecutionService
```

## Clean Code Patterns

- Use `Result<T>` pattern for error handling
- Strong typing for domain concepts (`XmlDocId`, `ProjectFilter`, etc.)
- Immutable configuration records
- Async-first API design
- Proper disposal patterns with `IDisposable`/`IAsyncDisposable`
