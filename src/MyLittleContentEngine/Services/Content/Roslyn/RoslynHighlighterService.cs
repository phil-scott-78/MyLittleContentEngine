using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Text;
using System.Web;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Services.Infrastructure;

namespace MyLittleContentEngine.Services.Content.Roslyn;

/// <summary>
/// A service for providing syntax highlighting for code blocks using Roslyn.
/// </summary>
public class RoslynHighlighterService : IDisposable
{
    private readonly ILogger<RoslynHighlighterService> _logger;
    private readonly SyntaxHighlighter _highlighter;
    private readonly RoslynExampleCoordinator? _documentProcessor;
    private readonly ConcurrentDictionary<int, string> _cache;
    private readonly IContentEngineFileWatcher _fileWatcher;
    private readonly IFileSystem _fileSystem;
    private readonly string? _solutionDirectory;
    private bool _disposed;

    /// <summary>
    /// Provides functionality for syntax highlighting of code using Roslyn.
    /// </summary>
    public RoslynHighlighterService(RoslynHighlighterOptions options, ILogger<RoslynHighlighterService> logger,
        IContentEngineFileWatcher fileWatcher, IFileSystem fileSystem, RoslynExampleCoordinator? documentProcessor = null)
    {
        _logger = logger;
        _highlighter = new SyntaxHighlighter();
        _cache = new ConcurrentDictionary<int, string>();
        _fileWatcher = fileWatcher;
        _fileSystem = fileSystem;
        _documentProcessor = documentProcessor;

        if (options.ConnectedSolution != null)
        {
            _solutionDirectory = fileSystem.Path.GetDirectoryName(options.ConnectedSolution.SolutionPath) 
                       ?? throw new InvalidOperationException("Could not determine solution directory");
            _fileWatcher.AddPathWatch(_solutionDirectory, "*.cs", OnFileChanged);
        }
    }

    private void OnFileChanged(string filePath)
    {
        _logger.LogDebug("FileChanged: {filePath}", filePath);
        _documentProcessor?.InvalidateFile(filePath);
    }

    internal async Task<string> GetCodeOutputAsync(string xmlDocId, string value)
    {
        if (_documentProcessor == null)
        {
            throw new InvalidOperationException(
                "Code output is only supported when ConnectedSolution is configured");
        }

        return await _documentProcessor.GetCodeResultAsync(xmlDocId, value);
    }

    internal async Task<string> HighlightExampleAsync(string xmlDocIds, bool bodyOnly)
    {
        if (_documentProcessor == null)
        {
            throw new InvalidOperationException(
                "Highlighting by XmlDocId is only supported when ConnectedSolution is configured");
        }

        var ids = xmlDocIds
            .ReplaceLineEndings()
            .Split(Environment.NewLine,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var sb = new StringBuilder();

        foreach (var xmlDocId in ids)
        {
            var code = await _documentProcessor.GetCodeFragmentAsync(xmlDocId, bodyOnly);
            code = TextFormatter.NormalizeIndents(code);
            var highlightExample = _highlighter.Highlight(code);
            sb.Append(highlightExample.TrimEnd());
            sb.AppendLine();
            sb.AppendLine();
        }

        return $"<pre><code>{sb.ToString().TrimEnd()}</code></pre>";
    }

    internal string Highlight(string codeContent, Language language = Language.CSharp)
    {
        var highlightExample = _cache.GetOrAdd(codeContent.GetHashCode(), _ =>
            _highlighter.Highlight(HttpUtility.HtmlDecode(codeContent), language));
        return $"<pre><code>{highlightExample}</code></pre>";
    }

    /// <summary>
    /// Retrieves the full content of a file using a path relative to the solution root.
    /// </summary>
    /// <param name="relativePath">The path relative to the solution root directory</param>
    /// <returns>The full content of the file</returns>
    /// <exception cref="InvalidOperationException">Thrown when no connected solution is configured</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist</exception>
    public async Task<string> GetFileContentAsync(string relativePath)
    {
        if (_solutionDirectory == null)
        {
            throw new InvalidOperationException(
                "File content retrieval is only supported when ConnectedSolution is configured");
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Relative path cannot be null or empty", nameof(relativePath));
        }

        // Normalize the path separators and remove any leading separators
        var normalizedPath = relativePath.Replace('\\', '/').TrimStart('/', '\\');
        var fullPath = _fileSystem.Path.Combine(_solutionDirectory, normalizedPath);

        // Ensure the resolved path is still within the solution directory (security check)
        var resolvedPath = _fileSystem.Path.GetFullPath(fullPath);
        var solutionPath = _fileSystem.Path.GetFullPath(_solutionDirectory);
        
        if (!resolvedPath.StartsWith(solutionPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                $"Access to files outside the solution directory is not allowed. Requested: {relativePath}");
        }

        if (!_fileSystem.File.Exists(resolvedPath))
        {
            throw new FileNotFoundException($"File not found: {relativePath}", resolvedPath);
        }

        _logger.LogDebug("Reading file content from: {FilePath}", resolvedPath);
        return await _fileSystem.File.ReadAllTextAsync(resolvedPath);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _fileWatcher.Dispose();
            _documentProcessor?.Dispose();
            _highlighter.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="RoslynHighlighterService"/> class.
    /// </summary>
    ~RoslynHighlighterService()
    {
        Dispose(false);
    }
}