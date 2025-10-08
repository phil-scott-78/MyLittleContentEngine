# Services/Content/MarkdigExtensions

Markdig pipeline extensions and utilities for parsing Markdown with YAML front matter, adding custom blocks, and extending Markdown capabilities.

## Files

### CodeBlockExtensions.cs
- **CodeBlockExtensions** - Extension methods for FencedCodeBlock that parse argument strings in key=value format into dictionaries

### MarkdownParserService.cs
- **MarkdownParserService** - Service for parsing and processing Markdown files with YAML front matter, providing caching, HTML conversion, and image path transformation

### MarkdownPipelineBuilderExtensions.cs
- **MarkdownPipelineBuilderExtensions** - Extension methods for adding custom syntax highlighting blocks, tabbed code blocks, and custom alert blocks to Markdig pipeline
