# Services/Content/MarkdigExtensions/CodeHighlighting

Advanced code syntax highlighting with support for multiple languages (Roslyn C#/VB, TextMate grammars, GBNF, Shell) and code transformations (line highlighting, diffs, word highlighting).

## Files

### CodeHighlightRenderer.cs
- **Test** - Test class containing a method demonstrating title case conversion
- **LanguageModifiers** - Constants class defining code block language modifiers (xmldocid, path, bodyonly)
- **LanguageIds** - Constants class defining language identifier strings for various programming languages
- **CodeHighlightRenderer** - HTML renderer for code blocks with syntax highlighting using a pipeline approach supporting C#/VB (Roslyn), GBNF, Shell, and TextMate grammars

### CodeHighlightRenderOptions.cs
- **CodeHighlightRenderOptions** - Record type for customizing CSS classes used in the code highlight renderer with default values

### CodeTransformer.cs
- **CodeTransformer** - Transforms highlighted HTML code blocks by processing directives (highlight, focus, diff, error, warning, word highlighting) and applying CSS classes
- **DirectiveMatch** - Record for matched directive information (full match, notation, index positions)
- **WordHighlightInfo** - Record for word highlighting information (word to highlight, optional message)
- **LineTransformation** - Internal class for tracking line-level transformations with notation and line number

### ColorCodingHighlighter.cs
- **ColorCodingHighlighter** - Markdig extension that replaces the default CodeBlockRenderer with CodeHighlightRenderer for syntax highlighting

### GbnfHighlighter.cs
- **GbnfHighlighter** - Static partial class for syntax highlighting GBNF (grammar) text with tokenization and HTML output
- **TokenType** - Enum defining GBNF token types (RuleName, Comment, StringLiteral, CharRange, Operator, Identifier, Whitespace)
- **Token** - Class representing a GBNF token with type and text

### ShellSyntaxHighlighter.cs
- **ShellSyntaxHighlighter** - Static partial class for syntax highlighting shell/bash scripts with support for commands, strings, flags, and comments

### TextMateHighlighter.cs
- **TextMateHighlighter** - Static class for syntax highlighting using TextMate grammars with scope-to-CSS class mapping for highlight.js compatibility
