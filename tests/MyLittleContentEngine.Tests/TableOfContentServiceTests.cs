using System.Collections.Immutable;
using Moq;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;
using Shouldly;

namespace MyLittleContentEngine.Tests;

public class TableOfContentServiceTests
{
    private readonly ContentEngineOptions _options = new()
    { 
        BaseUrl = "https://example.com",
        SiteTitle = "Test Blog",
        SiteDescription = "Test Description"
    };

    private static Mock<IContentService> CreateMockContentService(params (string title, string url, int order)[] pages)
    {
        var mockContentService = new Mock<IContentService>();
        var pagesToGenerate = pages.Select(p => new PageToGenerate(
            p.url, 
            p.url, 
            new Metadata { Title = p.title, Order = p.order })).ToImmutableList();

        mockContentService.Setup(x => x.GetPagesToGenerateAsync())
            .ReturnsAsync(pagesToGenerate);

        return mockContentService;
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithEmptyContentServices_ReturnsEmptyList()
    {
        // Arrange
        var service = new TableOfContentService(_options, []);

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Result
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithSinglePage_ReturnsCorrectEntry()
    {
        // Arrange
        var mockContentService = CreateMockContentService(("Home", "index", 1));
        var contentServices = new List<IContentService> { mockContentService.Object };
        var service = new TableOfContentService(_options, contentServices);

        // Act
        var result = await service.GetNavigationTocAsync("https://example.com/index");

        // Assert
        result.ShouldHaveSingleItem();
        var entry = result.First();
        entry.Name.ShouldBe("Home");
        entry.Href.ShouldBe("https://example.com/index");
        entry.Order.ShouldBe(1);
        entry.IsSelected.ShouldBeTrue();
        entry.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithMultiplePages_SortsCorrectlyByOrder()
    {
        // Arrange
        var mockContentService = CreateMockContentService(
            ("Third", "third", 3),
            ("First", "first", 1),
            ("Second", "second", 2)
        );
        var contentServices = new List<IContentService> { mockContentService.Object };
        var service = new TableOfContentService(_options, contentServices);

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.Count.ShouldBe(3),
            () => result[0].Name.ShouldBe("First"),
            () => result[0].Order.ShouldBe(1),
            () => result[1].Name.ShouldBe("Second"),
            () => result[1].Order.ShouldBe(2),
            () => result[2].Name.ShouldBe("Third"),
            () => result[2].Order.ShouldBe(3)
        );
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithNestedStructure_CreatesHierarchy()
    {
        // Arrange
        var mockContentService = CreateMockContentService(
            ("Home", "index", 1),
            ("About", "about/index", 2),
            ("Team", "about/team", 3),
            ("Contact", "contact", 4)
        );
        var contentServices = new List<IContentService> { mockContentService.Object };
        var service = new TableOfContentService(_options, contentServices);

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.Count.ShouldBe(3); // Home, About (with Team as child), Contact

        var aboutEntry = result.First(e => e.Name == "About");
        aboutEntry.Href.ShouldBe("https://example.com/about/index");
        aboutEntry.Items.ShouldHaveSingleItem();
        aboutEntry.Items[0].Name.ShouldBe("Team");
        aboutEntry.Items[0].Href.ShouldBe("https://example.com/about/team");
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithFolderWithoutIndex_CreatesFolderEntry()
    {
        // Arrange
        var mockContentService = CreateMockContentService(
            ("Team", "about/team", 1),
            ("History", "about/history", 2)
        );
        var contentServices = new List<IContentService> { mockContentService.Object };
        var service = new TableOfContentService(_options, contentServices);

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.ShouldHaveSingleItem();
        var aboutEntry = result.First();
        aboutEntry.Name.ShouldBe("About"); // Folder name from a segment
        aboutEntry.Href.ShouldBeNull(); // No href for the folder without an index
        aboutEntry.Items.Length.ShouldBe(2);
        aboutEntry.Items[0].Name.ShouldBe("Team");
        aboutEntry.Items[1].Name.ShouldBe("History");
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithCurrentUrlSelection_MarksCorrectEntryAsSelected()
    {
        // Arrange
        var mockContentService = CreateMockContentService(
            ("Home", "index", 1),
            ("About", "about", 2),
            ("Contact", "contact", 3)
        );
        var contentServices = new List<IContentService> { mockContentService.Object };
        var service = new TableOfContentService(_options, contentServices);

        // Act
        var result = await service.GetNavigationTocAsync("https://example.com/about");

        // Assert
        result.First(e => e.Name == "Home").IsSelected.ShouldBeFalse();
        result.First(e => e.Name == "About").IsSelected.ShouldBeTrue();
        result.First(e => e.Name == "Contact").IsSelected.ShouldBeFalse();
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithChildSelected_MarksParentAsSelected()
    {
        // Arrange
        var mockContentService = CreateMockContentService(
            ("About", "about/index", 1),
            ("Team", "about/team", 2),
            ("History", "about/history", 3)
        );
        var contentServices = new List<IContentService> { mockContentService.Object };
        var service = new TableOfContentService(_options, contentServices);

        // Act
        var result = await service.GetNavigationTocAsync("https://example.com/about/team");

        // Assert
        var aboutEntry = result.First(e => e.Name == "About");
        aboutEntry.IsSelected.ShouldBeTrue(); // Parent should be selected because a child is selected
        aboutEntry.Items.First(e => e.Name == "Team").IsSelected.ShouldBeTrue();
        aboutEntry.Items.First(e => e.Name == "History").IsSelected.ShouldBeFalse();
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithPagesWithoutTitle_SkipsPages()
    {
        // Arrange
        var mockContentService = new Mock<IContentService>();
        var pages = new List<PageToGenerate>
        {
            new("index", "index", new Metadata { Title = "Home", Order = 1 }),
            new("no-title", "no-title", new Metadata { Title = null, Order = 2 }),
            new("about", "about", new Metadata { Title = "About", Order = 3 })
        };
        mockContentService.Setup(x => x.GetPagesToGenerateAsync()).ReturnsAsync(pages.ToImmutableList());

        var contentServices = new List<IContentService> { mockContentService.Object };
        var service = new TableOfContentService(_options, contentServices);

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.Count.ShouldBe(2); // Only pages with titles
        result.Any(e => e.Href?.Contains("no-title") == true).ShouldBeFalse();
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithMultipleContentServices_CombinesAllPages()
    {
        // Arrange
        var mockContentService1 = CreateMockContentService(("Home", "index", 1));
        var mockContentService2 = CreateMockContentService(("About", "about", 2));
        var contentServices = new List<IContentService> { mockContentService1.Object, mockContentService2.Object };
        var service = new TableOfContentService(_options, contentServices);

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.Count.ShouldBe(2);
        result.Any(e => e.Name == "Home").ShouldBeTrue();
        result.Any(e => e.Name == "About").ShouldBeTrue();
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithIndexAndNonIndexPages_HandlesIndexCorrectly()
    {
        // Arrange
        var mockContentService = CreateMockContentService(
            ("Documentation", "docs/index", 1),
            ("Getting Started", "docs/getting-started", 2),
            ("API Reference", "docs/api", 3)
        );
        var contentServices = new List<IContentService> { mockContentService.Object };
        var service = new TableOfContentService(_options, contentServices);

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.ShouldHaveSingleItem();
        var docsEntry = result.First();
        docsEntry.Name.ShouldBe("Documentation"); // From the index page title
        docsEntry.Href.ShouldBe("https://example.com/docs/index"); // From index page URL
        docsEntry.Items.Length.ShouldBe(2); // Non-index pages in the folder
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithDifferentUrlFormats_NormalizesCorrectly()
    {
        // Arrange
        var mockContentService = CreateMockContentService(("Home", "index", 1));
        var contentServices = new List<IContentService> { mockContentService.Object };
        var service = new TableOfContentService(_options, contentServices);

        // Act - Test different URL formats that should all match the index page
        var resultWithSlash = await service.GetNavigationTocAsync("https://example.com/");
        var resultWithIndex = await service.GetNavigationTocAsync("https://example.com/index");
        var resultWithExactMatch = await service.GetNavigationTocAsync("https://example.com/index");

        // Assert - All should mark the home page as selected
        resultWithSlash.First().IsSelected.ShouldBeTrue();
        resultWithIndex.First().IsSelected.ShouldBeTrue();
        resultWithExactMatch.First().IsSelected.ShouldBeTrue();
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithComplexHierarchy_BuildsCorrectStructure()
    {
        // Arrange
        var mockContentService = CreateMockContentService(
            ("Home", "index", 1),
            ("Documentation", "docs/index", 10),
            ("Getting Started", "docs/getting-started", 11),
            ("Configuration", "docs/config/index", 20),
            ("Basic Config", "docs/config/basic", 21),
            ("Advanced Config", "docs/config/advanced", 22),
            ("API", "api", 30)
        );
        var contentServices = new List<IContentService> { mockContentService.Object };
        var service = new TableOfContentService(_options, contentServices);

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.Count.ShouldBe(3); // Home, Documentation, API

        var docsEntry = result.First(e => e.Name == "Documentation");
        docsEntry.Items.Length.ShouldBe(2); // Getting Started, Configuration

        var configEntry = docsEntry.Items.First(e => e.Name == "Configuration");
        configEntry.Items.Length.ShouldBe(2); // Basic Config, Advanced Config
        configEntry.Items[0].Name.ShouldBe("Basic Config");
        configEntry.Items[1].Name.ShouldBe("Advanced Config");
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithFolderNameWithDashes_ConvertsTitleCorrectly()
    {
        // Arrange
        var mockContentService = CreateMockContentService(
            ("Getting Started", "getting-started/page1", 1),
            ("API Reference", "api--reference/page2", 2)
        );
        var contentServices = new List<IContentService> { mockContentService.Object };
        var service = new TableOfContentService(_options, contentServices);

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.Count.ShouldBe(2);
        result.First().Name.ShouldBe("Getting Started"); // Single dash converted to space
        result.Last().Name.ShouldBe("Api-Reference"); // Double dash preserved as single dash, title case applied
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithDeepNesting_HandlesMultipleLevels()
    {
        // Arrange
        var mockContentService = CreateMockContentService(
            ("Level 1", "level1/index", 1),
            ("Level 2", "level1/level2/index", 2),
            ("Level 3", "level1/level2/level3/page", 3)
        );
        var contentServices = new List<IContentService> { mockContentService.Object };
        var service = new TableOfContentService(_options, contentServices);

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.ShouldHaveSingleItem();
        var level1 = result.First();
        level1.Name.ShouldBe("Level 1");
        level1.Items.ShouldHaveSingleItem();

        var level2 = level1.Items.First();
        level2.Name.ShouldBe("Level 2");
        level2.Items.ShouldHaveSingleItem();

        var level3 = level2.Items.First();
        level3.Name.ShouldBe("Level3");
        level3.Items.Length.ShouldBe(1); // The actual page is a child
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithZeroOrderValue_HandlesCorrectly()
    {
        // Arrange
        var mockContentService = CreateMockContentService(
            ("First", "first", 0),
            ("Second", "second", 1),
            ("Third", "third", -1)
        );
        var contentServices = new List<IContentService> { mockContentService.Object };
        var service = new TableOfContentService(_options, contentServices);

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.Count.ShouldBe(3);
        result[0].Name.ShouldBe("Third"); // Order -1
        result[1].Name.ShouldBe("First"); // Order 0
        result[2].Name.ShouldBe("Second"); // Order 1
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithDefaultMaxIntOrder_SortsLast()
    {
        // Arrange - One page with explicit order, one with default (int.MaxValue)
        var mockContentService = new Mock<IContentService>();
        var pages = new List<PageToGenerate>
        {
            new("first", "first", new Metadata { Title = "First", Order = 1 }),
            new("last", "last", new Metadata { Title = "Last" }) // Default Order is int.MaxValue
        };
        mockContentService.Setup(x => x.GetPagesToGenerateAsync()).ReturnsAsync(pages.ToImmutableList());

        var contentServices = new List<IContentService> { mockContentService.Object };
        var service = new TableOfContentService(_options, contentServices);

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("First");
        result[1].Name.ShouldBe("Last");
    }
}