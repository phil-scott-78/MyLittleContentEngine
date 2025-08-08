# Create Documentation Example

Creates a runnable code example for documentation that follows the established pattern.

## What this command does:
1. Reads the specified documentation file to understand the content and structure
2. Creates a runnable example class in the appropriate namespace folder
3. Breaks down the documentation content into separate methods for easy extraction
4. Ensures the example implements the IExample interface
5. Tests that the example builds and runs correctly

## Arguments:
- `<documentation-path>`: Path to the .md documentation file (relative to examples/Spectre.Console/Content/)
- `<example-namespace>`: The namespace path for the example (e.g., "console.tutorials" or "cli.how-to")

Extract those these from $ARGUMENTS. If example-namespace isn't given, infer from the documentation path

## Examples:
```bash
/create-example console/tutorials/interactive-prompt-and-dashboard-tutorial.md console.tutorials
/create-example cli/tutorials/quick-start-your-first-cli-app.md cli.tutorials
/create-example console/how-to/displaying-tables-and-trees.md console.how-to
```

## Implementation Requirements:

You are creating runnable code examples for Spectre.Console documentation. Follow these patterns:

### File Structure:
- Examples go in `examples/Spectre.Console.Examples/{namespace-path}/`  
- Class name should be `{DocumentTitle}Example` (PascalCase, remove spaces/hyphens)
- Namespace should be `Spectre.Console.Examples.{Namespace.Path}` (PascalCase)

### Code Pattern:
```csharp
using Spectre.Console;

namespace Spectre.Console.Examples.{Namespace.Path};

/// <summary>
/// Brief description matching the documentation summary.
/// Explain what concepts/features this example demonstrates.
/// </summary>
public class {DocumentTitle}Example : IExample
{
    public void Run(string[] args)
    {
        AnsiConsole.MarkupLine("[bold green]{Document Title}[/]");
        AnsiConsole.WriteLine();

        // Call separate methods for each major concept
        ShowConceptOne();
        AnsiConsole.WriteLine();
        
        ShowConceptTwo();
        // etc...
    }

    /// <summary>
    /// Method documentation explaining this specific concept.
    /// Should be extractable for documentation generation.
    /// </summary>
    public void ShowConceptOne()
    {
        AnsiConsole.MarkupLine("[bold yellow]Step N: Concept Title[/]");
        // Implementation demonstrating the concept
    }
    
    // Additional methods for other concepts...
}
```

### Method Guidelines:
- Each major concept/step gets its own method with `Show{ConceptName}()` naming
- Methods should have XML documentation explaining what they demonstrate
- Use clear section headers with `[bold yellow]Step N: Title[/]` markup
- Include practical examples that users can understand and modify
- Keep methods focused on single concepts for easy extraction

### Testing Requirements:
- Always run `dotnet build` to ensure the code compiles
- Test with `dotnet run {example-name}` to verify it executes correctly
- Fix any compilation errors before completing

### Content Extraction Considerations:
- Break complex tutorials into logical steps as separate methods
- Use descriptive method names that indicate what concept they demonstrate  
- Include comprehensive XML documentation for each method
- Ensure examples are complete and runnable, not just code snippets

The goal is to create examples where your documentation tool can extract individual class and method bodies to include as runnable code samples in the documentation.