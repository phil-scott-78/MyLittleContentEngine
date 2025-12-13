using Shouldly;
using Testably.Abstractions.Testing;

namespace MyLittleContentEngine.Tests.Services.Content.TableOfContents;

/// <summary>
/// Diagnostic tests to understand how MockFileSystem behaves on different host operating systems.
/// These tests help identify platform-specific path handling that might cause cross-platform test failures.
/// Results will be visible in GitHub Actions test output through assertion messages.
/// </summary>
public class MockFileSystemBehaviorTests
{
    /// <summary>
    /// Comprehensive test that simulates the exact flow used in TableOfContentService.
    /// This shows the complete path transformation from test setup through cache key generation.
    /// This is the CRITICAL test - if it fails, the error message will show what went wrong.
    /// </summary>
    [Theory]
    [InlineData(SimulationMode.Windows)]
    [InlineData(SimulationMode.Linux)]
    public void MockFileSystem_SimulateTableOfContentServiceFlow_ProducesCorrectCacheKey(SimulationMode mode)
    {
        // Arrange
        var fileSystem = new MockFileSystem(options =>
            options.SimulatingOperatingSystem(mode));

        // Step 1: Test setup - this is what the test does
        var contentPath = fileSystem.Path.GetFullPath("Content/console");

        // Step 2: Create subdirectory path
        var howToPath = fileSystem.Path.Combine(contentPath, "how-to");

        // Step 3: GetRelativePath - this is what happens in DiscoverMetadataInDirectoryAsync
        var relativePath = fileSystem.Path.GetRelativePath(contentPath, howToPath);

        // Step 4: Simulate combining with baseSegment (from ExtractBaseSegment)
        var baseSegment = "console"; // This always uses forward slashes (from UrlPath)
        var cacheKeyBeforeNormalization = $"{baseSegment}/{relativePath}";

        // Step 5: Normalize (this is what NormalizeFolderPath does)
        var normalized = cacheKeyBeforeNormalization
            .Replace('\\', '/')
            .Trim('/')
            .ToLowerInvariant();

        // Assert: The cache key must be correct regardless of simulation mode
        normalized.ShouldBe("console/how-to",
            $"Cache key mismatch with {mode} simulation! " +
            $"contentPath='{contentPath}', " +
            $"relativePath='{relativePath}', " +
            $"beforeNorm='{cacheKeyBeforeNormalization}', " +
            $"afterNorm='{normalized}'");
    }

    /// <summary>
    /// Tests whether Path.GetRelativePath behavior is consistent across simulation modes.
    /// </summary>
    [Theory]
    [InlineData(SimulationMode.Windows)]
    [InlineData(SimulationMode.Linux)]
    public void MockFileSystem_GetRelativePath_ReturnsConsistentFormat(SimulationMode mode)
    {
        // Arrange
        var fileSystem = new MockFileSystem(options =>
            options.SimulatingOperatingSystem(mode));

        var basePath = fileSystem.Path.GetFullPath("Content/console");
        var nestedPath = fileSystem.Path.Combine(basePath, "how-to", "advanced");

        // Act
        var relativePath = fileSystem.Path.GetRelativePath(basePath, nestedPath);

        // Normalize the result
        var normalized = relativePath.Replace('\\', '/');

        // Assert: After normalization, should always be "how-to/advanced"
        normalized.ShouldBe("how-to/advanced",
            $"GetRelativePath inconsistency with {mode} simulation! " +
            $"basePath='{basePath}', " +
            $"nestedPath='{nestedPath}', " +
            $"relativePath='{relativePath}', " +
            $"normalized='{normalized}'");
    }

    /// <summary>
    /// Tests what GetFullPath returns with different simulation modes.
    /// This helps understand if the simulation mode truly controls path format.
    /// </summary>
    [Theory]
    [InlineData(SimulationMode.Windows)]
    [InlineData(SimulationMode.Linux)]
    public void MockFileSystem_GetFullPath_ReturnsNonEmptyPath(SimulationMode mode)
    {
        // Arrange
        var fileSystem = new MockFileSystem(options =>
            options.SimulatingOperatingSystem(mode));

        // Act
        var result = fileSystem.Path.GetFullPath("Content/console");

        // Assert
        result.ShouldNotBeNullOrEmpty($"GetFullPath returned empty/null with {mode} simulation");

        // Additional assertion: path should be rooted
        fileSystem.Path.IsPathRooted(result).ShouldBeTrue(
            $"GetFullPath did not return rooted path with {mode} simulation. Got: '{result}'");
    }

    /// <summary>
    /// Tests that Path.Combine produces consistent results that can be normalized.
    /// </summary>
    [Theory]
    [InlineData(SimulationMode.Windows)]
    [InlineData(SimulationMode.Linux)]
    public void MockFileSystem_PathCombine_ProducesNormalizablePaths(SimulationMode mode)
    {
        // Arrange
        var fileSystem = new MockFileSystem(options =>
            options.SimulatingOperatingSystem(mode));

        // Act
        var combined = fileSystem.Path.Combine("console", "how-to", "advanced");

        // Normalize
        var normalized = combined.Replace('\\', '/');

        // Assert
        normalized.ShouldBe("console/how-to/advanced",
            $"Path.Combine inconsistency with {mode} simulation. " +
            $"combined='{combined}', normalized='{normalized}'");
    }
}
