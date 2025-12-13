using System.Collections.Immutable;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;
using MyLittleContentEngine.Tests.TestHelpers;
using Moq;
using Shouldly;

namespace MyLittleContentEngine.Tests.Services.Content.TableOfContents;

public class NavigationInfoTests
{
    [Fact]
    public async Task GetNavigationInfoAsync_ReturnsCorrectBreadcrumbs_ForCliSection()
    {
        // Arrange
        var mockContentService = new Mock<IContentService>();
        mockContentService.Setup(x => x.DefaultSection).Returns("cli");
        mockContentService.Setup(x => x.GetContentTocEntriesAsync()).ReturnsAsync(
            ImmutableList.Create(
                new ContentTocItem("Getting Started", "/cli/getting-started", 1, ["getting-started"]),
                new ContentTocItem("Configuration", "/cli/advanced/configuration", 2, ["advanced", "configuration"]),
                new ContentTocItem("Troubleshooting", "/cli/troubleshooting", 3, ["troubleshooting"])
            )
        );

        var tableOfContentService = ServiceMockFactory.CreateTableOfContentService(mockContentService.Object);

        // Act
        var navigationInfo = await tableOfContentService.GetNavigationInfoAsync("/cli/advanced/configuration");

        // Assert
        navigationInfo.ShouldNotBeNull();
        navigationInfo.SectionName.ShouldBe("CLI");
        navigationInfo.SectionPath.ShouldBe("cli");
        navigationInfo.PageTitle.ShouldBe("Configuration");
        
        // Check breadcrumbs (should skip the duplicate "cli" from hierarchy)
        navigationInfo.Breadcrumbs.Count.ShouldBe(4);
        navigationInfo.Breadcrumbs[0].Name.ShouldBe("Home");
        navigationInfo.Breadcrumbs[0].Href.ShouldBe("/");
        navigationInfo.Breadcrumbs[0].IsCurrent.ShouldBeFalse();
        
        navigationInfo.Breadcrumbs[1].Name.ShouldBe("CLI");
        navigationInfo.Breadcrumbs[1].Href.ShouldBeNull();  // No actual page at /cli, so href should be null
        navigationInfo.Breadcrumbs[1].IsCurrent.ShouldBeFalse();
        
        navigationInfo.Breadcrumbs[2].Name.ShouldBe("Advanced");
        navigationInfo.Breadcrumbs[2].Href.ShouldBeNull();  // No actual page at /cli/advanced
        navigationInfo.Breadcrumbs[2].IsCurrent.ShouldBeFalse();
        
        navigationInfo.Breadcrumbs[3].Name.ShouldBe("Configuration");
        navigationInfo.Breadcrumbs[3].Href.ShouldBeNull();
        navigationInfo.Breadcrumbs[3].IsCurrent.ShouldBeTrue();
        
        // Check next/previous navigation
        navigationInfo.PreviousPage.ShouldNotBeNull();
        navigationInfo.PreviousPage.Name.ShouldBe("Getting Started");
        navigationInfo.PreviousPage.Href.ShouldBe("/cli/getting-started");
        
        navigationInfo.NextPage.ShouldNotBeNull();
        navigationInfo.NextPage.Name.ShouldBe("Troubleshooting");
        navigationInfo.NextPage.Href.ShouldBe("/cli/troubleshooting");
    }

    [Fact]
    public async Task GetNavigationInfoAsync_ReturnsNull_ForNonExistentUrl()
    {
        // Arrange
        var mockContentService = new Mock<IContentService>();
        mockContentService.Setup(x => x.DefaultSection).Returns("docs");
        mockContentService.Setup(x => x.GetContentTocEntriesAsync()).ReturnsAsync(
            ImmutableList.Create(
                new ContentTocItem("Home", "/", 1, [])
            )
        );

        var tableOfContentService = ServiceMockFactory.CreateTableOfContentService(mockContentService.Object);

        // Act
        var navigationInfo = await tableOfContentService.GetNavigationInfoAsync("/nonexistent/page");

        // Assert
        navigationInfo.ShouldBeNull();
    }

    [Fact]
    public async Task GetNavigationInfoAsync_HandlesRootPage_Correctly()
    {
        // Arrange
        var mockContentService = new Mock<IContentService>();
        mockContentService.Setup(x => x.DefaultSection).Returns("");
        mockContentService.Setup(x => x.GetContentTocEntriesAsync()).ReturnsAsync(
            ImmutableList.Create(
                new ContentTocItem("Welcome", "/", 1, [])
            )
        );

        var tableOfContentService = ServiceMockFactory.CreateTableOfContentService(mockContentService.Object);

        // Act
        var navigationInfo = await tableOfContentService.GetNavigationInfoAsync("/");

        // Assert
        navigationInfo.ShouldNotBeNull();
        navigationInfo.SectionName.ShouldBe("Home");
        navigationInfo.SectionPath.ShouldBe("");
        navigationInfo.PageTitle.ShouldBe("Welcome");
        
        // Root page should have Welcome (actual home page title) and itself in breadcrumbs
        navigationInfo.Breadcrumbs.Count.ShouldBe(2);
        navigationInfo.Breadcrumbs[0].Name.ShouldBe("Welcome");  // Uses actual home page title
        navigationInfo.Breadcrumbs[0].Href.ShouldBe("/");
        navigationInfo.Breadcrumbs[0].IsCurrent.ShouldBeFalse();
        
        navigationInfo.Breadcrumbs[1].Name.ShouldBe("Welcome");
        navigationInfo.Breadcrumbs[1].Href.ShouldBeNull();
        navigationInfo.Breadcrumbs[1].IsCurrent.ShouldBeTrue();
    }

    [Fact]
    public async Task GetNavigationInfoAsync_FormatsApiAbbreviation_Correctly()
    {
        // Arrange
        var mockApiService = new Mock<IContentService>();
        mockApiService.Setup(x => x.DefaultSection).Returns("api");
        mockApiService.Setup(x => x.GetContentTocEntriesAsync()).ReturnsAsync(
            ImmutableList.Create(
                new ContentTocItem("API Reference", "/api/reference", 1, ["reference"])
            )
        );

        var tableOfContentService = ServiceMockFactory.CreateTableOfContentService(mockApiService.Object);

        // Act
        var apiInfo = await tableOfContentService.GetNavigationInfoAsync("/api/reference");

        // Assert
        apiInfo.ShouldNotBeNull();
        apiInfo.SectionName.ShouldBe("API");
    }
    
    [Fact]
    public async Task GetNavigationInfoAsync_FormatsFaqAbbreviation_Correctly()
    {
        // Arrange
        var mockFaqService = new Mock<IContentService>();
        mockFaqService.Setup(x => x.DefaultSection).Returns("faq");
        mockFaqService.Setup(x => x.GetContentTocEntriesAsync()).ReturnsAsync(
            ImmutableList.Create(
                new ContentTocItem("Common Questions", "/faq/common", 1, ["common"])
            )
        );

        var tableOfContentService = ServiceMockFactory.CreateTableOfContentService(mockFaqService.Object);

        // Act
        var faqInfo = await tableOfContentService.GetNavigationInfoAsync("/faq/common");

        // Assert
        faqInfo.ShouldNotBeNull();
        faqInfo.SectionName.ShouldBe("FAQ");
    }
    
    [Fact]
    public async Task GetNavigationInfoAsync_SkipsDuplicateSectionInHierarchy()
    {
        // Arrange - Simulate a case where the folder structure includes the section name
        var mockContentService = new Mock<IContentService>();
        mockContentService.Setup(x => x.DefaultSection).Returns("cli");
        mockContentService.Setup(x => x.GetContentTocEntriesAsync()).ReturnsAsync(
            ImmutableList.Create(
                // Hierarchy parts include "Cli" as first element (from folder structure)
                new ContentTocItem("Working with Multiple Command Hierarchies", 
                    "/cli/how-to/working-with-multiple-command-hierarchies", 
                    1, 
                    ["Cli", "How-To", "working-with-multiple-command-hierarchies"])
            )
        );

        var tableOfContentService = ServiceMockFactory.CreateTableOfContentService(mockContentService.Object);

        // Act
        var navigationInfo = await tableOfContentService.GetNavigationInfoAsync("/cli/how-to/working-with-multiple-command-hierarchies");

        // Assert - Should not have duplicate "CLI" in breadcrumbs
        navigationInfo.ShouldNotBeNull();
        navigationInfo.Breadcrumbs.Count.ShouldBe(4);
        
        navigationInfo.Breadcrumbs[0].Name.ShouldBe("Home");
        navigationInfo.Breadcrumbs[1].Name.ShouldBe("CLI"); // Section breadcrumb
        navigationInfo.Breadcrumbs[2].Name.ShouldBe("How To"); // Formatted from "How-To"
        navigationInfo.Breadcrumbs[3].Name.ShouldBe("Working with Multiple Command Hierarchies");
        
        // The URL for "How To" should be null since there's no page there
        navigationInfo.Breadcrumbs[2].Href.ShouldBeNull();
    }
    
    [Fact]
    public async Task GetNavigationInfoAsync_SkipsSectionBreadcrumb_WhenOnSectionIndexPage()
    {
        // Arrange - Simulate visiting a section's index page
        var mockContentService = new Mock<IContentService>();
        mockContentService.Setup(x => x.DefaultSection).Returns("console");
        mockContentService.Setup(x => x.GetContentTocEntriesAsync()).ReturnsAsync(
            ImmutableList.Create(
                new ContentTocItem("Console Documentation", "/console", 1, []),
                new ContentTocItem("Getting Started", "/console/getting-started", 2, ["getting-started"])
            )
        );

        var tableOfContentService = ServiceMockFactory.CreateTableOfContentService(mockContentService.Object);

        // Act - Navigate to the section index page
        var navigationInfo = await tableOfContentService.GetNavigationInfoAsync("/console");

        // Assert - Should not have duplicate "Console Documentation" in breadcrumbs
        navigationInfo.ShouldNotBeNull();
        navigationInfo.Breadcrumbs.Count.ShouldBe(2); // Home and current page only
        
        navigationInfo.Breadcrumbs[0].Name.ShouldBe("Home");
        navigationInfo.Breadcrumbs[0].Href.ShouldBe("/");
        navigationInfo.Breadcrumbs[0].IsCurrent.ShouldBeFalse();
        
        navigationInfo.Breadcrumbs[1].Name.ShouldBe("Console Documentation"); // Current page
        navigationInfo.Breadcrumbs[1].Href.ShouldBeNull();
        navigationInfo.Breadcrumbs[1].IsCurrent.ShouldBeTrue();
    }
    
    [Fact]
    public async Task GetNavigationInfoAsync_HandlesFirstAndLastPages_Correctly()
    {
        // Arrange
        var mockContentService = new Mock<IContentService>();
        mockContentService.Setup(x => x.DefaultSection).Returns("docs");
        mockContentService.Setup(x => x.GetContentTocEntriesAsync()).ReturnsAsync(
            ImmutableList.Create(
                new ContentTocItem("Introduction", "/docs/intro", 1, ["intro"]),
                new ContentTocItem("Getting Started", "/docs/getting-started", 2, ["getting-started"]),
                new ContentTocItem("Advanced Topics", "/docs/advanced", 3, ["advanced"])
            )
        );

        var tableOfContentService = ServiceMockFactory.CreateTableOfContentService(mockContentService.Object);

        // Act - First page
        var firstPageInfo = await tableOfContentService.GetNavigationInfoAsync("/docs/intro");
        
        // Assert - First page has no previous
        firstPageInfo.ShouldNotBeNull();
        firstPageInfo.PreviousPage.ShouldBeNull();
        firstPageInfo.NextPage.ShouldNotBeNull();
        firstPageInfo.NextPage.Name.ShouldBe("Getting Started");
        
        // Act - Last page
        var lastPageInfo = await tableOfContentService.GetNavigationInfoAsync("/docs/advanced");
        
        // Assert - Last page has no next
        lastPageInfo.ShouldNotBeNull();
        lastPageInfo.PreviousPage.ShouldNotBeNull();
        lastPageInfo.PreviousPage.Name.ShouldBe("Getting Started");
        lastPageInfo.NextPage.ShouldBeNull();
    }
}