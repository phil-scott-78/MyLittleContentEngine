---
title: "Adding Blog Posts"
description: "Learn how to create, organize, and manage blog content with front matter and tags"
order: 3
tags:
  - tutorial
  - content-creation
  - blogging
---

In this tutorial, you'll learn how to effectively create and manage blog posts using MyLittleContentEngine. You'll explore advanced front matter usage, content organization strategies, and best practices for maintaining a blog.

## Prerequisites

Before starting this tutorial, you should have:
- Completed the [Creating Your First Site](creating-first-site.md) tutorial
- A basic understanding of [core concepts](getting-started.md)
- A working MyLittleContentEngine project

## Blog Post Structure

Every blog post consists of two parts:

1. **Front Matter** - YAML metadata at the top
2. **Content** - Markdown content below

```markdown
---
title: "How to Master Blazor Components"
description: "A comprehensive guide to building reusable Blazor components"
date: 2025-01-20
tags:
  - blazor
  - components
  - csharp
isDraft: false
series: "Blazor Mastery"
author: "John Developer"
---

Your markdown content starts here...
```

## Enhanced Front Matter

Let's extend your `BlogPost` model to support more features:

```csharp
using MyLittleContentEngine.Models;

namespace MyFirstContentSite;

public class BlogPost : IFrontMatter
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsDraft { get; set; }
    
    // Additional properties for richer content
    public string? Series { get; set; }
    public string? Author { get; set; }
    public TimeSpan? ReadingTime { get; set; }
    public string? FeaturedImage { get; set; }
    public string? Excerpt { get; set; }
    public int Order { get; set; } = 0;
}
```

## Content Organization Strategies

### Date-Based Organization

The most common approach for blogs:

```
Content/Blog/
├── 2024/
│   ├── 12/
│   │   ├── year-end-review.md
│   │   └── upcoming-features.md
├── 2025/
│   ├── 01/
│   │   ├── new-year-goals.md
│   │   ├── getting-started-guide.md
│   │   └── media/
│   │       └── hero-image.jpg
│   └── 02/
│       └── advanced-techniques.md
```

### Topic-Based Organization

Alternative approach for focused content:

```
Content/Blog/
├── tutorials/
│   ├── blazor-basics.md
│   └── advanced-blazor.md
├── reviews/
│   ├── tool-review-2025.md
│   └── book-recommendations.md
├── announcements/
│   └── new-feature-release.md
```

## Creating Your First Blog Post

Let's create a comprehensive blog post step by step.

### Step 1: Plan Your Content

Before writing, consider:
- **Topic**: What specific problem are you solving?
- **Audience**: Who is this for (beginners, experts, etc.)?
- **Tags**: How will readers find this content?
- **Series**: Does this belong to a series of related posts?

### Step 2: Create the File

Create `Content/Blog/2025/01/mastering-blazor-forms.md`:

```yaml
---
title: "Mastering Blazor Forms: From Basic to Advanced"
description: "Learn how to build robust, validated forms in Blazor with practical examples and best practices"
date: 2025-01-22
tags:
  - blazor
  - forms
  - validation
  - csharp
isDraft: false
series: "Blazor Mastery"
author: "Your Name"
readingTime: "00:08:00"
featuredImage: "/media/2025/01/blazor-forms-hero.jpg"
order: 2
---

Building forms is a fundamental skill for any web developer. In Blazor, forms combine the power of C# with modern web standards to create robust, type-safe user interfaces.

## Why Blazor Forms Matter

Traditional web forms often suffer from:
- Weak typing and runtime errors
- Complex validation logic
- Poor user experience patterns
- Difficult testing scenarios

Blazor forms solve these problems by providing:
- **Strong typing** with compile-time checking
- **Built-in validation** with data annotations
- **Component-based architecture** for reusability
- **Seamless state management** with two-way binding

## Basic Form Example

Let's start with a simple contact form:

```razor
@page "/contact"
@using System.ComponentModel.DataAnnotations

<EditForm Model="contactModel" OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />
    
    <div class="form-group">
        <label for="name">Name:</label>
        <InputText id="name" @bind-Value="contactModel.Name" class="form-control" />
        <ValidationMessage For="@(() => contactModel.Name)" />
    </div>
    
    <div class="form-group">
        <label for="email">Email:</label>
        <InputText id="email" @bind-Value="contactModel.Email" class="form-control" />
        <ValidationMessage For="@(() => contactModel.Email)" />
    </div>
    
    <div class="form-group">
        <label for="message">Message:</label>
        <InputTextArea id="message" @bind-Value="contactModel.Message" class="form-control" rows="4" />
        <ValidationMessage For="@(() => contactModel.Message)" />
    </div>
    
    <button type="submit" class="btn btn-primary">Send Message</button>
</EditForm>

@code {
    private ContactModel contactModel = new();
    
    private async Task HandleSubmit()
    {
        // Handle form submission
        Console.WriteLine($"Contact from {contactModel.Name}: {contactModel.Message}");
    }
    
    public class ContactModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, ErrorMessage = "Name must be less than 50 characters")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Message is required")]
        [StringLength(500, ErrorMessage = "Message must be less than 500 characters")]
        public string Message { get; set; } = string.Empty;
    }
}
```

## Advanced Validation Techniques

### Custom Validation Attributes

Create reusable validation logic:

```csharp
public class NoSwearWordsAttribute : ValidationAttribute
{
    private readonly string[] forbiddenWords = { "spam", "bad", "terrible" };
    
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string text && forbiddenWords.Any(word => 
            text.Contains(word, StringComparison.OrdinalIgnoreCase)))
        {
            return new ValidationResult("Message contains inappropriate content");
        }
        
        return ValidationResult.Success;
    }
}
```

### Complex Validation Rules

Handle interdependent field validation:

```csharp
public class EventRegistrationModel : IValidatableObject
{
    [Required]
    public DateTime EventDate { get; set; }
    
    [Required]
    public DateTime RegistrationDeadline { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RegistrationDeadline >= EventDate)
        {
            yield return new ValidationResult(
                "Registration deadline must be before the event date",
                new[] { nameof(RegistrationDeadline) });
        }
    }
}
```

## Form State Management

### Handling Form State

Track form changes and provide user feedback:

```razor
<EditForm Model="model" OnValidSubmit="Save" OnInvalidSubmit="ShowErrors">
    <!-- Form fields -->
    
    <div class="form-actions">
        <button type="submit" class="btn btn-primary" disabled="@isSubmitting">
            @if (isSubmitting)
            {
                <span class="spinner-border spinner-border-sm me-2"></span>
            }
            @(isSubmitting ? "Saving..." : "Save")
        </button>
        
        @if (showSuccessMessage)
        {
            <div class="alert alert-success mt-2">Form saved successfully!</div>
        }
    </div>
</EditForm>

@code {
    private bool isSubmitting = false;
    private bool showSuccessMessage = false;
    
    private async Task Save()
    {
        isSubmitting = true;
        showSuccessMessage = false;
        
        try
        {
            // Simulate API call
            await Task.Delay(2000);
            showSuccessMessage = true;
        }
        finally
        {
            isSubmitting = false;
        }
    }
}
```

## Best Practices for Blog Content

### 1. Structure Your Posts

Use consistent heading hierarchy:
- `##` for main sections
- `###` for subsections
- `####` for detailed points

### 2. Include Code Examples

Always provide working code examples:

```csharp
// Good: Complete, runnable example
public class UserService
{
    public async Task<User> GetUserAsync(int id)
    {
        // Implementation here
        return await database.Users.FindAsync(id);
    }
}

// Bad: Incomplete snippet
public async Task<User> GetUser...
```

### 3. Add Visual Elements

Break up text with:
- Code blocks
- Lists and bullet points
- Blockquotes for important notes
- Images and diagrams (when helpful)

### 4. Provide Context

Explain not just "how" but "why":

> **💡 Pro Tip**: Use `EditForm` instead of plain HTML forms in Blazor because it provides automatic validation integration and better component lifecycle management.

## Managing Drafts and Publishing

### Draft Workflow

Keep posts as drafts while working:

```yaml
---
title: "Work in Progress Post"
isDraft: true  # This post won't appear on the site
---
```

### Publishing Checklist

Before publishing, verify:
- [ ] All code examples are tested and working
- [ ] Images are optimized and properly referenced
- [ ] Tags are relevant and consistent with existing content
- [ ] Front matter is complete and accurate
- [ ] Content is proofread for clarity and errors
- [ ] Links to other posts or external resources work correctly

## Series Management

### Creating a Series

Link related posts using the `series` field:

```yaml
# Post 1
---
title: "Blazor Mastery: Getting Started"
series: "Blazor Mastery"
order: 1
---

# Post 2  
---
title: "Blazor Mastery: Components"
series: "Blazor Mastery"
order: 2
---
```

### Displaying Series Information

Create a component to show series navigation:

```razor
@if (!string.IsNullOrEmpty(Post.FrontMatter.Series))
{
    <div class="series-nav card">
        <div class="card-header">
            <h6>📚 Part of the "@Post.FrontMatter.Series" series</h6>
        </div>
        <div class="card-body">
            @foreach (var seriesPost in SeriesPosts.OrderBy(p => p.FrontMatter.Order))
            {
                <div class="series-item">
                    @if (seriesPost.Url == Post.Url)
                    {
                        <strong>@seriesPost.FrontMatter.Title</strong> (current)
                    }
                    else
                    {
                        <a href="@seriesPost.Url">@seriesPost.FrontMatter.Title</a>
                    }
                </div>
            }
        </div>
    </div>
}
```

## What You've Learned

In this tutorial, you've mastered:

1. ✅ Enhanced front matter with additional properties
2. ✅ Content organization strategies (date-based vs. topic-based)
3. ✅ Creating comprehensive blog posts with examples
4. ✅ Advanced Blazor form techniques and validation
5. ✅ Content management best practices
6. ✅ Draft workflow and publishing checklist
7. ✅ Series management for related content

## Next Steps

Now you're ready to:

- **[Explore how-to guides](../how-to/index.md)** - Learn specific implementation techniques
- **[Configure content options](../how-to/configure-content-options.md)** - Customize your content processing
- **[Work with tags](../how-to/work-with-tags.md)** - Master content categorization
- **[Understand the content pipeline](../explanation/content-pipeline.md)** - Learn how content is processed

## Troubleshooting

### Front matter validation errors?
- Ensure all required properties are set
- Check YAML syntax (proper indentation, colons, etc.)
- Verify date format: `2025-01-22`

### Content not appearing?
- Check `isDraft: false` in front matter
- Verify file is in the correct directory
- Ensure front matter model matches your C# class

### Build failures?
- Validate all markdown syntax
- Check that images and links are valid
- Ensure code examples are properly formatted

You now have all the tools needed to create engaging, well-structured blog content with MyLittleContentEngine!
```

This completes the core content for the mastering forms blog post example. The post demonstrates practical Blazor form techniques while serving as an example of well-structured blog content.