---
title: "Markdown Extensions"
description: "TODO: Generate"
order: 2040
---

MyLittleContentEngine uses [Markdig](https://github.com/xoofx/markdig) for Markdown processing, which is a powerful and
extensible Markdown parser for .NET. It supports a wide range of Markdown features and extensions, making it suitable
for various content needs.

MyLittleContentEngine supports several Markdown extensions to enhance your content. These extensions provide additional
formatting options and features that are not part of standard Markdig. These extensions merely generate HTML, you'll
still need to style them with CSS to match your site's design. However, `MyLittleContentEngine.MonorailCss` provides some
default values.

## Default Markdig Pipeline

The default [Markdig pipeline used by MyLittleContentEngine includes the following extensions:

```csharp
var builder = new MarkdownPipelineBuilder()
   .UseAutoIdentifiers(AutoIdentifierOptions.GitHub) // This sets up GitHub-style header IDs
   .UseAlertBlocks()
   .UseAbbreviations()
   .UseCitations()
   .UseCustomContainers()
   .UseDefinitionLists()
   .UseEmphasisExtras()
   .UseFigures()
   .UseFooters()
   .UseFootnotes()
   .UseGridTables()
   .UseMathematics()
   .UseMediaLinks()
   .UsePipeTables()
   .UseListExtras()
   .UseTaskLists()
   .UseAutoLinks()
   .UseGenericAttributes()
   .UseDiagrams()
   .UseCustomContainers()
   .UseYamlFrontMatter()
```

`UseAutoIdentifiers(AutoIdentifierOptions.GitHub)` is critical for generating IDs for headers, which is necessary for
linking to specific sections of the content within the sidebar navigation.

## Code Highlighting

MyLittleContentEngine will make an to highlight code blocks based on the language specified in the opening code block
server-side.

The following rules are followed:

1. If the [roslyn is connected](../getting-started/connecting-to-roslyn), and the code block is C# or VB.NET,
   then
   Roslyn's [Microsoft.CodeAnalysis.Classification.Classifier](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.classification.classifier.getclassifiedspans?view=roslyn-dotnet-3.0)
   will be used to highlight the code block. This ensures
   the the latest language features are highlighted correctly.
2. If the code block is bash or shell, a built-in highlighter will be used to highlight command and their options.
3. If the code block a language is supported by [TextMateSharp](https://github.com/danipen/TextMateSharp)'s Grammar
   package,
   then TextMateSharp will be used to highlight the code
   block. [Current Grammars](https://github.com/danipen/TextMateSharp/tree/master/src/TextMateSharp.Grammars/Resources/Grammars)
4. If those rules are not met, then the code block will be rendered with the language set on the code block. Within the
   `MyLittleContentEngine.UI`
   `scripts.js` file, a `highlightCode` function will be used to highlight the code block using
   the [Hightlight.JS](https://highlightjs.org/). This library will only be
   loaded if the code block is not highlighted by the previous rules. This ensures that the page does not load
   unnecessary JavaScript.

## Code Tabs

You can create tabbed content sections using the `tabs` attribute on a code block. This allows you to organize content
into multiple tabs.

Place the `tabs=true` attribute on the opening code block, and the subsequent code blocks will be treated as tab
content.

You can also specify titles for each tab by using the `title` attribute.

``````markdown
```html tabs=true
<p>My Content</p>
```

```xml title="My XML Data"
<data>My Data</data>
```
``````

This will render as:

```html tabs=true
<p>My Content</p>
```

```xml title="My XML Data"

<data>My Data</data>
```

Code blocks will be highlighted according to the rules mentioned above.

## Enhanced Alerts

The Markdig AlertBlock has been tweaked to play nicer with Monorail and Tailwind styling.

### Note

```markdown
> [!NOTE]  
> Highlights information that users should take into account, even when skimming.
```

> [!NOTE]  
> Highlights information that users should take into account, even when skimming.

### Tip

```markdown
> [!TIP]
> Optional information to help a user be more successful.
```

> [!TIP]
> Optional information to help a user be more successful.

### Important

```markdown
> [!IMPORTANT]  
> Crucial information necessary for users to succeed.
```

> [!IMPORTANT]  
> Crucial information necessary for users to succeed.

### Warning

```markdown
> [!WARNING]  
> Critical content demanding immediate user attention due to potential risks.
```

> [!WARNING]  
> Critical content demanding immediate user attention due to potential risks.

### Caution

```markdown
> [!CAUTION]
> Negative potential consequences of an action.
```

## Mermaid Diagrams

MyLittleContentEngine supports [Mermaid](https://mermaid.js.org/) diagrams. If you are using `MyLittleContentEngine.UI`, then
the `scripts.js` file will automatically load the Mermaid library and render the diagrams with your current theme.

```mermaid
sequenceDiagram
  participant Model
  participant KV_Cache

  Model->>Model: Process token 1
  Model->>KV_Cache: Store K1, V1
  Model->>Model: Process token 2
  Model->>KV_Cache: Store K2, V2
  LV->>KV_Cache: Retrieve K1–K2, V1–V2
  Model->>Model: Generate token 3 using cache

```

## Blazor within Markdown vs Mdazor

Simple blazor components can be used within Markdown content, using
the [Mdazor](https://github.com/phil-scott-78/Mdazor)

Mdazor is a custom Markdig extension that:

* Parses component tags - Recognizes <ComponentName prop="value">content</ComponentName> syntax in your Markdown
* Uses Blazor's HtmlRenderer - Actually renders real Blazor components server-side
* Handles nested content - Markdown inside components gets processed recursively

### Steps Component

It includes a builtin Steps component that can be used to create a step-by-step guide.

To register the component, you need to add the following to your `Program.cs`:

```csharp
builder.Services.AddMdazor()
    .AddMdazorComponent<Step>()
    .AddMdazorComponent<Steps>();
```

Then you can use the component in your Markdown content like this:

```````markdown

## Here is some content that needs steps.

To do the action, follow these steps:

<Steps>
<Step stepNumber="1">
**The First Thing to Do**

This is the first step in the process.

It can contain any Markdown content, including **bold text**, *italic text*, and even code blocks.

```csharp
var i = 2+2;
```

</Step>
<Step stepNumber="2">
**The Second Thing to Do**

This is the second step in the process. For these two steps, the code is using
bold tags, but you can use Headers instead, and the links will still appear
in the sidebar.

</Step>
</Steps>
```````

This will render as:

<Steps>
<Step stepNumber="1">
**The First Thing to Do**

This is the first step in the process.

It can contain any Markdown content, including **bold text**, *italic text*, and even code blocks.

```csharp
var i = 2+2;
```

</Step>
<Step stepNumber="2">
**The Second Thing to Do**

This is the second step in the process. For these two steps, the code is using
bold tags, but you can use Headers instead, and the links will still appear
in the sidebar.

</Step>
</Steps>