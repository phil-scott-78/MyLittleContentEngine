---
title: "Connecting to Roslyn"
description: "Integrate Roslyn for enhanced code highlighting and documentation in your content site"
uid: "docs.getting-started.connecting-to-roslyn"
order: 1020
---

Because MyLittleContentEngine is a .NET application itself, you can integrate it with Roslyn. By adding the Roslyn
service, we will use Roslyn to provide enhanced code syntax highlighting and access our application's code directly
for documentation.

Once we have Roslyn added, we can also attach a solution file to the Roslyn Service. This allows you 
to use your application's demos and unit tests as documentation, ensuring that your content is always up to date 
with the latest syntax. No longer do you need to copy and paste code snippets into your documentation, instead, 
you can reference your code directly in your code blocks. Changes to your sample apps will be directly displayed.

In this tutorial, you'll learn how to integrate Roslyn with MyLittleContentEngine to enable enhanced code syntax
highlighting and automatic documentation generation from your .NET solution.

## What You'll Learn

By the end of this tutorial, you'll be able to:

- Connect MyLittleContentEngine to a .NET solution using Roslyn
- Use XML documentation ID syntax to reference code elements
- Configure file watching for source code changes
- Create content that automatically updates when your code changes

## Prerequisites

Before starting, ensure you have:

- Completed the [Getting Started](xref:docs.getting-started.creating-first-site) tutorial
- A .NET solution with XML documentation comments
- Familiarity with [XmlDocId](xref:docs.reference.xmldocid-format)

<Steps>
<Step stepNumber="1">
## Start with a Basic Site

We'll start by copying the MinimalExample to create a new site with Roslyn integration. If you don't have the
MinimalExample, follow the [Creating First Site](xref:docs.getting-started.creating-first-site) tutorial first.

</Step>

<Step stepNumber="2">
## Watch Source Code Changes

We need to tell `dotnet watch` to watch our solution for changes. Any files included via the `Watch` item group in the
`.csproj` file will be monitored for changes, triggering a refresh of the site.

Update the `.csproj` file to include source code watching while excluding build artifacts:

```xml

<ItemGroup>
    <Watch Include="Content\**\*.*"/>
    <Watch Include="..\..\src\**\*.cs" Exclude="..\..\src\*\bin\**\*.*;..\..\src\*\obj\**\*.*"/>
</ItemGroup>
```

The source path is relative to the host application root, not solution root. Adjust the path based on your project
structure.

This configuration ensures that:

- Content files are watched for changes (as before)
- Source code files are watched for changes
- Build artifacts in `bin` and `obj` folders are excluded to prevent unnecessary rebuilds. This is important as we will
  be building at runtime for certain actions, and we don't want to cause an unneeded refresh.
  </Step>

<Step stepNumber="3">
## Configure Roslyn Service

Update `Program.cs` to include Roslyn configuration. Add the required using statement:

```csharp
using MyLittleContentEngine.Services.Content.Roslyn;
```

Then add the Roslyn service configuration before `var app = builder.Build();`:

```csharp
// Add Roslyn service for code syntax highlighting and documentation
builder.Services.AddRoslynService(_ => new RoslynHighlighterOptions()
{
    ConnectedSolution = new ConnectedDotNetSolution()
    {
        SolutionPath = "../../{path-to-your-solution}.sln",
    }
});
```

The `SolutionPath` should point to your .NET solution file. This is relative to the host application root. Adjust the
path based on your project structure.
</Step>

<Step stepNumber="4">
## Create Content with XML Documentation ID Syntax

Create a new content file `Content/roslyn-integration-demo.md` to demonstrate the Roslyn features:

```markdown:path
examples/RoslynIntegrationExample/Content/roslyn-integration-demo.md
```

The key feature here is the XML documentation ID syntax in code blocks:

``````markdown
```csharp:xmldocid
T:MyLittleContentEngine.ContentEngineOptions
```
``````

This syntax allows you to reference specific code elements using [XML documentation ID syntax](xref:docs.reference.xmldocid-format).


`bodyonly` can be added to `xmldocid` for scenarios where you only want the body of the method or type without the
declaration. This can be useful for showing just the implementation details without the full signature.


</Step>

<Step stepNumber="5">
## Test the Integration

Run your site in development mode:

```bash
dotnet watch
```

Navigate to your site and visit the Roslyn integration demo page. You should see:

1. **Enhanced syntax highlighting** - Code blocks get proper colorization
2. **Automatic documentation** - XML doc comments are rendered with the code
3. **Live updates** - Changes to your source code are reflected immediately

While the site is running, try:

- Adding new public methods or properties
- Updating the code examples in your content

The changes should be reflected automatically without restarting the server.
</Step>
</Steps>

## Best Practices

When using Roslyn integration:

* **Use meaningful examples** - Reference code that's relevant to your content
* **Watch performance** - Roslyn analysis is going to be resource-intensive for large solutions
* **Test regularly** - Ensure your content updates correctly when code changes