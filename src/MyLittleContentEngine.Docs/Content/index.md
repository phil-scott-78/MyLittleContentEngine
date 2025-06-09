---
title: "MyLittleContentEngine Documentation"
description: "A static site generator for .NET Blazor applications with markdown content management and strong typing"
order: 1
---

MyLittleContentEngine is a powerful static site generator designed specifically for .NET Blazor applications. It enables developers to create content-rich websites, blogs, and documentation sites using markdown content with strongly-typed front matter, advanced code highlighting, and seamless static HTML generation.

```mermaid
flowchart LR
    Start([Start])
    Decision{Decision Point}
    Process1[Process Step 1]
    Process2[Process Step 2]
    AltPath[Alternate Path]
    End([Stop])

    Start --> Decision
    Decision -- Yes --> Process1
    Decision -- No --> AltPath
    Process1 --> Process2
    Process2 --> End
    AltPath --> End
```

graph

```mermaid
  graph TD
      A[Start] --> B[Process]
      B --> C[End]
```      

more

```mermaid
sequenceDiagram

Alice->Bob: Hello Bob, how are you?
Bob->Alice: Fine, how did your mother like the book I suggested? And did you catch the new book about alien invasion?
Alice->Bob: Good.
Bob->Alice: Cool
```

git
```mermaid
---
title: Example Git diagram
---
gitGraph
   commit
   commit
   branch develop
   checkout develop
   commit
   commit
   checkout main
   merge develop
   commit
   commit
```

class diagram

```mermaid
---
title: Animal example
---
classDiagram
    note "From Duck till Zebra"
    Animal <|-- Duck
    note for Duck "can fly\ncan swim\ncan dive\ncan help in debugging"
    Animal <|-- Fish
    Animal <|-- Zebra
    Animal : +int age
    Animal : +String gender
    Animal: +isMammal()
    Animal: +mate()
    class Duck{
        +String beakColor
        +swim()
        +quack()
    }
    class Fish{
        -int sizeInFeet
        -canEat()
    }
    class Zebra{
        +bool is_wild
        +run()
    }
```

journey
```mermaid
journey
    title My working day
    section Go to work
      Make tea: 5: Me
      Go upstairs: 3: Me
      Do work: 1: Me, Cat
    section Go home
      Go downstairs: 5: Me
      Sit down: 5: Me
```

er diagram

```mermaid
---
title: Order example
---
erDiagram
    CUSTOMER ||--o{ ORDER : places
    ORDER ||--|{ LINE-ITEM : contains
    CUSTOMER }|..|{ DELIVERY-ADDRESS : uses
```


state diagram
```mermaid
---
title: Simple sample
---
stateDiagram-v2
    [*] --> Still
    Still --> [*]

    Still --> Moving
    Moving --> Still
    Moving --> Crash
    Crash --> [*]
```

## What Makes It Different

- **Strongly Typed**: Type-safe front matter and content models with full IntelliSense support
- **Blazor Native**: Built specifically for Blazor Server and WebAssembly applications
- **Developer Experience**: Hot reload during development with compile-time checking
- **Advanced Code Highlighting**: Roslyn integration for live syntax highlighting of connected .NET solutions
- **Performance Focused**: Efficient caching and static generation for fast loading times

## Core Features

### Content Management
- Markdown-based content with YAML front matter
- Hierarchical content organization
- Built-in tag system with automatic cross-references
- Draft support for content staging
- Automatic table of contents generation

### Static Site Generation
- Conditional build modes (development server vs. static generation)
- Automatic route discovery and HTML generation
- Asset copying and optimization
- SEO-ready with sitemap.xml and RSS feed generation

### Styling Integration
- MonorailCSS support for utility-first styling
- Automatic CSS class detection and generation
- Configurable color themes and palettes
- Performance-optimized CSS delivery

### Code Highlighting
- Roslyn-powered syntax highlighting for .NET code
- TextMate grammar support for other languages
- Tabbed code blocks for multi-language examples
- Custom highlighter support (GBNF, shell, etc.)

## Getting Started

The best way to learn MyLittleContentEngine is to follow our step-by-step tutorials:

1. **[Creating Your First Site](tutorials/creating-first-site.md)** - Set up a basic content site
2. **[Getting Started Guide](tutorials/getting-started.md)** - Learn the fundamentals
3. **[Adding Blog Posts](tutorials/adding-blog-posts.md)** - Create and manage blog content

## Documentation Structure

This documentation follows the [Diátaxis framework](https://diataxis.fr/) to provide you with the right information at the right time:

### 📚 [Tutorials](tutorials/index.md)
Step-by-step learning-oriented guides that take you through the process of creating real applications with MyLittleContentEngine.

### 🛠️ [How-To Guides](how-to/index.md) 
Problem-solving guides that show you how to accomplish specific tasks and overcome common challenges.

### 💡 [Explanation](explanation/index.md)
Understanding-oriented discussions that clarify concepts, design decisions, and the reasoning behind MyLittleContentEngine's architecture.

### 📖 [Reference](reference/index.md)
Information-oriented technical reference for APIs, configuration options, and detailed specifications.

## Quick Example

Here's a minimal setup to get you started:

```csharp
// Program.cs
builder.Services.AddContentEngineService(() => new ContentEngineOptions
{
    BlogTitle = "My Site",
    BlogDescription = "Powered by MyLittleContentEngine",
    BaseUrl = "https://mysite.com",
});

builder.Services.AddContentEngineStaticContentService<MyFrontMatter>();
```

```yaml
# Content/Blog/my-first-post.md
---
title: "Hello World"
description: "My first post"
date: 2025-01-01
tags:
  - introduction
---

Welcome to my new site powered by MyLittleContentEngine!
```

Ready to dive in? Start with [Creating Your First Site](tutorials/creating-first-site.md) or explore the specific area that interests you most.