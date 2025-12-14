using System.Collections.Immutable;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;
using MyLittleContentEngine.Services.Infrastructure;
using MyLittleContentEngine.Tests.TestHelpers;
using Shouldly;
using Testably.Abstractions.Testing;

namespace MyLittleContentEngine.Tests;

public class TableOfContentServiceTests
{
    private class TestContentService2(params (string title, string url, int order)[] pages) : TestContentService(pages);

    private class WebContentService : IContentService
    {
        private readonly ImmutableList<PageToGenerate> _pages;

        public WebContentService(params (string title, string url, int order)[] pages)
        {
            _pages = pages.Select(p => new PageToGenerate(
                p.url,
                p.url,
                new Metadata { Title = p.title, Order = p.order, Section = "Web" })).ToImmutableList();
        }

        public int SearchPriority { get; } = 0;
        public string DefaultSection => "Web";
        public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync() => Task.FromResult(_pages);
        public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() => Task.FromResult(_pages.Where(p => p.Metadata?.Title != null).Select(p => new ContentTocItem(p.Metadata!.Title!, p.Url, p.Metadata.Order, p.Url.GetSegments(), p.Metadata.Section)).ToImmutableList());
        public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() => Task.FromResult(ImmutableList<ContentToCopy>.Empty);
        public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() => Task.FromResult(ImmutableList<CrossReference>.Empty);
    }

    private class ConsoleContentService : IContentService
    {
        private readonly ImmutableList<PageToGenerate> _pages;

        public ConsoleContentService(params (string title, string url, int order)[] pages)
        {
            _pages = pages.Select(p => new PageToGenerate(
                p.url,
                p.url,
                new Metadata { Title = p.title, Order = p.order, Section = "Console" })).ToImmutableList();
        }

        public int SearchPriority { get; } = 0;
        public string DefaultSection => "Console";
        public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync() => Task.FromResult(_pages);
        public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() => Task.FromResult(_pages.Where(p => p.Metadata?.Title != null).Select(p => new ContentTocItem(p.Metadata!.Title!, p.Url, p.Metadata.Order, p.Url.GetSegments(), p.Metadata.Section)).ToImmutableList());
        public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() => Task.FromResult(ImmutableList<ContentToCopy>.Empty);
        public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() => Task.FromResult(ImmutableList<CrossReference>.Empty);
    }

    private class TestContentServiceWithItemSection : IContentService
    {
        public int SearchPriority { get; } = 0;
        public string DefaultSection => "Console"; // Service default is Console

        public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync() => 
            Task.FromResult(ImmutableList.Create(
                new PageToGenerate("/service-default", "/service-default", new Metadata { Title = "Service Default Item", Order = 1 }),
                new PageToGenerate("/override-item", "/override-item", new Metadata { Title = "Override Item", Order = 2, Section = "Web" })
            ));

        public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() => 
            Task.FromResult(ImmutableList.Create(
                new ContentTocItem("Service Default Item", "/service-default", 1, ["service-default"]), // Uses service default section
                new ContentTocItem("Override Item", "/override-item", 2, ["override-item"], "Web") // Overrides to Web section
            ));

        public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() => Task.FromResult(ImmutableList<ContentToCopy>.Empty);
        public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() => Task.FromResult(ImmutableList<CrossReference>.Empty);
    }

    private class WebContentService2 : IContentService
    {
        private readonly ImmutableList<PageToGenerate> _pages;

        public WebContentService2(params (string title, string url, int order)[] pages)
        {
            _pages = pages.Select(p => new PageToGenerate(
                p.url,
                p.url,
                new Metadata { Title = p.title, Order = p.order, Section = "Web" })).ToImmutableList();
        }

        public int SearchPriority { get; } = 0;
        public string DefaultSection => "Web";
        public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync() => Task.FromResult(_pages);
        public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() => Task.FromResult(_pages.Where(p => p.Metadata?.Title != null).Select(p => new ContentTocItem(p.Metadata!.Title!, p.Url, p.Metadata.Order, p.Url.GetSegments(), p.Metadata.Section)).ToImmutableList());
        public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() => Task.FromResult(ImmutableList<ContentToCopy>.Empty);
        public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() => Task.FromResult(ImmutableList<CrossReference>.Empty);
    }
    
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

        public int SearchPriority { get; } = 0;
        public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync() => Task.FromResult(_pages);
        public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() => Task.FromResult(_pages.Where(p => p.Metadata?.Title != null).Select(p => new ContentTocItem(p.Metadata!.Title!, p.Url, p.Metadata.Order, p.Url.GetSegments())).ToImmutableList());

        public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() => Task.FromResult(ImmutableList<ContentToCopy>.Empty);

        public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() => Task.FromResult(ImmutableList<CrossReference>.Empty);
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithEmptyContentServices_ReturnsEmptyList()
    {
        // Arrange
        var service = ServiceMockFactory.CreateTableOfContentService();

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
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

        // Act
        var result = await service.GetNavigationTocAsync("/index");

        // Assert
        result.ShouldHaveSingleItem();
        var entry = result.First();
        entry.Name.ShouldBe("Home");
        entry.Href.ShouldBe("/index");
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
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

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
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.Count.ShouldBe(3); // Home, About (with Team as child), Contact

        var aboutEntry = result.First(e => e.Name == "About");
        aboutEntry.Href.ShouldBe("/about/index");
        aboutEntry.Items.ShouldHaveSingleItem();
        aboutEntry.Items[0].Name.ShouldBe("Team");
        aboutEntry.Items[0].Href.ShouldBe("/about/team");
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
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

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
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

        // Act
        var result = await service.GetNavigationTocAsync("/about");

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
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

        // Act
        var result = await service.GetNavigationTocAsync("/about/team");

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
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.Count.ShouldBe(2); // Only pages with titles
        result.Any(e => e.Href?.Contains("no-title") == true).ShouldBeFalse();
    }

    // Helper class for the above test to provide specific pages
    private class TestContentServiceWithSpecificPages(ImmutableList<PageToGenerate> pages) : IContentService
    {
        public int SearchPriority { get; } = 0;
        public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync() => Task.FromResult(pages);
        public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() => Task.FromResult(pages.Where(p => p.Metadata?.Title != null).Select(p => new ContentTocItem(p.Metadata!.Title!, p.Url, p.Metadata.Order, p.Url.GetSegments())).ToImmutableList());
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
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.Count.ShouldBe(2);
        result.Any(e => e.Name == "Home").ShouldBeTrue();
        result.Any(e => e.Name == "About").ShouldBeTrue();
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithSection_FiltersOnlySpecificSection()
    {
        // Arrange
        var contentService1 = new WebContentService(("Web Home", "/web-home", 1), ("Web Guide", "/web-guide", 2));
        var contentService2 = new ConsoleContentService(("Console Home", "/console-home", 1), ("Console Guide", "/console-guide", 2));
        var contentService3 = new TestContentService(("Global Page", "/global", 1)); // No section
        var contentServices = new List<IContentService> { contentService1, contentService2, contentService3 };
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

        // Act
        var webResult = await service.GetNavigationTocAsync("/current", "Web");
        var consoleResult = await service.GetNavigationTocAsync("/current", "Console");
        var globalResult = await service.GetNavigationTocAsync("/current", "");

        // Assert
        webResult.Count.ShouldBe(2);
        webResult.Any(e => e.Name == "Web Home").ShouldBeTrue();
        webResult.Any(e => e.Name == "Web Guide").ShouldBeTrue();
        webResult.Any(e => e.Name == "Console Home").ShouldBeFalse();

        consoleResult.Count.ShouldBe(2);  
        consoleResult.Any(e => e.Name == "Console Home").ShouldBeTrue();
        consoleResult.Any(e => e.Name == "Console Guide").ShouldBeTrue();
        consoleResult.Any(e => e.Name == "Web Home").ShouldBeFalse();

        globalResult.Count.ShouldBe(1);
        globalResult.Any(e => e.Name == "Global Page").ShouldBeTrue();
        globalResult.Any(e => e.Name == "Web Home").ShouldBeFalse();
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithSection_IncludesItemsWithOverrideSection()
    {
        // Arrange - Test item-level section override
        var contentService = new TestContentServiceWithItemSection();
        var service = ServiceMockFactory.CreateTableOfContentService(contentService);

        // Act
        var webResult = await service.GetNavigationTocAsync("/current", "Web");
        var consoleResult = await service.GetNavigationTocAsync("/current", "Console");
        
        // Assert
        webResult.Count.ShouldBe(1);
        webResult.Any(e => e.Name == "Override Item").ShouldBeTrue();
        
        consoleResult.Count.ShouldBe(1);
        consoleResult.Any(e => e.Name == "Service Default Item").ShouldBeTrue();
    }

    [Fact]
    public async Task GetNextPreviousAsync_WithSection_FiltersOnlySpecificSection()
    {
        // Arrange
        var contentService1 = new WebContentService(("Web Page 1", "/web/page1", 1), ("Web Page 2", "/web/page2", 2));
        var contentService2 = new ConsoleContentService(("Console Page 1", "/console/page1", 1), ("Console Page 2", "/console/page2", 2));
        var contentServices = new List<IContentService> { contentService1, contentService2 };
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

        // Act
        var webResult = await service.GetNextPreviousAsync("/web/page1", "Web");
        var consoleResult = await service.GetNextPreviousAsync("/console/page1", "Console");

        // Assert
        webResult.Previous.ShouldBeNull();
        webResult.Next?.Name.ShouldBe("Web Page 2");
        webResult.Next?.Href.ShouldBe("/web/page2");

        consoleResult.Previous.ShouldBeNull();
        consoleResult.Next?.Name.ShouldBe("Console Page 2");
        consoleResult.Next?.Href.ShouldBe("/console/page2");
    }

    [Fact]
    public async Task GetSectionsAsync_ReturnsAllDefinedSections()
    {
        // Arrange
        var contentService1 = new WebContentService(("Web Home", "/web-home", 1));
        var contentService2 = new ConsoleContentService(("Console Home", "/console-home", 1));
        var contentService3 = new TestContentService(("Global Page", "/global", 1)); // No section (empty string)
        var contentService4 = new TestContentServiceWithItemSection(); // Mixed sections
        var contentServices = new List<IContentService> { contentService1, contentService2, contentService3, contentService4 };
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

        // Act
        var sections = await service.GetSectionsAsync();

        // Assert
        sections.Count.ShouldBe(3); // "", "Console", "Web" (sorted)
        sections[0].ShouldBe(""); // Empty string (global) comes first when sorted
        sections[1].ShouldBe("Console");
        sections[2].ShouldBe("Web");
    }

    [Fact]
    public async Task GetSectionsAsync_WithNoSections_ReturnsEmptyStringOnly()
    {
        // Arrange
        var contentService = new TestContentService(("Global Page 1", "/global1", 1), ("Global Page 2", "/global2", 2));
        var service = ServiceMockFactory.CreateTableOfContentService(contentService);

        // Act
        var sections = await service.GetSectionsAsync();

        // Assert
        sections.Count.ShouldBe(1);
        sections[0].ShouldBe(""); // Only global section
    }

    [Fact]
    public async Task GetSectionsAsync_WithDuplicateSections_ReturnsUniqueValues()
    {
        // Arrange
        var contentService1 = new WebContentService(("Web Page 1", "/web1", 1), ("Web Page 2", "/web2", 2));
        var contentService2 = new WebContentService2(("Web Page 3", "/web3", 1)); // Same section as service1 but different service type
        var contentServices = new List<IContentService> { contentService1, contentService2 };
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

        // Act
        var sections = await service.GetSectionsAsync();

        // Assert
        sections.Count.ShouldBe(1);
        sections[0].ShouldBe("Web"); // Only unique sections returned
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithIndexAndNonIndexPages_HandlesIndexCorrectly()
    {
        // Arrange
        var contentService = new TestContentService(
            ("Documentation", "/docs/index", 1),
            ("Getting Started", "/docs/getting-started", 2),
            ("API Reference", "/docs/api", 3)
        );
        var contentServices = new List<IContentService> { contentService };
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.ShouldHaveSingleItem();
        var docsEntry = result.First();
        docsEntry.Name.ShouldBe("Documentation"); // From the index page title
        docsEntry.Href.ShouldBe("/docs/index"); // From index page URL
        docsEntry.Items.Length.ShouldBe(2); // Non-index pages in the folder
    }

    [Fact]
    public async Task GetNavigationTocAsync_WithDifferentUrlFormats_NormalizesCorrectly()
    {
        // Arrange
        var contentService = new TestContentService(("Home", "index", 1));
        var contentServices = new List<IContentService> { contentService };
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

        // Act - Test different URL formats that should all match the index page
        var resultWithSlash = await service.GetNavigationTocAsync("/");
        var resultWithIndex = await service.GetNavigationTocAsync("/index");

        // Assert - All should mark the home page as selected
        resultWithSlash.First().IsSelected.ShouldBeTrue();
        resultWithIndex.First().IsSelected.ShouldBeTrue();
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
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

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
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

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
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

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
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

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
        var service = ServiceMockFactory.CreateTableOfContentService(contentServices.ToArray());

        // Act
        var result = await service.GetNavigationTocAsync("/current");

        // Assert
        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("First");
        result[1].Name.ShouldBe("Last");
    }

    [Theory]
    [InlineData(SimulationMode.Windows)]
    [InlineData(SimulationMode.Linux)]
    public async Task GetNavigationTocAsync_WithFolderMetadata_AppliesMetadataToFolderNames(SimulationMode simulationMode)
    {
        // Arrange - Create a content service with BasePageUrl and ContentPath
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(simulationMode));
        // Set the mock file system for FilePath operations - critical for cross-platform path handling
        FilePath.FileSystem = fileSystem;
        var contentPath = fileSystem.Path.GetFullPath("Content/console");

        // Create metadata file for the "how-to" folder
        var metadataPath = fileSystem.Path.Combine(contentPath, "how-to/_index.metadata.yml");
        fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(metadataPath)!);
        fileSystem.File.WriteAllText(metadataPath, """
            title: How-To Guides
            order: 5
            """);

        // Create a test content service that implements both IContentService and IContentOptions
        var testContentService = new TestContentServiceWithOptions(
            contentPath,
            "/console",
            ("Getting Started", "/console/how-to/getting-started", 10),
            ("Advanced Topics", "/console/how-to/advanced", 20)
        );

        // Create TableOfContentService with the file system and content options
        var fileSystemUtilities = new FileSystemUtilities(fileSystem);
        var contentEngineOptions = new ContentEngineOptions
        {
            SiteTitle = "Test Site",
            SiteDescription = "Test Description"
        };
        var logger = NullLogger<TableOfContentService>.Instance;

        // Create TableOfContentService with the content service and folder metadata support
        var tableOfContentService = new TableOfContentService(
            [testContentService],
            [testContentService],
            contentEngineOptions,
            fileSystemUtilities,
            fileSystem,
            logger);

        // Act
        var result = await tableOfContentService.GetNavigationTocAsync("/console/how-to/getting-started");

        // Assert
        result.Count.ShouldBe(1);
        var consoleNode = result[0];
        consoleNode.Name.ShouldBe("Console"); // Folder name
        consoleNode.Items.ShouldNotBeEmpty();
        consoleNode.Items.Count().ShouldBe(1);

        var howToNode = consoleNode.Items.First();
        howToNode.Name.ShouldBe("How-To Guides"); // From metadata file, not "How To" from folder name
        howToNode.Order.ShouldBe(5); // From metadata file
        howToNode.Items.Count().ShouldBe(2);
        howToNode.Items.ElementAt(0).Name.ShouldBe("Getting Started");
        howToNode.Items.ElementAt(1).Name.ShouldBe("Advanced Topics");
    }

    // Folder Metadata Tests (migrated from FolderMetadataServiceTests)

    private static TableOfContentService CreateServiceWithFolderMetadata(
        MockFileSystem fileSystem,
        params (string contentPath, string basePageUrl)[] contentOptions)
    {
        // Set the mock file system for FilePath operations
        // This is critical for cross-platform path handling
        FilePath.FileSystem = fileSystem;

        var contentOptionsMocks = contentOptions.Select(opt =>
        {
            var mock = new Mock<IContentOptions>();
            mock.Setup(x => x.ContentPath).Returns(new FilePath(opt.contentPath));
            mock.Setup(x => x.BasePageUrl).Returns(new UrlPath(opt.basePageUrl));
            return mock.Object;
        }).ToList();

        var fileSystemUtilities = new FileSystemUtilities(fileSystem);
        var contentEngineOptions = new ContentEngineOptions
        {
            SiteTitle = "Test Site",
            SiteDescription = "Test Description"
        };
        var logger = NullLogger<TableOfContentService>.Instance;

        return new TableOfContentService(
            Array.Empty<IContentService>(),
            contentOptionsMocks,
            contentEngineOptions,
            fileSystemUtilities,
            fileSystem,
            logger);
    }

    private static void CreateMetadataFile(MockFileSystem fileSystem, string path, string title, int order)
    {
        var directory = fileSystem.Path.GetDirectoryName(path)!;
        fileSystem.Directory.CreateDirectory(directory);

        var yamlContent = $"""
            title: {title}
            order: {order}
            """;

        fileSystem.File.WriteAllText(path, yamlContent);
    }

    [Theory]
    [InlineData(SimulationMode.Windows)]
    [InlineData(SimulationMode.Linux)]
    public async Task GetFolderMetadata_WithBasePageUrl_ReturnsCachedMetadataWithCorrectKey(SimulationMode simulationMode)
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(simulationMode));
        var contentPath = fileSystem.Path.GetFullPath("Content/console");

        CreateMetadataFile(
            fileSystem,
            fileSystem.Path.Combine(contentPath, "how-to/_index.metadata.yml"),
            "How-To Guides",
            10);

        var service = CreateServiceWithFolderMetadata(fileSystem, (contentPath, "/console"));

        // Act
        var result = await service.GetFolderMetadata("console/how-to");

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe("How-To Guides");
        result.Order.ShouldBe(10);
    }

    [Theory]
    [InlineData(SimulationMode.Windows)]
    [InlineData(SimulationMode.Linux)]
    public async Task GetFolderMetadata_WithEmptyBasePageUrl_UsesRelativePathOnly(SimulationMode simulationMode)
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(simulationMode));
        var contentPath = fileSystem.Path.GetFullPath("Content");

        CreateMetadataFile(
            fileSystem,
            fileSystem.Path.Combine(contentPath, "docs/_index.metadata.yml"),
            "Documentation",
            5);

        var service = CreateServiceWithFolderMetadata(fileSystem, (contentPath, ""));

        // Act
        var result = await service.GetFolderMetadata("docs");

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe("Documentation");
        result.Order.ShouldBe(5);
    }

    [Theory]
    [InlineData(SimulationMode.Windows)]
    [InlineData(SimulationMode.Linux)]
    public async Task GetFolderMetadata_MultipleServicesWithSameFolderNames_DifferentiatesByBasePageUrl(SimulationMode simulationMode)
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(simulationMode));
        var consolePath = fileSystem.Path.GetFullPath("Content/console");
        var cliPath = fileSystem.Path.GetFullPath("Content/cli");

        CreateMetadataFile(
            fileSystem,
            fileSystem.Path.Combine(consolePath, "how-to/_index.metadata.yml"),
            "Console How-To",
            10);

        CreateMetadataFile(
            fileSystem,
            fileSystem.Path.Combine(cliPath, "how-to/_index.metadata.yml"),
            "CLI How-To",
            20);

        var service = CreateServiceWithFolderMetadata(
            fileSystem,
            (consolePath, "/console"),
            (cliPath, "/cli"));

        // Act
        var consoleResult = await service.GetFolderMetadata("console/how-to");
        var cliResult = await service.GetFolderMetadata("cli/how-to");

        // Assert
        consoleResult.ShouldNotBeNull();
        consoleResult.Title.ShouldBe("Console How-To");
        consoleResult.Order.ShouldBe(10);

        cliResult.ShouldNotBeNull();
        cliResult.Title.ShouldBe("CLI How-To");
        cliResult.Order.ShouldBe(20);
    }

    [Theory]
    [InlineData(SimulationMode.Windows)]
    [InlineData(SimulationMode.Linux)]
    public async Task GetFolderMetadata_WithMultiSegmentBasePageUrl_CreatesCorrectCacheKey(SimulationMode simulationMode)
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(simulationMode));
        var contentPath = fileSystem.Path.GetFullPath("Content/docs/api");

        CreateMetadataFile(
            fileSystem,
            fileSystem.Path.Combine(contentPath, "reference/_index.metadata.yml"),
            "API Reference",
            15);

        var service = CreateServiceWithFolderMetadata(fileSystem, (contentPath, "/docs/api"));

        // Act
        var result = await service.GetFolderMetadata("docs/api/reference");

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe("API Reference");
        result.Order.ShouldBe(15);
    }

    [Theory]
    [InlineData(SimulationMode.Windows)]
    [InlineData(SimulationMode.Linux)]
    public async Task GetFolderMetadata_WithNestedFolders_WorksCorrectly(SimulationMode simulationMode)
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(simulationMode));
        var contentPath = fileSystem.Path.GetFullPath("Content/console");

        CreateMetadataFile(
            fileSystem,
            fileSystem.Path.Combine(contentPath, "how-to/advanced/_index.metadata.yml"),
            "Advanced Guides",
            25);

        var service = CreateServiceWithFolderMetadata(fileSystem, (contentPath, "/console"));

        // Act
        var result = await service.GetFolderMetadata("console/how-to/advanced");

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe("Advanced Guides");
        result.Order.ShouldBe(25);
    }

    [Theory]
    [InlineData(SimulationMode.Windows)]
    [InlineData(SimulationMode.Linux)]
    public async Task GetFolderMetadata_CaseInsensitiveLookup_ReturnsMetadata(SimulationMode simulationMode)
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(simulationMode));
        var contentPath = fileSystem.Path.GetFullPath("Content/console");

        CreateMetadataFile(
            fileSystem,
            fileSystem.Path.Combine(contentPath, "how-to/_index.metadata.yml"),
            "How-To Guides",
            10);

        var service = CreateServiceWithFolderMetadata(fileSystem, (contentPath, "/console"));

        // Act
        var lowerCaseResult = await service.GetFolderMetadata("console/how-to");
        var mixedCaseResult = await service.GetFolderMetadata("Console/How-To");
        var upperCaseResult = await service.GetFolderMetadata("CONSOLE/HOW-TO");

        // Assert
        lowerCaseResult.ShouldNotBeNull();
        lowerCaseResult.Title.ShouldBe("How-To Guides");

        mixedCaseResult.ShouldNotBeNull();
        mixedCaseResult.Title.ShouldBe("How-To Guides");

        upperCaseResult.ShouldNotBeNull();
        upperCaseResult.Title.ShouldBe("How-To Guides");
    }

    [Theory]
    [InlineData(SimulationMode.Windows)]
    [InlineData(SimulationMode.Linux)]
    public async Task GetFolderMetadata_NonExistentFolder_ReturnsNull(SimulationMode simulationMode)
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(simulationMode));
        var contentPath = fileSystem.Path.GetFullPath("Content/console");
        fileSystem.Directory.CreateDirectory(contentPath);

        var service = CreateServiceWithFolderMetadata(fileSystem, (contentPath, "/console"));

        // Act
        var result = await service.GetFolderMetadata("console/non-existent");

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(SimulationMode.Windows)]
    [InlineData(SimulationMode.Linux)]
    public async Task GetFolderMetadata_EmptyMetadataFile_ReturnsNull(SimulationMode simulationMode)
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(simulationMode));
        var contentPath = fileSystem.Path.GetFullPath("Content/console");
        var metadataPath = fileSystem.Path.Combine(contentPath, "how-to/_index.metadata.yml");

        fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(metadataPath)!);
        fileSystem.File.WriteAllText(metadataPath, "");

        var service = CreateServiceWithFolderMetadata(fileSystem, (contentPath, "/console"));

        // Act
        var result = await service.GetFolderMetadata("console/how-to");

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(SimulationMode.Windows)]
    [InlineData(SimulationMode.Linux)]
    public async Task GetFolderMetadata_MalformedYaml_ReturnsNull(SimulationMode simulationMode)
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(simulationMode));
        var contentPath = fileSystem.Path.GetFullPath("Content/console");
        var metadataPath = fileSystem.Path.Combine(contentPath, "how-to/_index.metadata.yml");

        fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(metadataPath)!);
        fileSystem.File.WriteAllText(metadataPath, "invalid: yaml: content: [");

        var service = CreateServiceWithFolderMetadata(fileSystem, (contentPath, "/console"));

        // Act
        var result = await service.GetFolderMetadata("console/how-to");

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(SimulationMode.Windows)]
    [InlineData(SimulationMode.Linux)]
    public async Task GetFolderMetadata_SkipsBuildAndNodeModulesDirectories(SimulationMode simulationMode)
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(simulationMode));
        var contentPath = fileSystem.Path.GetFullPath("Content/console");

        // Create metadata in bin, obj, and node_modules folders (should be skipped)
        CreateMetadataFile(
            fileSystem,
            fileSystem.Path.Combine(contentPath, "bin/_index.metadata.yml"),
            "Should Not Be Found",
            1);

        CreateMetadataFile(
            fileSystem,
            fileSystem.Path.Combine(contentPath, "obj/_index.metadata.yml"),
            "Should Not Be Found",
            2);

        CreateMetadataFile(
            fileSystem,
            fileSystem.Path.Combine(contentPath, "node_modules/_index.metadata.yml"),
            "Should Not Be Found",
            3);

        var service = CreateServiceWithFolderMetadata(fileSystem, (contentPath, "/console"));

        // Act
        var binResult = await service.GetFolderMetadata("console/bin");
        var objResult = await service.GetFolderMetadata("console/obj");
        var nodeModulesResult = await service.GetFolderMetadata("console/node_modules");

        // Assert
        binResult.ShouldBeNull();
        objResult.ShouldBeNull();
        nodeModulesResult.ShouldBeNull();
    }

    [Theory]
    [InlineData(SimulationMode.Windows)]
    [InlineData(SimulationMode.Linux)]
    public async Task GetFolderMetadata_WithNonExistentContentRoot_HandlesGracefully(SimulationMode simulationMode)
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(simulationMode));
        var contentPath = fileSystem.Path.GetFullPath("Content/NonExistent");
        // Don't create the directory

        var service = CreateServiceWithFolderMetadata(fileSystem, (contentPath, "/console"));

        // Act
        var result = await service.GetFolderMetadata("console/how-to");

        // Assert
        result.ShouldBeNull();
    }

    private class TestContentServiceWithOptions : IContentService, IContentOptions
    {
        private readonly ImmutableList<PageToGenerate> _pages;

        public TestContentServiceWithOptions(string contentPath, string basePageUrl, params (string title, string url, int order)[] pages)
        {
            ContentPath = new FilePath(contentPath);
            BasePageUrl = new UrlPath(basePageUrl);
            _pages = pages.Select(p => new PageToGenerate(
                p.url,
                p.url,
                new Metadata { Title = p.title, Order = p.order })).ToImmutableList();
        }

        // IContentService implementation
        public int SearchPriority { get; } = 0;
        public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync() => Task.FromResult(_pages);
        public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() =>
            Task.FromResult(_pages.Where(p => p.Metadata?.Title != null)
                .Select(p => new ContentTocItem(p.Metadata!.Title!, p.Url, p.Metadata.Order, p.Url.GetSegments()))
                .ToImmutableList());
        public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() => Task.FromResult(ImmutableList<ContentToCopy>.Empty);
        public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() => Task.FromResult(ImmutableList<CrossReference>.Empty);

        // IContentOptions implementation
        public FilePath ContentPath { get; init; }

        public UrlPath BasePageUrl { get; init; }
    }
}