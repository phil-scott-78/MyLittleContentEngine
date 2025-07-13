---
title: "Razor Pages with Metadata"
description: "Learn how to add metadata to Razor pages using sidecar .yml files for enhanced static site generation with custom titles, descriptions, and ordering."
order: 2060
---

MyLittleContentEngine automatically discovers and generates static pages from Razor components in your application. You can enhance these pages with metadata by creating sidecar `.yml` files that provide additional information like titles, descriptions, and ordering for use in sitemaps, RSS feeds, and navigation.

## How It Works

The `RazorPageContentService` automatically scans all assemblies in your application for Razor components that have `@page` directives without parameters. For each component found, it searches for an optional metadata file using the naming convention:

```
ComponentName.razor.metadata.yml
```

For example, if you have an `Index.razor` component, the service will look for `Index.razor.metadata.yml`.

## Metadata File Discovery

The service requires metadata files to be located **in the same directory** as their corresponding Razor component. The discovery process:

1. **Finds the Razor component**: Searches common directories like `Components/Pages`, `Components`, `Pages`, `Views`, `Areas`, `src/Components/Pages`, etc.
2. **Looks for metadata side-by-side**: Once the `.razor` file is found, looks for the `.metadata.yml` file in the exact same directory
3. **Enforces co-location**: Metadata files in different directories are ignored

This approach ensures that components and their metadata are always kept together, making them easier to maintain and organize.

## Creating a Razor Page with Metadata

### Step 1: Create Your Razor Component

Create a standard Razor page component:

```razor
@page "/about"
@page "/about-us"

<PageTitle>About Us</PageTitle>

<h1>About Our Company</h1>

<p>Welcome to our company page...</p>
```

### Step 2: Create the Metadata File

Create a sidecar metadata file named `About.razor.metadata.yml` (matching your component's class name):

```yaml
title: "About Our Company"
description: "Learn more about our company history, mission, and values"
lastMod: "2024-01-15T10:30:00Z"
order: 10
rssItem: true
```

### Step 3: Place Files Side-by-Side

The metadata file **must** be in the same directory as the Razor component:

```
Components/Pages/
├── About.razor
└── About.razor.metadata.yml
```

Other examples of valid side-by-side organization:

```
Pages/
├── Index.razor
├── Index.razor.metadata.yml
├── Services.razor
└── Services.razor.metadata.yml
```

```
src/Components/Pages/
├── Contact.razor
└── Contact.razor.metadata.yml
```

## Metadata Properties

The metadata file supports all properties from the `Metadata` class:

### title
The page title used in RSS feeds and navigation.

```yaml
title: "About Our Company"
```

### description
A brief description of the page content, used in RSS feeds and SEO metadata.

```yaml
description: "Learn about our company history, mission, and values"
```

### lastMod
The last modification date in ISO 8601 format. Used in sitemaps for SEO.

```yaml
lastMod: "2024-01-15T10:30:00Z"
```

### order
Controls the order of pages in navigation and table of contents. Lower numbers appear first.

```yaml
order: 10
```

Default value is `int.MaxValue` (no specific ordering).

### rssItem
Whether this page should be included in RSS feeds.

```yaml
rssItem: true  # Include in RSS (default)
rssItem: false # Exclude from RSS
```

## Complete Example

Here's a complete example with both the Razor component and its metadata:

**Components/Pages/Services.razor**
```razor
@page "/services"
@page "/our-services"

<PageTitle>Our Services</PageTitle>

<h1>Professional Services</h1>

<div class="services-grid">
    <div class="service-card">
        <h2>Web Development</h2>
        <p>Custom web applications built with modern technologies.</p>
    </div>
    
    <div class="service-card">
        <h2>Consulting</h2>
        <p>Expert guidance for your digital transformation.</p>
    </div>
</div>
```

**Services.razor.metadata.yml**
```yaml
title: "Professional Services"
description: "Comprehensive web development and consulting services for modern businesses"
lastMod: "2024-01-20T14:15:00Z"
order: 20
rssItem: true
```

## Static Generation Integration

When MyLittleContentEngine generates your static site:

1. **Page Discovery**: Finds all Razor components with `@page` directives
2. **Metadata Loading**: Searches for and loads corresponding metadata files
3. **Static Generation**: Generates HTML files with enhanced metadata
4. **Sitemap Generation**: Includes `lastMod` dates in `sitemap.xml`
5. **RSS Feed**: Includes pages with `rssItem: true` in RSS feeds
6. **Navigation**: Uses `order` property for consistent page ordering

## Multi-Assembly Support

The service automatically discovers Razor pages in:

- Your main application assembly
- Referenced NuGet packages with Razor components
- Razor Class Libraries (RCLs)
- Any loaded assembly containing Blazor components

This means metadata files work even for components from external packages, as long as you can place the metadata file in your project structure.

## Best Practices

### Naming Convention
Always match the metadata file name exactly to your component's class name:
- Component: `ProductCatalog.razor` → Metadata: `ProductCatalog.razor.metadata.yml`
- Component: `Index.razor` → Metadata: `Index.razor.metadata.yml`

### Organization
Keep metadata files side-by-side with their components:

**Always co-locate**: Place `.razor.metadata.yml` files in the same directory as their corresponding `.razor` files
**Consistent structure**: Organize component directories logically, and metadata will naturally follow
**Team clarity**: The side-by-side requirement makes it clear which metadata belongs to which component

### Maintenance
- Update `lastMod` when you significantly change page content
- Use meaningful `order` values (10, 20, 30) to allow easy insertion of new pages
- Keep descriptions concise but descriptive for SEO and RSS readers

### Performance
- Metadata loading is cached during static generation
- Failed metadata parsing doesn't break page generation
- Missing metadata files don't cause errors

## Troubleshooting

### Metadata Not Loading
1. Verify the file name exactly matches your component class name
2. Check YAML syntax using an online YAML validator
3. Ensure the file is included in your project and deployed
4. Check the build output for any parsing warnings

### Common YAML Syntax Issues
```yaml
# ✅ Correct
title: "My Page Title"
description: "A description of the page"

# ❌ Incorrect (missing quotes with special characters)
title: About Us: Our Story
description: We're the best!

# ✅ Correct (quotes protect special characters)
title: "About Us: Our Story"
description: "We're the best!"
```

### Build Warnings
Enable detailed logging to see metadata loading information:

```json
{
  "Logging": {
    "LogLevel": {
      "MyLittleContentEngine.Services.Content.RazorPageContentService": "Debug"
    }
  }
}
```

This feature provides a powerful way to enhance your Razor pages with rich metadata while maintaining the simplicity and flexibility of standard Blazor development.