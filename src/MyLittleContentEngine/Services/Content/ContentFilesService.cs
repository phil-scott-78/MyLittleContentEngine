using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Infrastructure;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// Service for handling content file operations in a MyLittleContentEngine site.
/// </summary>
/// <typeparam name="TFrontMatter">The type of front matter used in content.</typeparam>
public class ContentFilesService<TFrontMatter>
    where TFrontMatter : class, IFrontMatter
{
    private readonly ContentEngineContentOptions<TFrontMatter> _engineContentOptions;
    private readonly PathUtilities _pathUtilities;
    private readonly ILogger<ContentFilesService<TFrontMatter>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentFilesService{TFrontMatter}"/> class.
    /// </summary>
    /// <param name="engineContentOptions">Content options.</param>
    /// <param name="pathUtilities">Path utilities service.</param>
    /// <param name="logger">Logger instance.</param>
    public ContentFilesService(
        ContentEngineContentOptions<TFrontMatter> engineContentOptions,
        PathUtilities pathUtilities,
        ILogger<ContentFilesService<TFrontMatter>> logger)
    {
        _engineContentOptions = engineContentOptions;
        _pathUtilities = pathUtilities;
        _logger = logger;
    }

    /// <summary>
    /// Gets all content files that match the configured pattern.
    /// </summary>
    /// <returns>A tuple with the array of file paths and the absolute content path.</returns>
    /// <exception cref="FileOperationException">Thrown when there is an error accessing content files.</exception>
    internal (string[] Files, string AbsContentPath) GetContentFiles()
    {
        try
        {
            // Validate the content path exists
            var absoluteContentPath = _pathUtilities.ValidateDirectoryPath(_engineContentOptions.ContentPath);

            return _pathUtilities.GetFilesInDirectory(
                absoluteContentPath,
                _engineContentOptions.PostFilePattern);
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Content directory not found: {ContentPath}", _engineContentOptions.ContentPath);
            throw new FileOperationException(
                "Content directory not found",
                _engineContentOptions.ContentPath,
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting content files from {ContentPath}", _engineContentOptions.ContentPath);
            throw new FileOperationException(
                "Failed to retrieve content files",
                _engineContentOptions.ContentPath,
                ex);
        }
    }

    /// <summary>
    /// Creates a content URL from a file path.
    /// </summary>
    /// <param name="filePath">The full file path.</param>
    /// <param name="baseContentPath">The base content path.</param>
    /// <returns>A URL for the content.</returns>
    internal string CreateContentUrl(string filePath, string baseContentPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentException.ThrowIfNullOrEmpty(baseContentPath);

        try
        {
            return _pathUtilities.FilePathToUrlPath(filePath, baseContentPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create content URL for {FilePath}", filePath);
            throw new FileOperationException(
                "Failed to create content URL",
                filePath,
                ex);
        }
    }

    /// <summary>
    /// Creates a navigation URL from a content URL.
    /// </summary>
    /// <param name="contentUrl">The content URL.</param>
    /// <returns>A full navigation URL.</returns>
    internal string CreateNavigationUrl(string contentUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentUrl);

        return PathUtilities.CombineUrl(_engineContentOptions.BasePageUrl, contentUrl);
    }

    /// <summary>
    /// Gets the output file path for a content URL.
    /// </summary>
    /// <param name="contentUrl">The content URL.</param>
    /// <returns>An output file path.</returns>
    internal string GetOutputFilePath(string contentUrl)
    {
        ArgumentException.ThrowIfNullOrEmpty(contentUrl);

        var relativePath = contentUrl.Replace('/', Path.DirectorySeparatorChar);
        return _pathUtilities.Combine(_engineContentOptions.BasePageUrl, $"{relativePath}.html");
    }

    /// <summary>
    /// Gets the page URL for a content URL.
    /// </summary>
    /// <param name="contentUrl">The content URL.</param>
    /// <returns>A page URL.</returns>
    internal string GetPageUrl(string contentUrl)
    {
        ArgumentException.ThrowIfNullOrEmpty(contentUrl);

        return PathUtilities.CombineUrl(_engineContentOptions.BasePageUrl, contentUrl);
    }

    /// <summary>
    /// Gets the content to be copied to the output directory.
    /// </summary>
    /// <returns>A list of content to copy.</returns>
    internal ImmutableList<ContentToCopy> GetContentToCopy()
    {
        try
        {
            // Validate the content path exists
            _pathUtilities.ValidateDirectoryPath(_engineContentOptions.ContentPath);

            return new[]
            {
                new ContentToCopy(_engineContentOptions.ContentPath, _engineContentOptions.BasePageUrl)
            }.ToImmutableList();
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Content directory not found: {ContentPath}", _engineContentOptions.ContentPath);
            return ImmutableList<ContentToCopy>.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting content to copy from {ContentPath}", _engineContentOptions.ContentPath);
            return ImmutableList<ContentToCopy>.Empty;
        }
    }
}