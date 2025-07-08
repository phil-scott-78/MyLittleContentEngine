using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.Navigation;
using Shouldly;

namespace MyLittleContentEngine.Tests.Navigation;

public class MarkdownOutlineGeneratorTests
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownOutlineGeneratorTests()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAutoIdentifiers()
            .Build();
    }

    [Fact]
    public void GenerateOutline_WithEmptyDocument_ReturnsEmptyArray()
    {
        var document = Markdown.Parse("", _pipeline);

        var result = MarkdownOutlineGenerator.GenerateOutline(document);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateOutline_WithNoHeadings_ReturnsEmptyArray()
    {
        var markdown = @"
This is a paragraph without any headings.

Another paragraph with some **bold** text.

- A list item
- Another list item
";
        var document = Markdown.Parse(markdown, _pipeline);

        var result = MarkdownOutlineGenerator.GenerateOutline(document);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateOutline_WithSingleHeading_ReturnsSingleEntry()
    {
        var markdown = "# Introduction\n\nSome content here.";
        var document = Markdown.Parse(markdown, _pipeline);

        var result = MarkdownOutlineGenerator.GenerateOutline(document);

        result.ShouldHaveSingleItem();
        result[0].Title.ShouldBe("Introduction");
        result[0].Id.ShouldBe("introduction");
        result[0].Children.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateOutline_WithMultipleTopLevelHeadings_ReturnsMultipleEntries()
    {
        var markdown = @"
# First Section
Content here.

# Second Section
More content.

# Third Section
Even more content.
";
        var document = Markdown.Parse(markdown, _pipeline);

        var result = MarkdownOutlineGenerator.GenerateOutline(document);

        result.Length.ShouldBe(3);
        result[0].Title.ShouldBe("First Section");
        result[0].Id.ShouldBe("first-section");
        result[1].Title.ShouldBe("Second Section");
        result[1].Id.ShouldBe("second-section");
        result[2].Title.ShouldBe("Third Section");
        result[2].Id.ShouldBe("third-section");
        result.All(r => r.Children.Length == 0).ShouldBeTrue();
    }

    [Fact]
    public void GenerateOutline_WithNestedHeadings_CreatesHierarchy()
    {
        var markdown = @"
# Main Section
Content here.

## Subsection A
Subsection content.

## Subsection B
More subsection content.

# Another Main Section
Final content.
";
        var document = Markdown.Parse(markdown, _pipeline);

        var result = MarkdownOutlineGenerator.GenerateOutline(document);

        result.Length.ShouldBe(2);
        
        // First main section
        result[0].Title.ShouldBe("Main Section");
        result[0].Id.ShouldBe("main-section");
        result[0].Children.Length.ShouldBe(2);
        result[0].Children[0].Title.ShouldBe("Subsection A");
        result[0].Children[0].Id.ShouldBe("subsection-a");
        result[0].Children[1].Title.ShouldBe("Subsection B");
        result[0].Children[1].Id.ShouldBe("subsection-b");

        // Second main section
        result[1].Title.ShouldBe("Another Main Section");
        result[1].Id.ShouldBe("another-main-section");
        result[1].Children.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateOutline_WithDeeplyNestedHeadings_CreatesDeepHierarchy()
    {
        var markdown = @"
# Level 1
Content.

## Level 2
Content.

### Level 3
Content.

#### Level 4
Content.

##### Level 5
Content.

###### Level 6
Content.
";
        var document = Markdown.Parse(markdown, _pipeline);

        var result = MarkdownOutlineGenerator.GenerateOutline(document);

        result.ShouldHaveSingleItem();
        
        var current = result[0];
        current.Title.ShouldBe("Level 1");
        current.Id.ShouldBe("level-1");
        
        current = current.Children[0];
        current.Title.ShouldBe("Level 2");
        current.Id.ShouldBe("level-2");
        
        current = current.Children[0];
        current.Title.ShouldBe("Level 3");
        current.Id.ShouldBe("level-3");
        
        current = current.Children[0];
        current.Title.ShouldBe("Level 4");
        current.Id.ShouldBe("level-4");
        
        current = current.Children[0];
        current.Title.ShouldBe("Level 5");
        current.Id.ShouldBe("level-5");
        
        current = current.Children[0];
        current.Title.ShouldBe("Level 6");
        current.Id.ShouldBe("level-6");
        current.Children.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateOutline_WithSkippedHeadingLevels_HandlesCorrectly()
    {
        var markdown = @"
# Level 1
Content.

### Level 3 (skipped level 2)
Content.

##### Level 5 (skipped level 4)
Content.

### Level 3 (back to level 3)
Content.
";
        var document = Markdown.Parse(markdown, _pipeline);

        var result = MarkdownOutlineGenerator.GenerateOutline(document);

        result.ShouldHaveSingleItem();
        result[0].Title.ShouldBe("Level 1");
        result[0].Children.Length.ShouldBe(2);
        
        result[0].Children[0].Title.ShouldBe("Level 3 (skipped level 2)");
        result[0].Children[0].Children.ShouldHaveSingleItem();
        result[0].Children[0].Children[0].Title.ShouldBe("Level 5 (skipped level 4)");
        
        result[0].Children[1].Title.ShouldBe("Level 3 (back to level 3)");
        result[0].Children[1].Children.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateOutline_WithFormattedHeadings_ExtractsPlainText()
    {
        var markdown = @"
# **Bold** Heading
Content.

## *Italic* and `Code` Heading
Content.

## [Link](http://example.com) in Heading
Content.
";
        var document = Markdown.Parse(markdown, _pipeline);

        var result = MarkdownOutlineGenerator.GenerateOutline(document);

        result.ShouldHaveSingleItem();
        result[0].Title.ShouldBe("Bold Heading");
        result[0].Children.Length.ShouldBe(2);
        result[0].Children[0].Title.ShouldBe("Italic and Code Heading");
        result[0].Children[1].Title.ShouldBe("Link in Heading");
    }

    [Fact]
    public void GenerateOutline_WithComplexHierarchy_BuildsCorrectStructure()
    {
        var markdown = @"
# Getting Started
Introduction content.

## Installation
How to install.

### Prerequisites
What you need first.

### Download
How to download.

## Configuration
How to configure.

### Basic Setup
Basic configuration.

### Advanced Setup
Advanced configuration.

#### Database Configuration
Database setup.

#### Cache Configuration
Cache setup.

# User Guide
User guide content.

## Basic Usage
Basic usage information.

## Advanced Features
Advanced features information.
";
        var document = Markdown.Parse(markdown, _pipeline);

        var result = MarkdownOutlineGenerator.GenerateOutline(document);

        result.Length.ShouldBe(2);
        
        // Getting Started section
        var gettingStarted = result[0];
        gettingStarted.Title.ShouldBe("Getting Started");
        gettingStarted.Children.Length.ShouldBe(2);
        
        var installation = gettingStarted.Children[0];
        installation.Title.ShouldBe("Installation");
        installation.Children.Length.ShouldBe(2);
        installation.Children[0].Title.ShouldBe("Prerequisites");
        installation.Children[1].Title.ShouldBe("Download");
        
        var configuration = gettingStarted.Children[1];
        configuration.Title.ShouldBe("Configuration");
        configuration.Children.Length.ShouldBe(2);
        configuration.Children[0].Title.ShouldBe("Basic Setup");
        configuration.Children[1].Title.ShouldBe("Advanced Setup");
        configuration.Children[1].Children.Length.ShouldBe(2);
        configuration.Children[1].Children[0].Title.ShouldBe("Database Configuration");
        configuration.Children[1].Children[1].Title.ShouldBe("Cache Configuration");
        
        // User Guide section
        var userGuide = result[1];
        userGuide.Title.ShouldBe("User Guide");
        userGuide.Children.Length.ShouldBe(2);
        userGuide.Children[0].Title.ShouldBe("Basic Usage");
        userGuide.Children[1].Title.ShouldBe("Advanced Features");
    }

    [Fact]
    public void GenerateOutline_WithHeadingsWithoutIds_SkipsThoseHeadings()
    {
        var pipelineWithoutAutoIds = new MarkdownPipelineBuilder().Build();
        var markdown = @"
# Heading With ID
Content.

## Another Heading
Content.
";
        var document = Markdown.Parse(markdown, pipelineWithoutAutoIds);

        var result = MarkdownOutlineGenerator.GenerateOutline(document);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateOutline_WithEmptyHeadings_SkipsEmptyHeadings()
    {
        var markdown = @"
# Valid Heading
Content.

# 
Content after empty heading.

## Another Valid Heading
More content.
";
        var document = Markdown.Parse(markdown, _pipeline);

        var result = MarkdownOutlineGenerator.GenerateOutline(document);

        result.ShouldHaveSingleItem();
        result[0].Title.ShouldBe("Valid Heading");
        result[0].Children.ShouldHaveSingleItem();
        result[0].Children[0].Title.ShouldBe("Another Valid Heading");
    }
}