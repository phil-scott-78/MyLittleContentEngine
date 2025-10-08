# Services/Content/CodeAnalysis/SyntaxHighlighting

Roslyn-based syntax highlighting services for generating HTML with Highlight.js-compatible CSS classes.

## Files

### ISyntaxHighlightingService.cs
- **ISyntaxHighlightingService** - Main service interface for syntax highlighting operations supporting code, symbols, and files
- **HighlightedCode** - Record representing the result of syntax highlighting with HTML output, plain text, metadata, and success status

### SyntaxHighlighter.cs
- **Language** - Enum defining supported programming languages (CSharp, VisualBasic)
- **SyntaxHighlighter** - Internal disposable class using Roslyn's classification API to highlight code with Highlight.js-compatible CSS classes

### SyntaxHighlightingService.cs
- **SyntaxHighlightingService** - Main implementation of ISyntaxHighlightingService orchestrating syntax highlighting for code strings, symbols by XML doc ID, and file paths
