---
title: "MonorailCSS Configuration"
description: "Complete reference for configuring MonorailCSS options, color schemes, and styling in MyLittleContentEngine"
uid: "docs.reference.monorail-css-configuration"
order: 4020
---

[MonorailCSS](https://github.com/monorailcss/MonorailCss.Framework) provides a Tailwind-like CSS framework specifically designed for MyLittleContentEngine. This reference covers all configuration options, built-in styles, and customization capabilities.

## MonorailCssOptions

The `MonorailCssOptions` class provides the primary configuration interface for customizing the CSS framework:

```csharp
public class MonorailCssOptions
{
    public Func<int> PrimaryHue { get; init; } = () => 250;
    public Func<string> BaseColorName { get; init; } = () => ColorNames.Gray;
    public Func<int, (int, int, int)> ColorSchemeGenerator { get; init; } = 
        primary => (primary + 180, primary + 90, primary - 90);
    public Func<CssFrameworkSettings, CssFrameworkSettings> CustomCssFrameworkSettings { get; init; } = 
        settings => settings;
}
```

### Configuration Properties

#### PrimaryHue
- **Type**: `Func<int>`
- **Default**: `() => 250` (blue)
- **Range**: 0-360 (HSL hue degrees)
- **Purpose**: Sets the primary color for your site theme

**Example Usage:**
```csharp
builder.Services.AddMonorailCss(options => new MonorailCssOptions
{
    PrimaryHue = () => 200  // Cyan/teal theme
});
```

#### BaseColorName
- **Type**: `Func<string>`
- **Default**: `() => ColorNames.Gray`
- **Options**: `ColorNames.Gray`, `ColorNames.Slate`, `ColorNames.Zinc`, `ColorNames.Neutral`, `ColorNames.Stone`
- **Purpose**: Sets the neutral color palette used for text, backgrounds, and borders

**Example Usage:**
```csharp
builder.Services.AddMonorailCss(options => new MonorailCssOptions
{
    BaseColorName = () => ColorNames.Slate  // Cooler neutral tones
});
```

#### ColorSchemeGenerator
- **Type**: `Func<int, (int, int, int)>`
- **Default**: `primary => (primary + 180, primary + 90, primary - 90)`
- **Purpose**: Generates accent and tertiary colors based on primary hue
- **Returns**: Tuple of (accent, tertiary-one, tertiary-two) hue values

**Default Behavior:**
- **Accent**: Primary + 180° (complementary color)
- **Tertiary One**: Primary + 90° (for syntax highlighting)
- **Tertiary Two**: Primary - 90° (for syntax highlighting)

**Example Usage:**
```csharp
builder.Services.AddMonorailCss(options => new MonorailCssOptions
{
    ColorSchemeGenerator = primaryHue => (
        primaryHue + 120,  // Triadic accent
        primaryHue + 60,   // Analogous tertiary one
        primaryHue - 60    // Analogous tertiary two
    )
});
```

#### CustomCssFrameworkSettings
- **Type**: `Func<CssFrameworkSettings, CssFrameworkSettings>`
- **Default**: `settings => settings` (no modification)
- **Purpose**: Allows deep customization of the underlying CSS framework

**Example Usage:**
```csharp
builder.Services.AddMonorailCss(options => new MonorailCssOptions
{
    CustomCssFrameworkSettings = settings => settings with
    {
        // Add custom utility classes or modify existing ones
        Applies = settings.Applies.Add(".my-custom-class", "bg-primary-500 text-white p-4")
    }
});
```

## Built-in Component Styles

MonorailCSS includes pre-configured styles for common components. You can modify these using the `CustomCssFrameworkSettings` option.

```csharp
builder.Services.AddMonorailCss(_ =>
{
    return new MonorailCssOptions
    {
        CustomCssFrameworkSettings = defaultSettings => defaultSettings with
        {
            Applies = defaultSettings.Applies.SetItem(".hljs-deletion", "text-amber-700 dark:text-amber-300")
        }
    };
});
```

### Tab Components
```csharp:xmldocid,bodyonly
M:MyLittleContentEngine.MonorailCss.MonorailCssService.TabApplies
```

### Code Highlighting
```csharp:xmldocid,bodyonly
M:MyLittleContentEngine.MonorailCss.MonorailCssService.CodeBlockApplies
```

### Markdown Alert Blocks
```csharp:xmldocid,bodyonly
M:MyLittleContentEngine.MonorailCss.MonorailCssService.MarkdownAlertApplies
```

## Syntax Highlighting

MonorailCSS provides a complete syntax highlighting theme using the generated color palettes:

### Color Mapping
- **Comments**: Base colors with reduced opacity, italic
- **Keywords**: Primary color palette
- **Strings/Numbers**: Tertiary-one color palette
- **Functions**: Accent color palette
- **Variables**: Tertiary-two color palette
- **Operators**: Base colors

### Configuration
```csharp:xmldocid,bodyonly
M:MyLittleContentEngine.MonorailCss.MonorailCssService.HljsApplies
```

## DocSearch Integration

MonorailCSS includes pre-configured styles for DocSearch (Algolia search):

### Search Container
- **Backdrop**: Blur effect with base colors
- **Modal**: Consistent with site theme
- **Responsive**: Optimized for mobile and desktop

### Search Elements
- **Input**: Styled to match site theme
- **Results**: Hover states and selection highlighting
- **Icons**: Consistent sizing and colors

### DocSearch Element Overrides

```csharp:xmldocid,bodyonly
M:MyLittleContentEngine.MonorailCss.MonorailCssService.DocSearchApplies
```

### CSS Variable Overrides

These are currently hardcoded and can't be modified.

```csharp:xmldocid,bodyonly
M:MyLittleContentEngine.MonorailCss.MonorailCssService.GetDocsearchOverride
```

