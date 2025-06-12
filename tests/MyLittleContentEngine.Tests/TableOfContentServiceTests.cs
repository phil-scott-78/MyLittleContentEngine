using System.Collections.Immutable;
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

    private class TestContentService2(params (string title, string url, int order)[] pages) : TestContentService(pages);
    
    // Test-specific concrete implementations of IContentService
    private class TestContentService : IContentService
    {
        private readonly ImmutableList<PageToGenerate> _pages;

        public TestContentService(params (string title, string url, int order)[] pages)
        {
            _pages = pages.Select(p => new PageToGenerate(
                p.url,
                p.url,
                new Metadata { Title = p.title, Order = p.order })).ToImmutableList();
        }

        public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync() => Task.FromResult(_pages);
        public Task<ImmutableList<PageToGenerate>> GetTocEntriesToGenerateAsync() => Task.FromResult(_pages);

        public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() => Task.FromResult(ImmutableList<ContentToCopy>.Empty);

        public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() => Task.FromResult(ImmutableList<CrossReference>.Empty);
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
        var contentService = new TestContentService(("Home", "index", 1));
        var contentServices = new List<IContentService> { contentService };
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
        var contentService = new TestContentService(
            ("Third", "third", 3),
            ("First", "first", 1),
            ("Second", "second", 2)
        );
        var contentServices = new List<IContentService> { contentService };
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
        var contentService = new TestContentService(
            ("Home", "index", 1),
            ("About", "about/index", 2),
            ("Team", "about/team", 3),
            ("Contact", "contact", 4)
        );
        var contentServices = new List<IContentService> { contentService };
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
        var contentService = new TestContentService(
            ("Team", "about/team", 1),
            ("History", "about/history", 2)
        );
        var contentServices = new List<IContentService> { contentService };
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
        var contentService = new TestContentService(
            ("Home", "index", 1),
            ("About", "about", 2),
            ("Contact", "contact", 3)
        );
        var contentServices = new List<IContentService> { contentService };
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
        var contentService = new TestContentService(
            ("About", "about/index", 1),
            ("Team", "about/team", 2),
            ("History", "about/history", 3)
        );
        var contentServices = new List<IContentService> { contentService };
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
        var pages = new List<PageToGenerate>
        {
            new("index", "index", new Metadata { Title = "Home", Order = 1 }),
            new("no-title", "no-title", new Metadata { Title = null, Order = 2 }),
            new("about", "about", new Metadata { Title = "About", Order = 3 })
        };

        // Create a custom IContentService that returns the specific pages for this test
        var customContentService = new TestContentServiceWithSpecificPages(pages.ToImmutableList());
        var contentServices = new List<IContentService> { customContentService };
        var service = new TableOfContentService(_options, contentServices);

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.Count.ShouldBe(2); // Only pages with titles
        result.Any(e => e.Href?.Contains("no-title") == true).ShouldBeFalse();
    }

    // Helper class for the above test to provide specific pages
    private class TestContentServiceWithSpecificPages(ImmutableList<PageToGenerate> pages) : IContentService
    {
        public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync() => Task.FromResult(pages);
        public Task<ImmutableList<PageToGenerate>> GetTocEntriesToGenerateAsync() => Task.FromResult(pages);
        public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() => Task.FromResult(ImmutableList<ContentToCopy>.Empty);
        public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() => Task.FromResult(ImmutableList<CrossReference>.Empty);
    }


    [Fact]
    public async Task GetNavigationTocAsync_WithMultipleContentServices_CombinesAllPages()
    {
        // Arrange
        var contentService1 = new TestContentService(("Home", "index", 1));
        var contentService2 = new TestContentService2(("About", "about", 2));
        var contentServices = new List<IContentService> { contentService1, contentService2 };
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
        var contentService = new TestContentService(
            ("Documentation", "docs/index", 1),
            ("Getting Started", "docs/getting-started", 2),
            ("API Reference", "docs/api", 3)
        );
        var contentServices = new List<IContentService> { contentService };
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
        var contentService = new TestContentService(("Home", "index", 1));
        var contentServices = new List<IContentService> { contentService };
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
        var contentService = new TestContentService(
            ("Home", "index", 1),
            ("Documentation", "docs/index", 10),
            ("Getting Started", "docs/getting-started", 11),
            ("Configuration", "docs/config/index", 20),
            ("Basic Config", "docs/config/basic", 21),
            ("Advanced Config", "docs/config/advanced", 22),
            ("API", "api", 30)
        );
        var contentServices = new List<IContentService> { contentService };
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
        var contentService = new TestContentService(
            ("Getting Started", "getting-started/page1", 1),
            ("API Reference", "api--reference/page2", 2)
        );
        var contentServices = new List<IContentService> { contentService };
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
        var contentService = new TestContentService(
            ("Level 1", "level1/index", 1),
            ("Level 2", "level1/level2/index", 2),
            ("Level 3", "level1/level2/level3/page", 3)
        );
        var contentServices = new List<IContentService> { contentService };
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
        var contentService = new TestContentService(
            ("First", "first", 0),
            ("Second", "second", 1),
            ("Third", "third", -1)
        );
        var contentServices = new List<IContentService> { contentService };
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
        var pages = new List<PageToGenerate>
        {
            new("first", "first", new Metadata { Title = "First", Order = 1 }),
            new("last", "last", new Metadata { Title = "Last" }) // Default Order is int.MaxValue
        };
        
        var customContentService = new TestContentServiceWithSpecificPages(pages.ToImmutableList());
        var contentServices = new List<IContentService> { customContentService };
        var service = new TableOfContentService(_options, contentServices);

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("First");
        result[1].Name.ShouldBe("Last");
    }
}