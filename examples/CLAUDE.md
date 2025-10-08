# Example Projects

This directory contains example projects demonstrating different features and use cases of MyLittleContentEngine.

## MinimalExample
The simplest possible setup with markdown content and MonorailCSS styling.

**What makes it unique:**
- Bare-bones implementation showing minimal configuration required
- Single markdown content service
- Basic MonorailCSS integration
- Good starting point for new projects

**Dependencies:** None (uses only core packages)

---

## BlogExample
Full-featured blog site using the `BlogSite` helper with RSS feed and sitemap generation.

**What makes it unique:**
- Uses the high-level `BlogSite` helper for rapid setup
- Pre-configured blog features: RSS feed, sitemap, tags, social links
- Custom fonts (Google Fonts: Inter, Noto Sans Display)
- Hero content and project showcase
- Social media image generation
- Custom color theming (hue: 300, Zinc palette)

**Dependencies:**
- `Microsoft.Playwright` - Browser automation for screenshot/image generation
- `Microsoft.CodeAnalysis.Workspaces.MSBuild` - Code analysis capabilities

---

## ApiReferenceExample
Auto-generated API documentation from Roslyn code analysis.

**What makes it unique:**
- Demonstrates Roslyn integration with `.WithConnectedRoslynSolution()`
- Automatically generates API reference documentation from C# source code
- Namespace filtering (include/exclude patterns)
- Combined markdown content and API reference in one site

**Dependencies:**
- Roslyn code analysis via `.WithApiReferenceContentService()`
- Requires solution path configuration

---

## RecipeExample
CookLang recipe parser with responsive image processing.

**What makes it unique:**
- Custom recipe content service using CookLang format (`.cook` files)
- Responsive image processing with dynamic size generation
- Custom image endpoint (`/images/{filename}-{size}.webp`)
- Custom fonts (Montserrat Alternates, Inter)
- Demonstrates extending the engine with domain-specific content types

**Dependencies:**
- `CooklangSharp` - CookLang recipe format parser
- `SixLabors.ImageSharp` - Image processing and manipulation
- `Fractions` - Recipe measurement handling
- `YamlDotNet` - YAML frontmatter parsing

---

## SearchExample
Custom `IContentService` implementation generating random content for search demonstrations.

**What makes it unique:**
- Implements custom `IContentService` from scratch
- Generates 1000 random pages using Bogus fake data
- Uses `DocSite` helper with custom content service
- Demonstrates how to create entirely custom content sources
- No markdown files - all content generated programmatically

**Dependencies:**
- `Bogus` - Fake data generation library

---

## UserInterfaceExample
Showcase of available UI components and styling options.

**What makes it unique:**
- Focuses on demonstrating UI component library
- Simple content structure to highlight components
- Uses `DocsFrontMatter` for documentation-style pages

**Dependencies:** None (uses only core packages)

---

## MultipleContentSourceExample
Multiple markdown content services with different base URLs and folder structures.

**What makes it unique:**
- Three separate markdown services in one site:
  - Root content (`/`) with `ExcludeSubfolders = true`
  - Blog content (`/blog`)
  - Documentation content (`/docs`)
- Demonstrates multi-section site organization
- Different frontmatter types per section

**Dependencies:** None (uses only core packages)

---

## RoslynIntegrationExample
Roslyn integration for code documentation without API reference generation.

**What makes it unique:**
- Shows Roslyn integration via `.WithConnectedRoslynSolution()`
- Enables code analysis features in markdown documentation
- Watches source files for hot reload (`<Watch Include="..\..\src\**\*.cs" />`)
- Combined markdown + code analysis without full API reference

**Dependencies:**
- Roslyn code analysis via `.WithConnectedRoslynSolution()`

---

## Spectre.Console
Multi-section documentation site for Spectre.Console library with advanced theming.

**What makes it unique:**
- Three distinct content sections with different frontmatter:
  - Console documentation (`/console`)
  - CLI documentation (`/cli`)
  - Blog posts (`/blog`)
- Table of contents sections (`TableOfContentsSectionKey`)
- Custom color scheme generator
- Roslyn integration for code documentation
- Advanced MonorailCSS customization (hue: 315, custom color scheme)

**Dependencies:**
- Roslyn code analysis
- Multiple markdown content services

---

## Spectre.Console.Examples
Console application runner for executing Spectre.Console code examples.

---

## SingleFileApp
Single-file C# application using top-level statements and special package directives.

**What makes it unique:**
- Entire app in one `app.cs` file
- Uses `#:sdk` and `#:package` directives for dependencies
- Live reload integration for content changes
- `DocSite` helper in minimal format
- Demonstrates the simplest deployment scenario

**Dependencies:**
- `Westwind.AspNetCore.LiveReload` - Hot reload for content files

**Note:** Uses special single-file syntax requiring .NET script support
