---
title: "Deploying to Subdirectories"
description: "Deploy your MyLittleContentEngine site to subdirectories with BaseUrl rewriting middleware"
uid: "docs.guides.deploying-to-subdirectories"
order: 2200
---

When deploying your MyLittleContentEngine site to hosting services like GitHub Pages, Azure Static Web Apps, or any other host where your site runs in a subdirectory (e.g., `https://mysite.com/my-app/`), you need to configure BaseUrl rewriting to ensure all links work correctly.

MyLittleContentEngine includes the `BaseUrlRewritingMiddleware` that automatically handles this by rewriting root-relative URLs in your HTML responses to include the configured base path.

> [!NOTE]
> For comprehensive information about link types, BaseUrl concepts, and testing strategies, see <xref:docs.guides.linking-documents-and-media>.

## How BaseUrl Rewriting Works

The BaseUrlRewritingMiddleware performs two main functions:

1. **Cross-Reference Resolution**: Resolves `xref:` links to their actual targets
2. **BaseUrl Rewriting**: Rewrites root-relative URLs (starting with `/`) to include your configured BaseUrl

### URL Rewriting Process

The middleware processes HTML responses and rewrites URLs in the following elements:

- **Links**: `<a href="/page">` and `<link href="/styles.css">`
- **Images**: `<img src="/image.jpg">`
- **Scripts**: `<script src="/script.js">`
- **Media**: `<iframe>`, `<embed>`, `<source>`, `<track>` tags
- **Data attributes**: Any `data-*` attributes containing URLs
- **CSS**: `url()` functions and `@import` statements in style attributes

### Example Transformation

With `BaseUrl = "/my-app"`, the middleware transforms:

```html
<!-- Before -->
<a href="/docs/getting-started">Getting Started</a>
<img src="/images/logo.png" alt="Logo">
<script src="/scripts/app.js"></script>

<!-- After -->
<a href="/my-app/docs/getting-started">Getting Started</a>
<img src="/my-app/images/logo.png" alt="Logo">
<script src="/my-app/scripts/app.js"></script>
```

## Configuration

<Steps>
<Step stepNumber="1">
## Set BaseUrl in ContentEngineOptions

Configure your BaseUrl in the `ContentEngineOptions`:

```csharp
builder.Services.AddContentEngineService(_ => new ContentEngineOptions
{
    SiteTitle = "My Site",
    SiteDescription = "My awesome site",
    BaseUrl = "/my-app", // Your subdirectory path
    ContentRootPath = "Content",
});
```

### BaseUrl Guidelines

- **Include leading slash**: Use `/my-app`, not `my-app`
- **No trailing slash**: Use `/my-app`, not `/my-app/`
- **Root deployment**: Use `/` for root directory deployment
- **Environment variables**: Use environment variables for different deployment targets

```csharp
BaseUrl = Environment.GetEnvironmentVariable("BaseUrl") ?? "/",
```
</Step>

<Step stepNumber="2">
## Configure Static Generation

For static site generation, ensure your build process uses the correct BaseUrl:

```csharp
// Use environment variable for different deployment targets
builder.Services.AddContentEngineService(_ => new ContentEngineOptions
{
    SiteTitle = "My Site",
    SiteDescription = "My awesome site",
    BaseUrl = Environment.GetEnvironmentVariable("BaseUrl") ?? "/",
    ContentRootPath = "Content",
});
```

### GitHub Actions Example

```yaml
name: Build and publish to GitHub Pages

on:
   push:
      branches: [ "*" ]
   pull_request:
      branches: [ "main" ]

env:
   ASPNETCORE_ENVIRONMENT: Production
   WEBAPP_PATH: ./src/MyContentSite/
   WEBAPP_CSPROJ: MyContentSite.csproj

permissions:
   contents: read
   pages: write
   id-token: write

# Allow only one concurrent deployment
concurrency:
   group: "pages"
   cancel-in-progress: false

jobs:
   build:
      runs-on: ubuntu-latest

      steps:
         - uses: actions/checkout@v4

         - name: Install .NET
           uses: actions/setup-dotnet@v4
           with:
              global-json-file: global.json

         - name: Build the Project
           run: |
              dotnet build

         - name: Run webapp and generate static files
           run: |
              set BaseHref="/"
              dotnet run --project ${{ env.WEBAPP_PATH }}${{env.WEBAPP_CSPROJ}} --configuration Release -- build

         - name: Setup Pages
           uses: actions/configure-pages@v4

         - name: Upload artifact
           uses: actions/upload-pages-artifact@v3
           with:
              path: ${{ env.WEBAPP_PATH }}output

   deploy:
      environment:
         name: github-pages
         url: ${{ steps.deployment.outputs.page_url }}
      runs-on: ubuntu-latest
      needs: build
      if: (github.event_name == 'push' && github.ref == 'refs/heads/main') || (github.event_name == 'pull_request' && github.event.action == 'closed' && github.event.pull_request.merged == true)
      steps:
         - name: Deploy to GitHub Pages
           id: deployment
           uses: actions/deploy-pages@v4

```
</Step>
</Steps>

## Cross-Reference Resolution

The BaseUrlRewritingMiddleware also handles cross-reference (`xref:`) resolution, converting references like:

```markdown
See the [ContentService documentation](xref:MyLittleContentEngine.ContentService)
```

Into actual links:

```html
<a href="/my-app/api/MyLittleContentEngine.ContentService">ContentService documentation</a>
```

### Unresolved Cross-References

When a cross-reference cannot be resolved, the middleware:

1. **Preserves the link text** for user experience
2. **Adds error styling** with red color and strikethrough
3. **Includes debug attributes** for troubleshooting:
   - `data-xref-error="Reference not found"`
   - `data-xref-uid="the-unresolved-uid"`

```html
<!-- Unresolved xref becomes: -->
<span data-xref-error="Reference not found" 
      data-xref-uid="Unknown.Type" 
      style="color: red; text-decoration: line-through;">
  Unknown Type
</span>
```

## Best Practices

1. **Use environment variables** for BaseUrl to support multiple deployment targets
2. **Use root-relative URLs** in your content (`/page` not `page`)
3. **Test locally** with different BaseUrl values to ensure functionality
4. **Monitor for unresolved xrefs** using browser developer tools to check for error attributes
5. **Use LinkService** in Blazor components for consistent URL generation

> [!TIP]
> For guidance on choosing the right link types and linking best practices, see the [Linking Documents and Media](xref:docs.guides.linking-documents-and-media) guide.

The BaseUrlRewritingMiddleware makes subdirectory deployment seamless while maintaining the flexibility to deploy to different environments without code changes.