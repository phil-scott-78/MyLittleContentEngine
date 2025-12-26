using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;

namespace MyLittleContentEngine.Services.Generation;

/// <summary>
///     Service responsible for generating static HTML pages from a Blazor application.
///     This enables server-side rendered Blazor applications to be deployed as static websites.
/// </summary>
/// <param name="environment">The web hosting environment providing access to web root files</param>
/// <param name="contentServiceCollection">Collection of content services picroviding pages to generate and content to copy</param>
/// <param name="routeHelper">Service for discovering configured ASP.NET routes.</param>
/// <param name="options">Configuration options for the static generation process</param>
/// <param name="outputOptions">Output configuration options including folder path</param>
/// <param name="serviceProvider">Service provider for accessing registered content options</param>
/// <param name="linkVerificationService">Service for verifying internal links in generated HTML</param>
/// <param name="logger">Logger for diagnostic output</param>
internal class OutputGenerationService(
    IWebHostEnvironment environment,
    IEnumerable<IContentService> contentServiceCollection,
    IFileSystem fileSystem,
    RoutesHelperService routeHelper,
    ContentEngineOptions options,
    OutputOptions outputOptions,
    IServiceProvider serviceProvider,
    LinkVerificationService linkVerificationService,
    ILogger<OutputGenerationService> logger)
{
    private readonly IFileSystem _fileSystem = fileSystem;

    /// <summary>
    /// Generates static HTML pages for the Blazor application.
    /// </summary>
    /// <param name="appUrl">The base URL of the running Blazor application, used for making HTTP requests to fetch page content</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="InvalidOperationException">Thrown when no pages are available to generate</exception>
    /// <remarks>
    /// <para>
    /// This method performs several key operations to generate static HTML files from a running Blazor application.
    /// </para>
    /// <list type="number">
    ///     <item>
    ///         <description>Collects pages to generate from all registered content services (including RazorPageContentService)</description>
    ///     </item>
    ///     <item>
    ///         <description>Adds routes registered via MapGet based on configuration</description>
    ///     </item>
    ///     <item>
    ///         <description>Clears and recreates the output directory</description>
    ///     </item>
    ///     <item>
    ///         <description>Generates a sitemap.xml file if configured</description>
    ///     </item>
    ///     <item>
    ///         <description>Copies static content (wwwroot files, etc.) to the output directory</description>
    ///     </item>
    ///     <item>
    ///         <description>Renders each page by making HTTP requests to the running application</description>
    ///     </item>
    ///     <item>
    ///         <description>Saves each page as a static HTML file in the output directory</description>
    ///     </item>
    /// </list>
    /// </remarks>
    internal async Task GenerateStaticPages(string appUrl)
    {
        // Collect pages to generate from content services and options
        var pagesToGenerate = ImmutableList<(PageToGenerate Page, Priority Priority)>.Empty;
        foreach (var content in contentServiceCollection)
        {
            pagesToGenerate = pagesToGenerate.AddRange(await content.GetPagesToGenerateAsync(), Priority.Normal);

        }

        // Note: Non-parametrized Razor pages are now handled by RazorPageContentService
        // which is registered as an IContentService and processed above with other content services

        // add explicitly defined pages to generate
        pagesToGenerate = pagesToGenerate.AddRange(options.PagesToGenerate, Priority.Normal);

        // add pages that have been mapped via app.MapGet()
        // this contains styles.css which needs to be last
        pagesToGenerate = pagesToGenerate.AddRange(routeHelper.GetMapGetRoutes(), Priority.MustBeLast);

        // Clear and recreate the output directory
        if (_fileSystem.Directory.Exists(outputOptions.OutputFolderPath.Value))
        {
            _fileSystem.Directory.Delete(outputOptions.OutputFolderPath.Value, true);
        }
        _fileSystem.Directory.CreateDirectory(outputOptions.OutputFolderPath.Value);

        var contentToCopy = ImmutableList<ContentToCopy>.Empty;
        foreach (var content in contentServiceCollection)
        {
            contentToCopy = contentToCopy.AddRange(await content.GetContentToCopyAsync());
        }

        contentToCopy = contentToCopy.AddRange(GetStaticWebAssetsToOutput(environment.WebRootFileProvider, string.Empty));

        // Also include static files from Razor Class Libraries
        var allFileProviders = GetAllFileProviders();
        foreach (var fileProvider in allFileProviders)
        {
            if (fileProvider != environment.WebRootFileProvider)
            {
                logger.LogDebug("Adding static web assets from file provider: {fileProvider}", fileProvider);
                contentToCopy = contentToCopy.AddRange(GetStaticWebAssetsToOutput(fileProvider, string.Empty));
            }
        }

        // Also include custom content file providers that MapContentEngineStaticAssets sets up
        var customContentProviders = GetContentEngineFileProviders();
        foreach (var (fileProvider, requestPath) in customContentProviders)
        {
            logger.LogDebug("Adding content engine static assets from: {fileProvider} at path: {requestPath}", fileProvider, requestPath);
            var assets = GetStaticWebAssetsToOutput(fileProvider, string.Empty);

            // Adjust the target path to include the request path prefix
            var adjustedAssets = assets.Select(asset => new ContentToCopy(
                asset.SourcePath,
                string.IsNullOrEmpty(requestPath) ? asset.TargetPath : $"{requestPath.TrimStart('/')}/{asset.TargetPath}".TrimStart('/'),
                asset.ExcludedExtensions
            ));

            contentToCopy = contentToCopy.AddRange(adjustedAssets);
        }

        // Remove duplicates based on target path - keep the first occurrence
        contentToCopy = contentToCopy
            .GroupBy(c => c.TargetPath)
            .Select(g => g.First())
            .ToImmutableList();

        // Copy all content to the output directory
        foreach (var pathToCopy in contentToCopy)
        {
            var targetPath = _fileSystem.Path.Combine(outputOptions.OutputFolderPath.Value, pathToCopy.TargetPath);

            logger.LogInformation("Copying {sourcePath} to {targetPath}", pathToCopy.SourcePath, targetPath);
            CopyContent(pathToCopy.SourcePath, targetPath, pathToCopy.ExcludedExtensions);
        }

        // Collect content to create from all content services
        var contentToCreate = ImmutableList<ContentToCreate>.Empty;
        foreach (var content in contentServiceCollection)
        {
            contentToCreate = contentToCreate.AddRange(await content.GetContentToCreateAsync());
        }

        // Create content files in the output directory
        foreach (var contentItem in contentToCreate)
        {
            var targetPath = _fileSystem.Path.Combine(outputOptions.OutputFolderPath.Value, contentItem.TargetPath.RemoveLeadingSlash().Value);

            var directoryPath = _fileSystem.Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                _fileSystem.Directory.CreateDirectory(directoryPath);
            }

            logger.LogInformation("Creating content file at {targetPath}", targetPath);
            await _fileSystem.File.WriteAllBytesAsync(targetPath, contentItem.Bytes);
        }

        // Create an HTTP client for fetching rendered pages
        using HttpClient client = new();
        client.BaseAddress = new Uri(appUrl);

        var sw = Stopwatch.StartNew();

        pagesToGenerate = pagesToGenerate
            .GroupBy(c => c.Page.OutputFile)
            .Select(g => g.First())
            .ToImmutableList();

        // Build a lookup of all valid output paths for link verification (including static files)
        var validOutputPaths = BuildValidOutputPathsLookup(pagesToGenerate, contentToCopy, outputOptions.BaseUrl);
        var allBrokenLinks = ImmutableList<BrokenLink>.Empty;
        var brokenLinksLock = new object();

        // Generate each page by making HTTP requests and saving the response
        foreach (var priority in pagesToGenerate.Select(i => i.Priority).Distinct().Order())
        {
            var pagesToGenerateByPriority = pagesToGenerate
                .Where(i => i.Priority == priority)
                .Select(i => i.Page)
                .ToList();

            if (pagesToGenerateByPriority.Count == 0)
            {
                continue;
            }

            await Parallel.ForEachAsync(pagesToGenerateByPriority, async (page, ctx) =>
            {
                if (page.IsBinary)
                {
                    byte[] content;
                    try
                    {
                        content = await client.GetByteArrayAsync(page.Url, ctx);

                        logger.LogInformation("Generated binary {pageUrl} into {pageOutputFile}", page.Url, page.OutputFile);

                    }
                    catch (HttpRequestException ex)
                    {
                        logger.LogWarning("Failed to retrieve page at {pageUrl}. StatusCode:{statusCode}. Error: {exceptionMessage}", page.Url, ex.StatusCode, ex.Message);
                        return;
                    }

                    var outFilePath = _fileSystem.Path.Combine(outputOptions.OutputFolderPath.Value, page.OutputFile.RemoveLeadingSlash().Value);

                    var directoryPath = _fileSystem.Path.GetDirectoryName(outFilePath);
                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        _fileSystem.Directory.CreateDirectory(directoryPath);
                    }

                    await _fileSystem.File.WriteAllBytesAsync(outFilePath, content, ctx);
                }
                else
                {
                    string content;
                    try
                    {
                        content = await client.GetStringAsync(page.Url, ctx);

                        logger.LogInformation("Generated {pageUrl} into {pageOutputFile}", page.Url, page.OutputFile);

                    }
                    catch (HttpRequestException ex)
                    {
                        logger.LogWarning("Failed to retrieve page at {pageUrl}. StatusCode:{statusCode}. Error: {exceptionMessage}", page.Url, ex.StatusCode, ex.Message);
                        return;
                    }

                    // Validate links in the generated HTML if verification is enabled
                    if (outputOptions.VerifyLinks)
                    {
                        var brokenLinksInPage = await linkVerificationService.ValidateLinksAsync(
                            content,
                            page.Url,
                            validOutputPaths,
                            outputOptions.BaseUrl);

                        if (brokenLinksInPage.Count > 0)
                        {
                            lock (brokenLinksLock)
                            {
                                allBrokenLinks = allBrokenLinks.AddRange(brokenLinksInPage);
                            }
                        }
                    }

                    var outFilePath = _fileSystem.Path.Combine(outputOptions.OutputFolderPath.Value, page.OutputFile.RemoveLeadingSlash().Value);

                    var directoryPath = _fileSystem.Path.GetDirectoryName(outFilePath);
                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        _fileSystem.Directory.CreateDirectory(directoryPath);
                    }

                    await _fileSystem.File.WriteAllTextAsync(outFilePath, content, ctx);
                }

            });
        }

        sw.Stop();
        logger.LogInformation("All pages generated in {elapsedMilliseconds}ms", sw.ElapsedMilliseconds);

        // Report broken links if any were found
        if (allBrokenLinks.Count > 0)
        {
            throw new BrokenLinksException(allBrokenLinks);
        }
    }

    /// <summary>
    /// Gets all file providers used by the application, including those from Razor Class Libraries
    /// </summary>
    /// <returns>Collection of file providers</returns>
    private IEnumerable<IFileProvider> GetAllFileProviders()
    {
        // Check if the WebRootFileProvider is a composite that includes RCL assets
        if (environment.WebRootFileProvider is CompositeFileProvider webRootComposite)
        {
            foreach (var provider in webRootComposite.FileProviders)
            {
                yield return provider;
            }
        }
        else
        {
            // If not composite, just return the main provider
            yield return environment.WebRootFileProvider;
        }
    }

    /// <summary>
    /// Gets the custom file providers that MapContentEngineStaticAssets creates for content directories
    /// </summary>
    /// <returns>Collection of file providers with their request paths</returns>
    private IEnumerable<(IFileProvider FileProvider, string RequestPath)> GetContentEngineFileProviders()
    {
        var currentDirectory = _fileSystem.Directory.GetCurrentDirectory();

        // Add the main content root file provider (mimics MapContentEngineStaticAssets logic)
        if (!string.IsNullOrEmpty(options.ContentRootPath))
        {
            var contentRootPath = _fileSystem.Path.Combine(currentDirectory, options.ContentRootPath);
            if (_fileSystem.Directory.Exists(contentRootPath))
            {
                yield return (new PhysicalFileProvider(contentRootPath), "");
            }
        }

        // Get all IContentOptions from services (mimics MapContentEngineStaticAssets logic)
        var contentOptions = serviceProvider.GetServices<IContentOptions>().ToList();
        foreach (var option in contentOptions)
        {
            var contentPath = _fileSystem.Path.Combine(currentDirectory, option.ContentPath.Value);

            if (_fileSystem.Directory.Exists(contentPath))
            {
                var requestPath = option.BasePageUrl.EnsureLeadingSlash();
                yield return (new PhysicalFileProvider(contentPath), requestPath.Value);
            }
        }
    }

    /// <summary>
    ///     Recursively collects all static web assets (files in wwwroot) that should be copied to the output directory.
    /// </summary>
    /// <param name="fileProvider">File provider for accessing static web assets</param>
    /// <param name="subPath">Current sub-path being processed</param>
    /// <returns>Collection of content items to copy</returns>
    private static IEnumerable<ContentToCopy> GetStaticWebAssetsToOutput(IFileProvider fileProvider, string subPath)
    {
        var contents = fileProvider.GetDirectoryContents(subPath);

        foreach (var item in contents)
        {
            var fullPath = $"{subPath}{item.Name}";
            if (item.IsDirectory)
            {
                foreach (var file in GetStaticWebAssetsToOutput(fileProvider, $"{fullPath}/"))
                {
                    yield return file;
                }
            }
            else
            {
                if (item.PhysicalPath is not null)
                {
                    yield return new ContentToCopy(item.PhysicalPath, fullPath);
                }
            }
        }
    }



    /// <summary>
    /// Copies content from a source path to a target path, respecting glob patterns for ignored paths.
    /// Handles both single file and directory copying.
    /// </summary>
    /// <param name="sourcePath">The source file or directory path</param>
    /// <param name="targetPath">The target file or directory path</param>
    /// <param name="excludedExtensions">File extensions to exclude during copying (e.g., [".md", ".txt"])</param>
    private void CopyContent(string sourcePath, string targetPath, string[]? excludedExtensions = null)
    {
        // Calculate relative path from output folder to target path for glob matching
        var relativePath = GetRelativePathFromOutputFolder(targetPath);

        // Check if the target matches any ignored patterns
        if (FilePathGlobMatcher.IsIgnored(relativePath, options.IgnoredPathsOnContentCopy))
        {
            logger.LogDebug("Skipping ignored path: {targetPath}", targetPath);
            return;
        }

        try
        {
            // Handle single file copy
            if (_fileSystem.File.Exists(sourcePath))
            {
                CopySingleFile(sourcePath, targetPath, excludedExtensions);
                return;
            }

            // Handle directory copy
            if (!_fileSystem.Directory.Exists(sourcePath))
            {
                logger.LogError("Source path '{SourcePath}' does not exist", sourcePath);
                return;
            }

            CopyDirectory(sourcePath, targetPath, excludedExtensions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error copying from '{SourcePath}' to '{TargetPath}'", sourcePath, targetPath);
        }
    }

    /// <summary>
    /// Copies a single file, creating the target directory if needed
    /// </summary>
    /// <param name="sourceFile">The source file path</param>
    /// <param name="targetFile">The target file path</param>
    /// <param name="excludedExtensions">File extensions to exclude during copying</param>
    private void CopySingleFile(string sourceFile, string targetFile, string[]? excludedExtensions = null)
    {
        // Check if the file extension should be excluded
        if (excludedExtensions != null)
        {
            var fileExtension = _fileSystem.Path.GetExtension(sourceFile);
            if (excludedExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
            {
                logger.LogDebug("Skipping file {SourceFile} due to excluded extension {Extension}", sourceFile, fileExtension);
                return;
            }
        }

        var targetDir = _fileSystem.Path.GetDirectoryName(targetFile);
        if (string.IsNullOrEmpty(targetDir))
        {
            logger.LogError("Invalid target path '{TargetFile}'", targetFile);
            return;
        }
        _fileSystem.Directory.CreateDirectory(targetDir);
        _fileSystem.File.Copy(sourceFile, targetFile, overwrite: true);
    }

    /// <summary>
    /// Copies a directory and its contents to the target location, respecting glob patterns for ignored paths
    /// </summary>
    /// <param name="sourceDir">The source directory path</param>
    /// <param name="targetDir">The target directory path</param>
    /// <param name="excludedExtensions">File extensions to exclude during copying</param>
    private void CopyDirectory(string sourceDir, string targetDir, string[]? excludedExtensions = null)
    {
        _fileSystem.Directory.CreateDirectory(targetDir);

        // Create all subdirectories first (except ignored ones)
        foreach (var dirPath in _fileSystem.Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var newDirPath = GetTargetPath(dirPath, sourceDir, targetDir);
            var relativePath = GetRelativePathFromOutputFolder(newDirPath);

            if (FilePathGlobMatcher.IsIgnored(relativePath, options.IgnoredPathsOnContentCopy))
            {
                logger.LogDebug("Skipping ignored directory: {newDirPath}", newDirPath);
                continue;
            }
            _fileSystem.Directory.CreateDirectory(newDirPath);
        }

        // Copy all files (except those in ignored paths or with excluded extensions)
        foreach (var filePath in _fileSystem.Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            var targetFilePath = GetTargetPath(filePath, sourceDir, targetDir);
            var relativePath = GetRelativePathFromOutputFolder(targetFilePath);

            // Skip if the file path matches ignored patterns or the parent directory doesn't exist
            var targetFileDir = _fileSystem.Path.GetDirectoryName(targetFilePath);
            if (FilePathGlobMatcher.IsIgnored(relativePath, options.IgnoredPathsOnContentCopy) || !_fileSystem.Directory.Exists(targetFileDir))
            {
                if (FilePathGlobMatcher.IsIgnored(relativePath, options.IgnoredPathsOnContentCopy))
                {
                    logger.LogDebug("Skipping ignored file: {targetFilePath}", targetFilePath);
                }
                continue;
            }

            // Check if the file extension should be excluded
            if (excludedExtensions != null)
            {
                var fileExtension = _fileSystem.Path.GetExtension(filePath);
                if (excludedExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
                {
                    logger.LogDebug("Skipping file {FilePath} due to excluded extension {Extension}", filePath, fileExtension);
                    continue;
                }
            }

            _fileSystem.File.Copy(filePath, targetFilePath, overwrite: true);
        }
    }

    private string GetTargetPath(string sourcePath, string sourceDir, string targetDir)
    {
        var relativePath = sourcePath[sourceDir.Length..].TrimStart(Path.DirectorySeparatorChar);
        return _fileSystem.Path.Combine(targetDir, relativePath);
    }

    /// <summary>
    /// Calculates the relative path from the output folder to a target path for glob pattern matching
    /// </summary>
    /// <param name="targetPath">The full target path</param>
    /// <returns>The relative path from the output folder</returns>
    private string GetRelativePathFromOutputFolder(string targetPath)
    {
        var outputFolder = outputOptions.OutputFolderPath.Value;
        if (targetPath.StartsWith(outputFolder, StringComparison.OrdinalIgnoreCase))
        {
            return targetPath[outputFolder.Length..].TrimStart(Path.DirectorySeparatorChar, '/');
        }
        return targetPath;
    }

    /// <summary>
    /// Builds a lookup set of all valid output paths for link verification.
    /// Includes both generated pages and static files (scripts, CSS, images, etc.).
    /// Includes variations with/without trailing slashes and index.html handling.
    /// </summary>
    private ImmutableHashSet<string> BuildValidOutputPathsLookup(
        ImmutableList<(PageToGenerate Page, Priority Priority)> pagesToGenerate,
        ImmutableList<ContentToCopy> contentToCopy,
        UrlPath baseUrl)
    {
        var validPaths = ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);

        // Add all generated pages
        foreach (var (page, _) in pagesToGenerate)
        {
            // Add the URL as-is (without BaseUrl)
            var normalizedUrl = NormalizeUrlForValidation(page.Url, baseUrl);
            validPaths.Add(normalizedUrl);

            // Also add URL with index.html removed (for directory-style URLs)
            if (normalizedUrl.EndsWith("/index.html", StringComparison.OrdinalIgnoreCase))
            {
                validPaths.Add(normalizedUrl[..^"index.html".Length]);
            }

            // Also add URL with trailing slash removed
            if (normalizedUrl.EndsWith('/') && normalizedUrl.Length > 1)
            {
                validPaths.Add(normalizedUrl.TrimEnd('/'));
            }

            // Also add URL with trailing slash added
            if (!normalizedUrl.EndsWith('/'))
            {
                validPaths.Add(normalizedUrl + "/");
            }
        }

        // Add all static files (scripts, CSS, images, fonts, etc.)
        foreach (var content in contentToCopy)
        {
            // Convert file system path to URL path (replace backslashes with forward slashes)
            var urlPath = content.TargetPath.Value.Replace('\\', '/');

            // Ensure it starts with /
            if (!urlPath.StartsWith('/'))
            {
                urlPath = "/" + urlPath;
            }

            // Normalize and add to valid paths
            var normalizedUrl = NormalizeUrlForValidation(new UrlPath(urlPath), baseUrl);
            validPaths.Add(normalizedUrl);
        }

        return validPaths.ToImmutable();
    }

    /// <summary>
    /// Normalizes a URL for validation by removing BaseUrl prefix and ensuring leading slash.
    /// </summary>
    private string NormalizeUrlForValidation(UrlPath url, UrlPath baseUrl)
    {
        var normalizedUrl = url.Value;

        // Remove BaseUrl prefix if present
        if (!string.IsNullOrEmpty(baseUrl.Value) && baseUrl.Value != "/")
        {
            var baseUrlNormalized = baseUrl.Value.TrimEnd('/');
            if (normalizedUrl.StartsWith(baseUrlNormalized, StringComparison.OrdinalIgnoreCase))
            {
                normalizedUrl = normalizedUrl[baseUrlNormalized.Length..];
            }
        }

        // Ensure it starts with /
        if (!normalizedUrl.StartsWith('/'))
        {
            normalizedUrl = "/" + normalizedUrl;
        }

        return normalizedUrl;
    }

    enum Priority
    {
        MustBeFirst = 0,
        Normal = 50,
        MustBeLast = 100,
    }
}

internal static class ListExtensions
{
    public static ImmutableList<(TItem, TPriority)> AddRange<TItem, TPriority>(
        this ImmutableList<(TItem, TPriority)> queue,
        IEnumerable<TItem> items, TPriority priority)
    {
        return items.Aggregate(queue, (current, item) => current.Add((item, priority)));
    }
}