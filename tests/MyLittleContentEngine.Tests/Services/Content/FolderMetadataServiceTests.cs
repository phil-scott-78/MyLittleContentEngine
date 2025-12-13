using System.IO.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Infrastructure;
using Shouldly;
using Testably.Abstractions.Testing;

namespace MyLittleContentEngine.Tests.Services.Content;

public class FolderMetadataServiceTests
{
    private static FolderMetadataService CreateService(
        MockFileSystem fileSystem,
        params (string contentPath, string basePageUrl)[] contentOptions)
    {
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
        var logger = NullLogger<FolderMetadataService>.Instance;

        return new FolderMetadataService(
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

    [Fact]
    public async Task GetFolderMetadata_WithBasePageUrl_ReturnsCachedMetadataWithCorrectKey()
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
        var contentPath = fileSystem.Path.GetFullPath("Content/console");

        CreateMetadataFile(
            fileSystem,
            fileSystem.Path.Combine(contentPath, "how-to/_index.metadata.yml"),
            "How-To Guides",
            10);

        var service = CreateService(fileSystem, (contentPath, "/console"));

        // Act
        var result = await service.GetFolderMetadata("console/how-to");

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe("How-To Guides");
        result.Order.ShouldBe(10);
    }

    [Fact]
    public async Task GetFolderMetadata_WithEmptyBasePageUrl_UsesRelativePathOnly()
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
        var contentPath = fileSystem.Path.GetFullPath("Content");

        CreateMetadataFile(
            fileSystem,
            fileSystem.Path.Combine(contentPath, "docs/_index.metadata.yml"),
            "Documentation",
            5);

        var service = CreateService(fileSystem, (contentPath, ""));

        // Act
        var result = await service.GetFolderMetadata("docs");

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe("Documentation");
        result.Order.ShouldBe(5);
    }

    [Fact]
    public async Task GetFolderMetadata_MultipleServicesWithSameFolderNames_DifferentiatesByBasePageUrl()
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
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

        var service = CreateService(
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

    [Fact]
    public async Task GetFolderMetadata_WithMultiSegmentBasePageUrl_CreatesCorrectCacheKey()
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
        var contentPath = fileSystem.Path.GetFullPath("Content/docs/api");

        CreateMetadataFile(
            fileSystem,
            fileSystem.Path.Combine(contentPath, "reference/_index.metadata.yml"),
            "API Reference",
            15);

        var service = CreateService(fileSystem, (contentPath, "/docs/api"));

        // Act
        var result = await service.GetFolderMetadata("docs/api/reference");

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe("API Reference");
        result.Order.ShouldBe(15);
    }

    [Fact]
    public async Task GetFolderMetadata_WithNestedFolders_WorksCorrectly()
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
        var contentPath = fileSystem.Path.GetFullPath("Content/console");

        CreateMetadataFile(
            fileSystem,
            fileSystem.Path.Combine(contentPath, "how-to/advanced/_index.metadata.yml"),
            "Advanced Guides",
            25);

        var service = CreateService(fileSystem, (contentPath, "/console"));

        // Act
        var result = await service.GetFolderMetadata("console/how-to/advanced");

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe("Advanced Guides");
        result.Order.ShouldBe(25);
    }

    [Fact]
    public async Task GetFolderMetadata_CaseInsensitiveLookup_ReturnsMetadata()
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
        var contentPath = fileSystem.Path.GetFullPath("Content/console");

        CreateMetadataFile(
            fileSystem,
            fileSystem.Path.Combine(contentPath, "how-to/_index.metadata.yml"),
            "How-To Guides",
            10);

        var service = CreateService(fileSystem, (contentPath, "/console"));

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

    [Fact]
    public async Task GetFolderMetadata_NonExistentFolder_ReturnsNull()
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
        var contentPath = fileSystem.Path.GetFullPath("Content/console");
        fileSystem.Directory.CreateDirectory(contentPath);

        var service = CreateService(fileSystem, (contentPath, "/console"));

        // Act
        var result = await service.GetFolderMetadata("console/non-existent");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetFolderMetadata_EmptyMetadataFile_ReturnsNull()
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
        var contentPath = fileSystem.Path.GetFullPath("Content/console");
        var metadataPath = fileSystem.Path.Combine(contentPath, "how-to/_index.metadata.yml");

        fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(metadataPath)!);
        fileSystem.File.WriteAllText(metadataPath, "");

        var service = CreateService(fileSystem, (contentPath, "/console"));

        // Act
        var result = await service.GetFolderMetadata("console/how-to");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetFolderMetadata_MalformedYaml_ReturnsNull()
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
        var contentPath = fileSystem.Path.GetFullPath("Content/console");
        var metadataPath = fileSystem.Path.Combine(contentPath, "how-to/_index.metadata.yml");

        fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(metadataPath)!);
        fileSystem.File.WriteAllText(metadataPath, "invalid: yaml: content: [");

        var service = CreateService(fileSystem, (contentPath, "/console"));

        // Act
        var result = await service.GetFolderMetadata("console/how-to");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetFolderMetadata_SkipsBuildAndNodeModulesDirectories()
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
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

        var service = CreateService(fileSystem, (contentPath, "/console"));

        // Act
        var binResult = await service.GetFolderMetadata("console/bin");
        var objResult = await service.GetFolderMetadata("console/obj");
        var nodeModulesResult = await service.GetFolderMetadata("console/node_modules");

        // Assert
        binResult.ShouldBeNull();
        objResult.ShouldBeNull();
        nodeModulesResult.ShouldBeNull();
    }

    [Fact]
    public async Task GetFolderMetadata_WithNonExistentContentRoot_HandlesGracefully()
    {
        // Arrange
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
        var contentPath = fileSystem.Path.GetFullPath("Content/NonExistent");
        // Don't create the directory

        var service = CreateService(fileSystem, (contentPath, "/console"));

        // Act
        var result = await service.GetFolderMetadata("console/how-to");

        // Assert
        result.ShouldBeNull();
    }
}
