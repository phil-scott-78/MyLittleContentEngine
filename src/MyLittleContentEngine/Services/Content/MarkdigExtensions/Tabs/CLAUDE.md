# Services/Content/MarkdigExtensions/Tabs

Tabbed code block support for displaying multiple code examples in an interactive tab interface.

## Files

### LanguageNormalizer.cs
- **LanguageNormalizer** - Static class for normalizing programming language identifiers to display names using a comprehensive language mapping dictionary

### TabbedCodeBlock.cs
- **TabbedCodeBlock** - Custom Markdig block container that holds multiple code blocks for tabbed display

### TabbedCodeBlockRenderer.cs
- **TabbedCodeBlockRenderer** - HTML renderer for tabbed code blocks that generates ARIA-compliant tab interface with unique group names

### TabbedCodeBlockRenderOptions.cs
- **TabbedCodeBlockRenderOptions** - Record type for customizing CSS classes used in the tabbed code block renderer

### TabbedCodeBlocksExtension.cs
- **TabbedCodeBlocksExtension** - Markdig extension that transforms consecutive code blocks marked with tabs=true into tabbed containers
