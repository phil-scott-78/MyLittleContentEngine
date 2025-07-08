---
title: "Table of Contents Generation"
description: "How MyLittleContentEngine builds hierarchical navigation from your content structure"
order: 3020
---

The Table of Contents (TOC) system in MyLittleContentEngine automatically creates hierarchical navigation menus from
your content pages. It analyzes your page URLs and front matter to build organized, nested navigation structures that
reflect your site's content organization.

To retrieve the TOC entries, you can use the `GetNavigationTocAsync()` method from the `ITableOfContentService` interface.



## How TOC Generation Works

The TOC generation process transforms your flat collection of content files into a structured navigation tree. It
handles complex scenarios like folder structures, index pages, and custom ordering to create intuitive navigation.

<Steps>
<Step stepNumber="1">
## Collecting Pages and Metadata

The system starts by gathering all pages from your content sources and extracting the information needed for navigation:

### Page Discovery

Pages come from multiple sources:

- **Markdown files** with front matter in your content directories
- **API documentation** automatically generated from your code
- **Custom content sources** you've configured

Each `IContentService` must implement `GetContentTocEntriesAsync()` to provide the necessary metadata for TOC
generation.

### Required Information

For each page, the system needs four pieces of information:

- **Title**: The display name for navigation (from the `title` property in front matter)
- **URL**: The page's web address 
- **Order**: A number for sorting (from the `order` property, defaults to a high number if not specified)
- **Hierarchy Parts**: An array of strings that defines the navigation structure

Pages without titles are automatically excluded from navigation menus.

</Step>
<Step stepNumber="2">

## Building the Hierarchy

The system organizes pages into a tree structure based on hierarchy parts provided by each content service.

### Hierarchy Parts Organization

Each content service provides hierarchy parts that determine the page's position in the navigation. For example:

- `["getting-started", "installation"]` becomes a child of the "Getting Started" section
- `["api", "classes", "ContentService"]` creates nested folders: API → Classes → ContentService

Content services have full control over their hierarchy structure and can customize it. 
For example, the `MarkdownContentService` uses the folder structure to generate the hierarchy parts. 
A service providing a product list might use product categories as hierarchy parts.

### Folder Structure Creation

The system automatically creates folder-like navigation entries for hierarchy parts that don't have corresponding pages.
These folders help organize related content even when there's no explicit index page.

### Index Page Handling

Pages named `index` get special treatment - they represent both the folder and a navigable page. An index page like
`/getting-started/index` becomes:

- A clickable navigation item with the folder's name
- A container that can hold child pages

</Step>
<Step stepNumber="3">

## Navigation Entry Types

The system creates different types of navigation entries based on your content structure:

Regular Pages
   : Standard content pages that appear as individual navigation items. They can have child pages if other content exists
   beneath them in the URL hierarchy.

Index Pages
   : Special pages that serve dual purposes - they're both clickable navigation items and containers for child pages. When
   you have an index page, it becomes the representative for its entire folder.

Folder Containers
   : When you have pages in a subfolder but no index page, the system creates a non-clickable folder entry that organizes the
   child pages.

Folder Absorption
   : If a folder contains an index page, the folder "absorbs" the index page's properties. The navigation shows the index
   page's title and link, but includes all the folder's other children as sub-items.

</Step>
<Step stepNumber="4">

## Automatic Naming

When folders don't have explicit index pages, the system generates readable names from URL segments:

### Converting Hierarchy Parts to Titles

Hierarchy parts like `getting-started` are automatically converted to proper titles like "Getting Started". The system:

- Converts dashes to spaces
- Handles double dashes specially (preserves them as single dashes)
- Applies proper title case formatting

### Title Case Rules

The system uses APA title case:

- Always capitalizes the first word and important words
- Keeps articles, conjunctions, and short prepositions lowercase (unless they start a title)
- Capitalizes both parts of hyphenated words

Examples:

- `api-reference` → "API Reference"
- `under-the-hood` → "Under the Hood"
- `getting-started` → "Getting Started"
- `how--to` → "How-To" (double dash preserved as single dash)

</Step>
<Step stepNumber="5">

## Selection and Active States

The navigation system tracks which page you're currently viewing and highlights the appropriate navigation items:

The navigation entry matching your current URL is marked as selected and visually highlighted.


All parent folders and sections containing the current page are also marked as selected, creating a visual breadcrumb
effect in the navigation.

</Step>
<Step stepNumber="6">

## Ordering and Sorting

Navigation items are sorted based on the `order` values from your front matter:

Set explicit `order` values in your front matter to control navigation sequence:

```yaml
---
title: "Installation Guide"
order: 100
---
```

Pages without explicit order values appear after those with order values, sorted alphabetically.

Folders inherit the order of their lowest-ordered child page, ensuring logical grouping in the navigation.

</Step>
</Steps>

## Practical Example

Consider this content structure:

```
Content/
├── index.md (order: 1)
├── getting-started/
│   ├── index.md (order: 100)
│   ├── installation.md (order: 101)
│   └── first-steps.md (order: 102)
└── api/
    ├── classes/
    │   └── ContentService.md (order: 200)
    └── interfaces/
        └── IContentService.md (order: 201)
```

This would generate navigation like:

1. **Home** (clickable, goes to index.md)
2. **Getting Started** (clickable, goes to getting-started/index.md)
    - **Installation** (clickable, goes to installation.md)
    - **First Steps** (clickable, goes to first-steps.md)
3. **API** (folder container, not clickable)
    - **Classes** (folder container, not clickable)
        - **ContentService** (clickable, goes to API page)
    - **Interfaces** (folder container, not clickable)
        - **IContentService** (clickable, goes to API page)

The system automatically handles the hierarchy, creates readable folder names, respects your custom ordering, and
provides both tree-based and sequential navigation.