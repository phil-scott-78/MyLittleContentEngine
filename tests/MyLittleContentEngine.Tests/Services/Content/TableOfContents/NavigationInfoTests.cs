using System.Collections.Immutable;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;
using Moq;

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

        var tableOfContentService = new TableOfContentService([mockContentService.Object]);

        // Act
        var navigationInfo = await tableOfContentService.GetNavigationInfoAsync("/cli/advanced/configuration");

        // Assert
        Assert.NotNull(navigationInfo);
        Assert.Equal("CLI", navigationInfo.SectionName);
        Assert.Equal("cli", navigationInfo.SectionPath);
        Assert.Equal("Configuration", navigationInfo.PageTitle);
        
        // Check breadcrumbs (should skip the duplicate "cli" from hierarchy)
        Assert.Equal(4, navigationInfo.Breadcrumbs.Count);
        Assert.Equal("Home", navigationInfo.Breadcrumbs[0].Name);
        Assert.Equal("/", navigationInfo.Breadcrumbs[0].Href);
        Assert.False(navigationInfo.Breadcrumbs[0].IsCurrent);
        
        Assert.Equal("CLI", navigationInfo.Breadcrumbs[1].Name);
        Assert.Equal("/cli", navigationInfo.Breadcrumbs[1].Href);
        Assert.False(navigationInfo.Breadcrumbs[1].IsCurrent);
        
        Assert.Equal("Advanced", navigationInfo.Breadcrumbs[2].Name);
        Assert.Equal("/cli/advanced", navigationInfo.Breadcrumbs[2].Href);
        Assert.False(navigationInfo.Breadcrumbs[2].IsCurrent);
        
        Assert.Equal("Configuration", navigationInfo.Breadcrumbs[3].Name);
        Assert.Null(navigationInfo.Breadcrumbs[3].Href);
        Assert.True(navigationInfo.Breadcrumbs[3].IsCurrent);
        
        // Check next/previous navigation
        Assert.NotNull(navigationInfo.PreviousPage);
        Assert.Equal("Getting Started", navigationInfo.PreviousPage.Name);
        Assert.Equal("/cli/getting-started", navigationInfo.PreviousPage.Href);
        
        Assert.NotNull(navigationInfo.NextPage);
        Assert.Equal("Troubleshooting", navigationInfo.NextPage.Name);
        Assert.Equal("/cli/troubleshooting", navigationInfo.NextPage.Href);
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

        var tableOfContentService = new TableOfContentService([mockContentService.Object]);

        // Act
        var navigationInfo = await tableOfContentService.GetNavigationInfoAsync("/nonexistent/page");

        // Assert
        Assert.Null(navigationInfo);
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

        var tableOfContentService = new TableOfContentService([mockContentService.Object]);

        // Act
        var navigationInfo = await tableOfContentService.GetNavigationInfoAsync("/");

        // Assert
        Assert.NotNull(navigationInfo);
        Assert.Equal("Home", navigationInfo.SectionName);
        Assert.Equal("", navigationInfo.SectionPath);
        Assert.Equal("Welcome", navigationInfo.PageTitle);
        
        // Root page should have Home and itself in breadcrumbs
        Assert.Equal(2, navigationInfo.Breadcrumbs.Count);
        Assert.Equal("Home", navigationInfo.Breadcrumbs[0].Name);
        Assert.Equal("/", navigationInfo.Breadcrumbs[0].Href);
        Assert.False(navigationInfo.Breadcrumbs[0].IsCurrent);
        
        Assert.Equal("Welcome", navigationInfo.Breadcrumbs[1].Name);
        Assert.Null(navigationInfo.Breadcrumbs[1].Href);
        Assert.True(navigationInfo.Breadcrumbs[1].IsCurrent);
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

        var tableOfContentService = new TableOfContentService([mockApiService.Object]);

        // Act
        var apiInfo = await tableOfContentService.GetNavigationInfoAsync("/api/reference");

        // Assert
        Assert.NotNull(apiInfo);
        Assert.Equal("API", apiInfo.SectionName);
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

        var tableOfContentService = new TableOfContentService([mockFaqService.Object]);

        // Act
        var faqInfo = await tableOfContentService.GetNavigationInfoAsync("/faq/common");

        // Assert
        Assert.NotNull(faqInfo);
        Assert.Equal("FAQ", faqInfo.SectionName);
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

        var tableOfContentService = new TableOfContentService([mockContentService.Object]);

        // Act
        var navigationInfo = await tableOfContentService.GetNavigationInfoAsync("/cli/how-to/working-with-multiple-command-hierarchies");

        // Assert - Should not have duplicate "CLI" in breadcrumbs
        Assert.NotNull(navigationInfo);
        Assert.Equal(4, navigationInfo.Breadcrumbs.Count);
        
        Assert.Equal("Home", navigationInfo.Breadcrumbs[0].Name);
        Assert.Equal("CLI", navigationInfo.Breadcrumbs[1].Name); // Section breadcrumb
        Assert.Equal("How To", navigationInfo.Breadcrumbs[2].Name); // Formatted from "How-To"
        Assert.Equal("Working with Multiple Command Hierarchies", navigationInfo.Breadcrumbs[3].Name);
        
        // The URL for "How To" should be correct
        Assert.Equal("/cli/how-to", navigationInfo.Breadcrumbs[2].Href);
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

        var tableOfContentService = new TableOfContentService([mockContentService.Object]);

        // Act - First page
        var firstPageInfo = await tableOfContentService.GetNavigationInfoAsync("/docs/intro");
        
        // Assert - First page has no previous
        Assert.NotNull(firstPageInfo);
        Assert.Null(firstPageInfo.PreviousPage);
        Assert.NotNull(firstPageInfo.NextPage);
        Assert.Equal("Getting Started", firstPageInfo.NextPage.Name);
        
        // Act - Last page
        var lastPageInfo = await tableOfContentService.GetNavigationInfoAsync("/docs/advanced");
        
        // Assert - Last page has no next
        Assert.NotNull(lastPageInfo);
        Assert.NotNull(lastPageInfo.PreviousPage);
        Assert.Equal("Getting Started", lastPageInfo.PreviousPage.Name);
        Assert.Null(lastPageInfo.NextPage);
    }
}