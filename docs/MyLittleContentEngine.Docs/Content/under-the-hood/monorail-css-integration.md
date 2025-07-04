---
title: "MonorailCSS Integration"
description: "Understanding the MonorailCSS integration and how to customize the design system, colors, and styling"
order: 3002
---

MyLittleContentEngine integrates with [MonorailCSS](https://github.com/DanielGaull/MonorailCSS) to provide a utility-first CSS framework with automatic class scanning and generation. This integration offers a complete design system with customizable colors, typography, and component styling.

## Overview

The MonorailCSS integration provides:

- **Automatic Class Scanning**: Only includes CSS classes that are actually used in your Razor components
- **Dynamic Color Generation**: Generates complete color palettes from a single hue value
- **Built-in Dark Mode**: Comprehensive dark mode support with automatic color inversions
- **Custom Component Styles**: Pre-configured styles for content engine components
- **Syntax Highlighting**: Integrated code highlighting with theme-aware colors

## Configuration

### Basic Setup

MonorailCSS is configured through the `MonorailCssOptions` class:

```csharp
builder.Services.AddMonorailCss(_ => new MonorailCssOptions
{
    PrimaryHue = () => 350,                           // Primary color hue (0-360)
    BaseColorName = () => ColorNames.Zinc,            // Base color palette
    ColorSchemeGenerator = primary => (               // Generate accent colors
        primary + 180,                                // Accent (complementary)
        primary + 90,                                 // Tertiary one
        primary - 90                                  // Tertiary two
    ),
    CustomCssFrameworkSettings = defaultSettings => defaultSettings with
    {
        // Override framework settings
        DesignSystem = defaultSettings.DesignSystem with
        {
            FontFamilies = defaultSettings.DesignSystem.FontFamilies
                .Add("display", new FontFamilyDefinition("Lexend, sans-serif"))
        }
    }
});
```

### Color System

The color system is based on **hue-driven generation** where you specify a primary hue, and the system generates complementary colors:

#### Primary Color
- **Hue Range**: 0-360 degrees on the color wheel
- **Palette Generation**: Automatically generates 50, 100, 200, 300, 400, 500, 600, 700, 800, 900 shades
- **Usage**: Primary actions, links, focus states

#### Generated Colors
Based on the primary hue, the system generates:

```csharp
// Example with primary hue of 350 (magenta/pink)
PrimaryHue = () => 350,

// Generated colors:
// - Accent: 350 + 180 = 530 (normalized to 170) - teal/cyan
// - Tertiary One: 350 + 90 = 440 (normalized to 80) - yellow/green  
// - Tertiary Two: 350 - 90 = 260 - blue/purple
```

### Base Color Palettes

Choose from MonorailCSS's built-in color palettes:

```csharp
BaseColorName = () => ColorNames.Gray,      // Neutral gray
BaseColorName = () => ColorNames.Slate,     // Cool gray
BaseColorName = () => ColorNames.Zinc,      // Modern gray
BaseColorName = () => ColorNames.Neutral,   // Warm gray
BaseColorName = () => ColorNames.Stone,     // Earthy gray
```

## Built-in Component Styles

### Code Blocks and Syntax Highlighting

The integration includes comprehensive syntax highlighting with theme-aware colors:

```css
/* Example generated styles */
.hljs { color: theme(colors.base.900); }
.hljs-keyword { color: theme(colors.primary.700); }
.hljs-string { color: theme(colors.tertiary-one.700); }
.hljs-function { color: theme(colors.accent.700); }

/* Dark mode variants */
.dark .hljs { color: theme(colors.base.200); }
.dark .hljs-keyword { color: theme(colors.primary.300); }
```

### Tabbed Code Blocks

Pre-configured styles for interactive code tabs:

```css
.tab-container {
  @apply flex flex-col bg-base-100 border border-base-300/75 
         shadow-xs rounded-xl overflow-x-auto 
         dark:bg-base-950/25 dark:border-base-700/50;
}

.tab-button {
  @apply whitespace-nowrap border-b border-transparent py-2 text-xs 
         text-base-900/90 font-medium transition-colors hover:text-accent-500
         aria-selected:text-accent-700 aria-selected:border-accent-700;
}
```

### Markdown Alerts

Styled alert blocks with color-coded variations:

```css
.markdown-alert-note    { /* Emerald theme */ }
.markdown-alert-tip     { /* Blue theme */ }
.markdown-alert-caution { /* Amber theme */ }
.markdown-alert-warning { /* Rose theme */ }
.markdown-alert-important { /* Sky theme */ }
```

### Prose Styling

Custom prose styles that override MonorailCSS defaults:

```css
/* Links without code elements get underlines */
a:not(:has(> code)) {
  border-bottom: 1px solid theme(colors.primary.500/75%);
}

/* Code blocks with subtle backgrounds */
pre {
  background-color: theme(colors.base.200/50%);
  border-radius: 0.4rem;
  box-shadow: inset 0 0 0 1px oklch(87.1% .006 286.286);
}
```

## CSS Variable System

MonorailCSS generates CSS variables for all colors, making them available throughout your application:

```css
:root {
  --monorail-color-primary-50: /* Generated from hue */;
  --monorail-color-primary-100: /* Generated */;
  /* ... all primary shades */
  
  --monorail-color-accent-50: /* Generated from hue + 180 */;
  /* ... all accent shades */
  
  --monorail-color-base-50: /* From selected base palette */;
  /* ... all base shades */
}
```

## Customization Examples

### Custom Font Families

```csharp
CustomCssFrameworkSettings = defaultSettings => defaultSettings with
{
    DesignSystem = defaultSettings.DesignSystem with
    {
        FontFamilies = defaultSettings.DesignSystem.FontFamilies
            .Add("display", new FontFamilyDefinition("Lexend, sans-serif"))
            .SetItem("mono", new FontFamilyDefinition("\"Cascadia Code\", monospace"))
    }
}
```

### Custom Color Scheme

```csharp
// Create a triadic color scheme
ColorSchemeGenerator = primary => (
    primary + 120,  // Triadic color 1
    primary + 240,  // Triadic color 2  
    primary + 60    // Split-complementary
)
```

### Adding Custom Component Styles

```csharp
CustomCssFrameworkSettings = defaultSettings => defaultSettings with
{
    Applies = defaultSettings.Applies.AddRange(new Dictionary<string, string>
    {
        { ".my-custom-card", "bg-base-100 p-4 rounded-lg shadow-sm border border-base-200" },
        { ".my-button", "bg-primary-600 text-white px-4 py-2 rounded hover:bg-primary-700" }
    })
}
```

## Dark Mode Support

Dark mode is automatically supported through CSS variables and the `dark:` prefix:

```css
/* Light mode */
.bg-base-100 { background-color: theme(colors.base.100); }

/* Dark mode */
.dark .bg-base-100 { background-color: theme(colors.base.900); }
```

### Prose Dark Mode

Special "invert" prose styles for dark mode:

```css
.prose-invert pre {
  background-color: theme(colors.base.800/75%);
  box-shadow: inset 0 0 0 1px oklab(100% 0 5.96046e-8/.1);
}
```

## Integration with DocSearch

The integration includes pre-configured DocSearch styling that automatically adapts to your color scheme:

```css
.DocSearch {
  --docsearch-primary-color: var(--monorail-color-primary-900);
  --docsearch-text-color: var(--monorail-color-base-800);
  --docsearch-modal-background: var(--monorail-color-base-100);
  /* ... many more variables */
}
```

## Performance Optimization

### Class Scanning

MonorailCSS only includes CSS for classes that are actually used:

```csharp
public class CssClassCollector
{
    // Scans all Razor files at build time
    // Only includes CSS for classes found in your components
    // Dramatically reduces final CSS bundle size
}
```

### Build-Time Generation

CSS is generated at build time, not runtime:

```csharp
// During build process
var cssClassValues = cssClassCollector.GetClasses();
var styleSheet = GetCssFramework().Process(cssClassValues);
```

## Usage in Components

### Utility Classes

```razor
<div class="bg-base-100 text-base-900 p-4 rounded-lg border border-base-300">
    <h2 class="text-primary-800 font-display text-xl">Card Title</h2>
    <p class="text-base-700">Card content with accent text.</p>
    <button class="bg-accent-600 text-white px-4 py-2 rounded hover:bg-accent-700">
        Action Button
    </button>
</div>
```

### Responsive Design

```razor
<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
    <div class="bg-base-100 p-4 rounded-lg">
        <h3 class="text-lg font-semibold text-primary-800">Item 1</h3>
    </div>
    <div class="bg-base-100 p-4 rounded-lg">
        <h3 class="text-lg font-semibold text-primary-800">Item 2</h3>
    </div>
</div>
```

## Best Practices

### Color Usage Guidelines

- **Primary**: Main actions, links, focus states
- **Accent**: Secondary actions, highlights, call-to-action elements
- **Base**: Text, backgrounds, borders, neutral elements
- **Tertiary**: Syntax highlighting, decorative elements

### Consistent Spacing

Use MonorailCSS's spacing scale consistently:

```razor
<!-- Good: Consistent spacing -->
<div class="p-4 mb-4 space-y-4">
    <div class="p-2 mb-2">Content</div>
</div>

<!-- Better: Use gap utilities -->
<div class="p-4 space-y-4">
    <div class="p-2">Content</div>
</div>
```

### Dark Mode Considerations

Always test both light and dark modes:

```razor
<!-- Ensure proper contrast in both modes -->
<div class="bg-base-100 text-base-900 dark:bg-base-900 dark:text-base-100">
    Content that works in both modes
</div>
```

## Troubleshooting

### Missing CSS Classes

If utility classes aren't appearing in your generated CSS:

1. **Check Class Scanning**: Ensure classes are in `.razor` files, not generated files
2. **Verify Syntax**: Use exact MonorailCSS syntax (e.g., `bg-primary-600`, not `bg-primary-600/50`)
3. **Restart Development**: Class scanning happens at build time

### Color Variations

If colors don't match expectations:

1. **Verify Hue Values**: Check that hue values are within 0-360 range
2. **Test Color Generation**: Use online color wheel tools to verify generated colors
3. **Check CSS Variables**: Inspect generated CSS variables in browser dev tools

### Performance Issues

For large applications:

1. **Optimize Class Usage**: Remove unused utility classes
2. **Use Semantic Classes**: Consider custom component classes for repeated patterns
3. **Monitor Bundle Size**: Check generated CSS file size

The MonorailCSS integration provides a powerful, flexible, and performant styling solution that grows with your application while maintaining consistency and developer productivity.