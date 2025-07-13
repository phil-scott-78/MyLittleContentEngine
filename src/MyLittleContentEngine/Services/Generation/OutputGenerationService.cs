using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
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
/// <param name="serviceProvider">Service provider for accessing registered content options</param>
/// <param name="logger">Logger for diagnostic output</param>
internal class OutputGenerationService(
    IWebHostEnvironment environment,
    IEnumerable<IContentService> contentServiceCollection,
    IFileSystem fileSystem,
    RoutesHelperService routeHelper,
    ContentEngineOptions options,
    IServiceProvider serviceProvider,
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
        if (_fileSystem.Directory.Exists(options.OutputFolderPath))
        {
            _fileSystem.Directory.Delete(options.OutputFolderPath, true);
        }
        _fileSystem.Directory.CreateDirectory(options.OutputFolderPath);

        // Prepare paths to ignore during content copy
        var ignoredPathsWithOutputFolder = options
            .IgnoredPathsOnContentCopy
            .Select(x => _fileSystem.Path.Combine(options.OutputFolderPath, x))
            .ToList();

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
                (string.IsNullOrEmpty(requestPath) ? asset.TargetPath : $"{requestPath.TrimStart('/')}/{asset.TargetPath}".TrimStart('/')).ToLowerInvariant()
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
            var targetPath = _fileSystem.Path.Combine(options.OutputFolderPath, pathToCopy.TargetPath);

            logger.LogInformation("Copying {sourcePath} to {targetPath}", pathToCopy.SourcePath, targetPath);
            CopyContent(pathToCopy.SourcePath, targetPath, ignoredPathsWithOutputFolder);
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
            var targetPath = _fileSystem.Path.Combine(options.OutputFolderPath, contentItem.TargetPath.TrimStart('/').ToLowerInvariant());
            
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

                var outFilePath = _fileSystem.Path.Combine(options.OutputFolderPath, page.OutputFile.TrimStart('/').ToLowerInvariant());

                var directoryPath = _fileSystem.Path.GetDirectoryName(outFilePath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    _fileSystem.Directory.CreateDirectory(directoryPath);
                }

                await _fileSystem.File.WriteAllTextAsync(outFilePath, content, ctx);
            });
        }

        sw.Stop();
        logger.LogInformation("All pages generated in {elapsedMilliseconds}ms", sw.ElapsedMilliseconds);
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
            var contentPath = _fileSystem.Path.Combine(currentDirectory, option.ContentPath);
            
            if (_fileSystem.Directory.Exists(contentPath))
            {
                string requestPath = "";
                if (!string.IsNullOrWhiteSpace(option.BasePageUrl))
                {
                    requestPath = option.BasePageUrl.StartsWith('/') 
                        ? option.BasePageUrl 
                        : '/' + option.BasePageUrl;
                }
                
                yield return (new PhysicalFileProvider(contentPath), requestPath);
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
                    yield return new ContentToCopy(item.PhysicalPath, fullPath.ToLowerInvariant());
                }
            }
        }
    }



    /// <summary>
    /// Copies content from a source path to a target path, respecting a list of ignored paths.
    /// Handles both single file and directory copying.
    /// </summary>
    /// <param name="sourcePath">The source file or directory path</param>
    /// <param name="targetPath">The target file or directory path</param>
    /// <param name="ignoredPaths">List of paths to ignore during copying</param>
    private void CopyContent(string sourcePath, string targetPath, List<string> ignoredPaths)
    {
        // Check if the target is in ignored paths
        if (ignoredPaths.Contains(targetPath))
        {
            return;
        }

        try
        {
            // Handle single file copy
            if (_fileSystem.File.Exists(sourcePath))
            {
                CopySingleFile(sourcePath, targetPath);
                return;
            }

            // Handle directory copy
            if (!_fileSystem.Directory.Exists(sourcePath))
            {
                logger.LogError("Source path '{SourcePath}' does not exist", sourcePath);
                return;
            }

            CopyDirectory(sourcePath, targetPath, ignoredPaths);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error copying from '{SourcePath}' to '{TargetPath}'", sourcePath, targetPath);
        }
    }

    /// <summary>
    /// Copies a single file, creating the target directory if needed
    /// </summary>
    private void CopySingleFile(string sourceFile, string targetFile)
    {
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
    /// Copies a directory and its contents to the target location, respecting ignored paths
    /// </summary>
    private void CopyDirectory(string sourceDir, string targetDir, List<string> ignoredPaths)
    {
        _fileSystem.Directory.CreateDirectory(targetDir);

        // Transform ignored paths to be relative to the target
        var ignoredTargetPaths = ignoredPaths
            .ConvertAll(path => _fileSystem.Path.Combine(targetDir, path));

        // Create all subdirectories first (except ignored ones)
        foreach (var dirPath in _fileSystem.Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var newDirPath = GetTargetPath(dirPath, sourceDir, targetDir);
            if (ignoredTargetPaths.Contains(newDirPath))
            {
                continue;
            }
            _fileSystem.Directory.CreateDirectory(newDirPath);
        }

        // Copy all files (except those in ignored paths)
        foreach (var filePath in _fileSystem.Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            var targetFilePath = GetTargetPath(filePath, sourceDir, targetDir);

            // Skip if the file path is ignored or the parent directory doesn't exist
            var targetFileDir = _fileSystem.Path.GetDirectoryName(targetFilePath);
            if (ignoredTargetPaths.Contains(targetFilePath) || !_fileSystem.Directory.Exists(targetFileDir))
            {
                continue;
            }
            _fileSystem.File.Copy(filePath, targetFilePath, overwrite: true);
        }
    }

    private string GetTargetPath(string sourcePath, string sourceDir, string targetDir)
    {
        var relativePath = sourcePath[sourceDir.Length..].TrimStart(Path.DirectorySeparatorChar);
        return _fileSystem.Path.Combine(targetDir, relativePath);
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