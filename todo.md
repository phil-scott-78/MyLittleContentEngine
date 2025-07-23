# MyLittleContentEngine Documentation Update Todo

This document outlines necessary updates to bring the documentation in sync with the current state of the codebase. The analysis was completed on 2025-01-23.

## Critical Issues That Need Immediate Attention

### 1. **MAJOR**: Getting Started Tutorial References Non-Existent Examples
- **File**: `docs/getting-started/creating-first-site.md`
- **Issue**: References `examples/MinimalExample/` components and Program.cs, but these don't match current example structure
- **Current State**: MinimalExample exists but has different structure than documented
- **Action**: Completely rewrite tutorial to match actual MinimalExample or create new example that matches tutorial

### 2. **MAJOR**: BlogSite Configuration API Changes
- **File**: `docs/guides/using-blogsite.md`
- **Issue**: Documents old `BlogSiteOptions` properties that may not exist or have changed
- **Current State**: BlogExample shows `SocialMediaImageUrlFactory` and other properties not in docs
- **Action**: Audit all BlogSiteOptions properties and update documentation

### 3. **MAJOR**: RecipeExample Completely Undocumented
- **Issue**: RecipeExample with CookLang integration exists but has zero documentation
- **Current State**: Full-featured example with responsive images, CookLang parsing, custom content services
- **Action**: Create comprehensive guide for CookLang/Recipe functionality

## New Features Requiring Documentation

### 4. **NEW FEATURE**: Custom Content Services Framework
- **Issue**: Multiple examples show custom content services but no documentation exists
- **Examples**: 
  - `RecipeExample/RecipeContentService.cs` 
  - `SearchExample/RandomContentService.cs`
- **Action**: Create guide "Creating Custom Content Services"

### 5. **NEW FEATURE**: Responsive Image Processing
- **Issue**: `ResponsiveImageContentService` exists with no documentation
- **Current State**: Full implementation in RecipeExample with image resizing/WebP conversion
- **Action**: Create guide "Implementing Responsive Images"

### 6. **NEW FEATURE**: Single File Applications
- **Issue**: `examples/SingleFileApp/app.cs` shows minimal single-file setup with no documentation
- **Action**: Create tutorial "Single File Applications"

### 7. **NEW FEATURE**: Multiple Content Source Architecture
- **Issue**: `MultipleContentSourceExample` shows advanced multi-service setup with no documentation
- **Current State**: Shows blog, docs, and pages with different front matter types
- **Action**: Create guide "Managing Multiple Content Sources"

### 8. **NEW FEATURE**: Razor Pages with Metadata
- **Issue**: `razor-pages-with-metadata.md` exists but needs major updates
- **Current State**: Examples show `.razor.metadata.yml` files but documentation is incomplete
- **Action**: Update with current implementation details

## Missing API Documentation

### 9. **MISSING**: Search Configuration Options
- **Issue**: Search guide exists but lacks configuration details
- **Missing**: 
  - SearchIndexService customization
  - Search priority configuration
  - FlexSearch options
- **Action**: Expand search documentation with full API reference

### 10. **MISSING**: File Watching and Hot Reload
- **Issue**: `hot-reload-architecture.md` exists but may be outdated
- **Current State**: `ContentEngineFileWatcher` shows sophisticated file watching
- **Action**: Update with current architecture and troubleshooting

### 11. **MISSING**: MonorailCSS Configuration
- **Issue**: `monorail-css-configuration.md` is minimal
- **Current State**: Examples show extensive MonorailCSS customization
- **Action**: Create comprehensive MonorailCSS guide

## Infrastructure Features Needing Documentation

### 12. **MISSING**: Base URL Rewriting Middleware
- **Issue**: `BaseUrlRewritingMiddleware` exists with no documentation
- **Use Case**: Subdirectory deployments, GitHub Pages
- **Action**: Create guide "Deploying to Subdirectories"

### 13. **MISSING**: Word Breaking Middleware
- **Issue**: `WordBreakingMiddleware` exists with no documentation
- **Action**: Document word breaking functionality

### 14. **MISSING**: XRef Resolver System
- **Issue**: `XrefResolver` exists but documentation is minimal
- **Current State**: Sophisticated cross-reference system
- **Action**: Expand xref documentation

## Code Highlighting & Extensions Updates

### 15. **UPDATE**: Markdown Extensions Documentation
- **Issue**: `markdown-extensions.md` is extensive but may be outdated
- **Current State**: Multiple new highlighters (GBNF, ColorCoding, etc.)
- **Action**: Audit all extensions and update examples

### 16. **MISSING**: Roslyn Integration Deep Dive
- **Issue**: `connecting-to-roslyn.md` exists but lacks advanced configuration
- **Current State**: Multiple Roslyn services (`CodeExecutionService`, `RoslynExampleCoordinator`)
- **Action**: Create advanced Roslyn integration guide

## UI Components Updates

### 17. **UPDATE**: UI Components Documentation
- **Issue**: `using-ui-elements.md` covers basic components but not all
- **Missing**: `BigTable`, updated `Badge` component
- **Action**: Document all UI components with examples

### 18. **MISSING**: JavaScript Architecture
- **Issue**: `javascript-architecture.md` exists but may be outdated
- **Current State**: `scripts.js` has sophisticated functionality
- **Action**: Update JavaScript documentation

## Reference Documentation Gaps

### 19. **UPDATE**: Front Matter Properties
- **Issue**: `front-matter-properties.md` is good but may be incomplete
- **Action**: Audit all front matter implementations and ensure completeness

### 20. **MISSING**: Complete API Reference
- **Issue**: API documentation generation exists but self-documentation is minimal
- **Action**: Generate comprehensive API docs for MyLittleContentEngine itself

## Example Inconsistencies

### 21. **CONSISTENCY**: Update All Example References
- **Issue**: Documentation references example files that may not exist or be different
- **Files Affected**: Most getting-started and guides documentation
- **Action**: Systematic review of all `path` and file references

### 22. **MISSING**: Example Index/Overview
- **Issue**: No documentation explaining what each example demonstrates
- **Action**: Create examples overview guide

## Deployment & Operations

### 23. **UPDATE**: GitHub Pages Deployment
- **Issue**: `deploying-to-github-pages.md` exists but may be outdated
- **Action**: Test and update deployment documentation

### 24. **MISSING**: Static Site Generation Deep Dive
- **Issue**: `content-processing-pipeline.mdx` is good but missing troubleshooting
- **Action**: Add troubleshooting section for static generation

### 25. **MISSING**: Performance Optimization Guide
- **Issue**: No documentation on optimizing build times, bundle sizes
- **Action**: Create performance guide

## Content Organization Issues

### 26. **RESTRUCTURE**: Documentation Site Navigation
- **Issue**: Some guides may be in wrong categories
- **Action**: Review Diátaxis framework alignment

### 27. **MISSING**: Migration Guides
- **Issue**: No migration documentation for version updates
- **Action**: Create migration guides for breaking changes

## Testing & Development

### 28. **MISSING**: Testing Content Applications
- **Issue**: No documentation on testing content engines
- **Current State**: `CLAUDE.md` shows test helpers but not documented publicly
- **Action**: Create testing guide

### 29. **MISSING**: Development Workflow Guide
- **Issue**: No documentation on contributing, building, debugging
- **Action**: Create contributor documentation

## Quick Wins (Easy Updates)

### 30. **QUICK**: Fix Broken Cross-References
- **Action**: Scan all `xref:` links and fix broken ones

### 31. **QUICK**: Update Package Installation Commands
- **Issue**: Some docs may reference old package names or missing `--prerelease`
- **Action**: Standardize installation commands

### 32. **QUICK**: Add Missing Code Language Tags
- **Issue**: Some code blocks lack language specification
- **Action**: Add appropriate language tags for syntax highlighting

## New Documentation Topics to Create

### 33. **NEW**: "CookLang Recipe Integration"
- Based on RecipeExample functionality

### 34. **NEW**: "Building Documentation Sites"
- Expand DocSite documentation

### 35. **NEW**: "Advanced Blazor Integration"
- Server-side rendering, component architecture

### 36. **NEW**: "Content Security and Validation"
- Front matter validation, content sanitization

### 37. **NEW**: "Troubleshooting Common Issues"
- Based on common GitHub issues and problems

### 38. **NEW**: "Extending MyLittleContentEngine"
- Plugin architecture, custom services

## Priority Classification

### Immediate (Blocking Users)
- Items 1, 2, 21 (Fix example references and tutorials)

### High Priority (Missing Core Features)
- Items 3, 4, 5, 6, 7 (Document major undocumented features)

### Medium Priority (Improve User Experience)
- Items 8-20 (API documentation and infrastructure)

### Low Priority (Nice to Have)
- Items 22-38 (Additional guides and improvements)

## Methodology Notes

This analysis was conducted by:
1. Reading all documentation in `docs/MyLittleContentEngine.Docs/Content/`
2. Examining all examples in `examples/`
3. Reviewing source code structure in `src/`
4. Comparing documentation claims with actual implementations
5. Identifying gaps where functionality exists but documentation doesn't

The codebase shows significant evolution beyond what's currently documented, particularly in areas of custom content services, responsive images, and advanced deployment scenarios.