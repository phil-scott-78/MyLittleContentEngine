---
title: "Linking Documents and Media"
description: "Learn how to link to other pages and include media files in your content"
order: 1010
---

Working with links and media in MyLittleContentEngine is straightforward once you understand how paths are resolved relative to your configured `ContentRootPath`.

## Understanding ContentRootPath

When you configure MyLittleContentEngine in your `Program.cs`, you specify a `ContentRootPath`:

```csharp
builder.Services.AddContentEngineService(() => new ContentEngineOptions
{
    ContentRootPath = "Content", // This is your content root
});
```

All image paths and internal links are resolved relative to this content root directory.

## Linking to Images

### Absolute Paths from Content Root

Images can be referenced using absolute paths from your content root. If your content structure looks like this:

```
Content/
├── index.md
├── media/
│   └── photo.jpg
└── sub-folder/
    ├── page.md
    └── local-image.jpg
```

You can reference images using absolute paths from the content root:

```markdown
![Photo](/media/photo.jpg)
```

This works from any page in your content structure, regardless of how deeply nested it is.

### Relative Paths

You can also use relative paths to reference images in the same directory or nearby directories:

```markdown
<!-- From sub-folder/page.md -->
![Local Image](local-image.jpg)
![Photo from Media](../media/photo.jpg)
```

## Linking to Other Pages

### Absolute Links

Link to other pages using absolute paths from your content root:

```markdown
[Home Page](/index)
[Sub-folder Page](/sub-folder/page)
```

Note that you typically omit the `.md` extension in links, as MyLittleContentEngine handles the routing automatically.

### Relative Links

You can also use relative links between pages:

```markdown
<!-- From sub-folder/page.md -->
[Another Page](other-page)
[Back to Home](../index)
```

## Practical Examples

Let's look at some real examples from our sample content:

### Sample Post with Mixed Media

```markdown
## Images from Media Folder
![Unsplash Photo](/media/dan-cristian-padure-8cxJzVpGKk8-unsplash.jpg)

## Image from Current Folder
![Local Photo](kelly-sikkema-rNdkGDOPJLc-unsplash.jpg)
```

### Navigation Between Pages

```markdown
## Navigation
- [Sample Post with Images](sample-post) - Learn about image handling
- [Page Two - Advanced Topics](/sub-folder/page-two) - Dive deeper
- [Home](/index) - Return to the home page
```

## Best Practices

### Consistent Path Strategy

Choose either absolute or relative paths and stick with that approach consistently across your site:

- **Absolute paths** are more reliable when reorganizing content
- **Relative paths** are more portable if you need to move entire sections

### Media Organization

Organize your media files logically:

```
Content/
├── media/           # Site-wide media
├── section-1/
│   ├── images/      # Section-specific images
│   └── page.md
└── section-2/
    ├── images/
    └── page.md
```

### File Naming

Use descriptive, URL-friendly filenames:
- `my-awesome-post.md` instead of `My Awesome Post.md`
- `hero-image.jpg` instead of `Hero Image.jpg`

## Common Issues

### Broken Links After Reorganization

If you move files around, remember to update all references. Absolute paths from the content root are less likely to 
break during reorganization. 

### Case Sensitivity

Be aware that file systems can be case-sensitive. Keep your file names consistently lowercase to avoid issues when deploying to different environments.

### It Loads a Markdown File Instead of Rendering It

You'll want to ensure that your links to markdown files do not include the `.md` extension. ASP.NET 
automatically handles routing for markdown files, so you should link to them without the extension. 
If you include the `.md` extension, it may try to serve the raw markdown file instead of rendering it.