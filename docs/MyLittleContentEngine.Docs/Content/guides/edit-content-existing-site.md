---
title: "Edit Content in an Existing Site"
description: "Use dotnet watch for instant live reload when editing markdown content in your MyLittleContentEngine site"
uid: "docs.guides.edit-content-existing-site"
order: 2020
---

Content editing in static sites often requires manual rebuilds and browser refreshes after every change.
MyLittleContentEngine's `dotnet watch` integration provides instant live reload, automatically detecting markdown
changes and refreshing your browser for immediate feedback.

## The Editing Workflow

Start the development server with file watching:

```bash
dotnet watch
```

Expected output:

```
dotnet watch 🔥 Hot reload enabled. For a list of supported edits, see https://aka.ms/dotnet/hot-reload.
dotnet watch 💡 Press Ctrl+R to restart.
Using launch settings from B:\Website\Properties\launchSettings.json...
2025-12-09 12:57:22 info: Microsoft.Hosting.Lifetime[14] Now listening on: http://localhost:5131
2025-12-09 12:57:22 info: Microsoft.Hosting.Lifetime[0] Application started. Press Ctrl+C to shut down.
2025-12-09 12:57:22 info: Microsoft.Hosting.Lifetime[0] Hosting environment: Development
2025-12-09 12:57:22 info: Microsoft.Hosting.Lifetime[0] Content root path: B:\Website\
```

Open your site in a browser and position it side-by-side with your code editor. Make changes to any markdown file in
your `Content` directory, then save. The console will show:

```
dotnet watch ⌚ File updated: .\Content\guides\edit-content-existing-site.md
dotnet watch ⌚ No C# changes to apply.
```

The "No C# changes to apply" message indicates we don't have a code change, but our content will still be refreshed in
the browser.

## Hot Reload vs. Full Restart

Most content changes trigger **hot reload** (fast, browser auto-refreshes):

- Editing markdown content
- Modifying existing frontmatter values
- Updating images and static assets in the `Content` directory
- Changes to frontmatter that affect navigation structure
- Adding or removing content files
- Adjusting Razor pages

Some changes require a **full restart** (stop with Ctrl+C, then rerun `dotnet watch`):
 
- Modifying `Program.cs` 


## Related Topics

- [Markdown Extensions](xref:docs.guides.markdown-extensions) - Code tabs, alerts, Mermaid diagrams, and more
- [Linking Documents and Media](xref:docs.guides.linking-documents-and-media) - Cross-references, images, and static
  assets
- [Configure Custom Styling](xref:docs.guides.configure-custom-styling) - Customize appearance with MonorailCSS
