# Write Documentation

Writes comprehensive documentation using C# xmldocid references to existing example classes, following Diátaxis framework principles.

## What this command does:
1. Reads an existing example class to understand its structure and methods
2. Creates documentation following the appropriate Diátaxis style (tutorial, how-to, explanation, reference)
3. Uses C# xmldocid references instead of inline code samples
4. Maintains an approachable, friendly tone throughout
5. Includes proper progression and learning structure

## Arguments:
- `<example-class-path>`: Path to the example class (relative to examples/Spectre.Console.Examples/)
- `<documentation-path>`: Path where documentation should be written (relative to examples/Spectre.Console/Content/)

Extract these from $ARGUMENTS. If documentation-path isn't given, infer from the example class path.

## Examples:
```bash
/write-documentation console/tutorials/GettingStartedExample.cs console/tutorials/getting-started-building-rich-console-app.md
/write-documentation cli/how-to/ConfiguringCommandsExample.cs cli/how-to/configuring-commandapp-and-commands.md
/write-documentation console/explanation/RenderingModelExample.cs console/explanation/understanding-rendering-model.md
```

## Diátaxis Framework Guidelines:

### Tutorials (learning-oriented)
- **Goal**: Take beginners from zero to a working result
- **Tone**: Encouraging, patient, like a friendly teacher
- **Structure**: Step-by-step, building complexity gradually
- **Focus**: What the user will accomplish and learn
- **Language**: "Let's...", "You'll...", "Now we can..."

### How-to Guides (problem-oriented)
- **Goal**: Solve specific, real-world problems
- **Tone**: Direct, efficient, solutions-focused
- **Structure**: Clear steps to achieve the goal
- **Focus**: Practical solutions to common needs
- **Language**: "To do X...", "When you need...", "This approach..."

### Explanations (understanding-oriented)
- **Goal**: Clarify and illuminate concepts
- **Tone**: Thoughtful, informative, like a knowledgeable colleague
- **Structure**: Topic-based, connecting ideas
- **Focus**: Why things work the way they do
- **Language**: "The reason...", "This happens because...", "Consider..."

### Reference (information-oriented)
- **Goal**: Provide accurate, comprehensive information
- **Tone**: Neutral, precise, authoritative
- **Structure**: Systematic, complete coverage
- **Focus**: Facts, parameters, specifications
- **Language**: "This parameter...", "Returns...", "Available options..."

## Documentation Structure by Type:

### Tutorial Pattern:
```markdown
---
title: "Getting Started: [Friendly Action Title]"
description: "A beginner-friendly tutorial that..."
section: "Console"
---

Welcome! This tutorial will guide you through... By the end, you'll have...

## What You'll Build
[Exciting overview of the end result]

## Before We Start
[Prerequisites in encouraging tone]

## Step 1: [First Concept]
Let's start by... [xmldocid reference + explanation]

## Step 2: [Next Concept]  
Now that you've..., let's add... [xmldocid reference + explanation]

## Putting It All Together
[Complete example reference and celebration of achievement]

## What's Next?
[Encouraging pointers to next learning steps]
```

### How-to Pattern:
```markdown
---
title: "[Specific Task]: [Clear Outcome]"
description: "Learn how to [specific goal] using [specific approach]"
---

## Problem
[Brief description of what this solves]

## Solution
[xmldocid reference to main approach]

## Step-by-Step
1. [Action step with xmldocid reference]
2. [Action step with xmldocid reference]
3. [Action step with xmldocid reference]

## Variations
[Alternative approaches or configurations]

## Common Issues
[Troubleshooting tips]
```

### Explanation Pattern:
```markdown
---
title: "[Concept Name]: [What It Explains]"
description: "Understanding [concept] and how it works in Spectre.Console"
---

## Overview
[High-level explanation of the concept]

## How It Works
[Detailed explanation with xmldocid references to illustrative code]

## Why This Matters
[Practical implications and benefits]

## Key Concepts
[Important principles and relationships]

## In Practice
[xmldocid reference showing real-world usage]
```

## Writing Style Guidelines:

### Approachable Tone:
- Use "you" and "we" to create connection
- Avoid jargon without explanation
- Celebrate small wins and progress
- Acknowledge when things might be confusing
- Use encouraging language: "Great!", "Perfect!", "You've got this!"

### Clear Structure:
- Short paragraphs (2-3 sentences max)
- Bullet points for lists
- Clear headings that preview content
- Logical flow from simple to complex

### xmldocid Integration:
```csharp
// Method references for specific techniques:
```csharp:xmldocid
M:Spectre.Console.Examples.{Namespace}.{ClassName}.{MethodName}
```

// Class references for complete examples:
```csharp:xmldocid
T:Spectre.Console.Examples.{Namespace}.{ClassName}
```
```

### Content Quality:
- Explain the "why" behind each concept
- Connect features to real-world use cases
- Include context about when to use different approaches
- Reference related concepts and next steps
- Maintain consistency with established patterns

The goal is to create documentation that feels helpful and encouraging while providing practical, actionable information through tested code examples.