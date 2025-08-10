using Shouldly;

namespace MyLittleContentEngine.Tests.TestHelpers;

/// <summary>
/// Example test demonstrating the usage of test helper utilities.
/// This serves as both documentation and verification that the helpers work correctly.
/// </summary>
public class TestHelpersExampleTest
{
    [Fact]
    public async Task ContentEngineTestBuilder_WithSampleContent_CreatesContentService()
    {
        // Arrange
        var builder = ContentEngineTestBuilder.WithSampleContent();
        
        // Act
        var contentService = await builder.BuildContentServiceAsync();
        var pages = await contentService.GetPagesToGenerateAsync();
        
        // Assert
        pages.ShouldNotBeEmpty();
        pages.Count.ShouldBe(3); // From WithSampleContent
    }

    [Fact]
    public async Task ServiceMockFactory_CreateContentService_ReturnsExpectedPages()
    {
        // Arrange
        var mockService = ServiceMockFactory.CreateContentService(
            ("Home", "/", 1),
            ("About", "/about", 2)
        );
        
        // Act
        var pages = await mockService.Object.GetPagesToGenerateAsync();
        
        // Assert
        pages.Count.ShouldBe(2);
        pages[0].Metadata!.Title.ShouldBe("Home");
        pages[1].Metadata!.Title.ShouldBe("About");
    }

    [Fact]
    public void MarkdownTestData_SampleFiles_ProvidesVariousScenarios()
    {
        // Arrange & Act
        var allSamples = MarkdownTestData.SampleFiles;
        var publishedOnly = MarkdownTestData.PublishedFiles;
        var draftsOnly = MarkdownTestData.DraftFiles;
        
        // Assert
        allSamples.Count.ShouldBe(7);
        publishedOnly.Count().ShouldBe(6); // All except the draft
        draftsOnly.Count().ShouldBe(1); // Just the draft
        
        // Verify content variety
        allSamples.ShouldContain(f => f.content.Contains("isDraft: true"));
        allSamples.ShouldContain(f => f.content.Contains("tags:"));
        allSamples.ShouldContain(f => f.content.Contains("```csharp"));
    }

    [Fact]
    public void ServiceMockFactory_PageBuilder_CreatesValidPages()
    {
        // Arrange & Act
        var simplePage = ServiceMockFactory.PageBuilder.Create("Test Page", "/test", 5);
        var richPage = ServiceMockFactory.PageBuilder.CreateRich(
            "Rich Page", 
            "/rich", 
            10, 
            ["tag1", "tag2"]);
        
        // Assert
        simplePage.Metadata!.Title.ShouldBe("Test Page");
        simplePage.Metadata.Order.ShouldBe(5);
        
        richPage.Metadata!.Title.ShouldBe("Rich Page");
        richPage.Metadata.Order.ShouldBe(10);
    }

    [Fact]
    public void MarkdownTestData_CreatePost_GeneratesValidMarkdown()
    {
        // Arrange & Act
        var customPost = MarkdownTestData.CreatePost(
            "Custom Title",
            42,
            "# Custom Content\n\nThis is custom.",
            ["custom", "test"]
        );
        
        // Assert
        customPost.ShouldContain("title: Custom Title");
        customPost.ShouldContain("order: 42");
        customPost.ShouldContain("tags: [\"custom\", \"test\"]");
        customPost.ShouldContain("# Custom Content");
        customPost.ShouldNotContain("isDraft: true");
    }
}