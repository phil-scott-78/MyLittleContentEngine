# Services/Generation

Static site generation services for rendering Blazor pages to HTML and discovering application routes.

## Files

### OutputGenerationService.cs
- **OutputGenerationService** - Service for generating static HTML pages from a Blazor application by fetching rendered content via HTTP with integrated link verification
- **ListExtensions** - Static extension methods for adding items with priority to immutable lists

### LinkVerificationService.cs
- **LinkVerificationService** - Service for verifying internal links in generated HTML during static site generation using AngleSharp for parsing

### RoutesHelper.cs
- **RoutesHelperService** - Service for discovering and processing routes in Blazor applications (both component routes and MapGet endpoints)
