using System.Collections.Immutable;
using System.Text;
using MyLittleContentEngine.Models;

namespace MyLittleContentEngine.Services;

/// <summary>
/// Base exception class for all MyLittleContentEngine-specific exceptions.
/// </summary>
public class ContentEngineException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentEngineException"/> class.
    /// </summary>
    public ContentEngineException() : base("An error occurred in MyLittleContentEngine.") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentEngineException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    protected ContentEngineException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentEngineException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ContentEngineException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when there is an error processing content in MyLittleContentEngine.
/// </summary>
public class ContentProcessingException : ContentEngineException
{
    /// <summary>
    /// Gets the path to the content file that caused the exception.
    /// </summary>
    public string? ContentPath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentProcessingException"/> class.
    /// </summary>
    public ContentProcessingException() : base("An error occurred while processing content.") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentProcessingException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    protected ContentProcessingException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentProcessingException"/> class with a specified error message
    /// and the path to the content file that caused the exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="contentPath">The path to the content file that caused the exception.</param>
    protected ContentProcessingException(string message, string contentPath)
        : base($"{message} Content path: {contentPath}")
    {
        ContentPath = contentPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentProcessingException"/> class with a specified error message,
    /// the path to the content file that caused the exception, and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="contentPath">The path to the content file that caused the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ContentProcessingException(string message, string contentPath, Exception innerException)
        : base($"{message} Content path: {contentPath}", innerException)
    {
        ContentPath = contentPath;
    }

    /// <inheritdoc />
    public ContentProcessingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when there is an error with file operations in MyLittleContentEngine.
/// </summary>
public class FileOperationException : ContentEngineException
{
    /// <summary>
    /// Gets the path to the file that caused the exception.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileOperationException"/> class.
    /// </summary>
    public FileOperationException() : base("An error occurred during a file operation.") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileOperationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public FileOperationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileOperationException"/> class with a specified error message
    /// and the path to the file that caused the exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="filePath">The path to the file that caused the exception.</param>
    public FileOperationException(string message, string filePath)
        : base($"{message} File path: {filePath}")
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileOperationException"/> class with a specified error message,
    /// the path to the file that caused the exception, and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="filePath">The path to the file that caused the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public FileOperationException(string message, string filePath, Exception innerException)
        : base($"{message} File path: {filePath}", innerException)
    {
        FilePath = filePath;
    }

    /// <inheritdoc />
    public FileOperationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when broken links are detected during static site generation.
/// Contains detailed information about all broken links found.
/// </summary>
public class BrokenLinksException : ContentEngineException
{
    /// <summary>
    /// Gets the collection of broken links that were detected.
    /// </summary>
    public ImmutableList<BrokenLink> BrokenLinks { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BrokenLinksException"/> class.
    /// </summary>
    /// <param name="brokenLinks">The collection of broken links detected.</param>
    public BrokenLinksException(ImmutableList<BrokenLink> brokenLinks)
        : base(FormatMessage(brokenLinks))
    {
        BrokenLinks = brokenLinks;
    }

    private static string FormatMessage(ImmutableList<BrokenLink> brokenLinks)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Found {brokenLinks.Count} broken link(s) during static site generation:");
        sb.AppendLine();

        // Group by source page for better readability
        var grouped = brokenLinks.GroupBy(bl => bl.SourcePage.Value);
        foreach (var group in grouped)
        {
            sb.AppendLine($"  In page: {group.Key}");
            foreach (var link in group)
            {
                sb.AppendLine($"    - <{link.ElementType} {link.LinkType.ToString().ToLower()}=\"{link.BrokenUrl}\">");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}