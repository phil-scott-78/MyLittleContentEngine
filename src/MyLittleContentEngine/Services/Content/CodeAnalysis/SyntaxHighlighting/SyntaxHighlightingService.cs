using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Services.Content.CodeAnalysis.Configuration;
using MyLittleContentEngine.Services.Content.CodeAnalysis.SymbolAnalysis;

namespace MyLittleContentEngine.Services.Content.CodeAnalysis.SyntaxHighlighting;

/// <summary>
/// Main implementation of syntax highlighting that orchestrates other services
/// </summary>
internal class SyntaxHighlightingService : ISyntaxHighlightingService
{
    private readonly ILogger<SyntaxHighlightingService> _logger;
    private readonly CodeAnalysisOptions _options;
    private readonly SyntaxHighlighter _syntaxHighlighter;
    private readonly ISymbolExtractionService? _symbolService;
    private readonly IFileSystem _fileSystem;

    public SyntaxHighlightingService(
        ILogger<SyntaxHighlightingService> logger,
        CodeAnalysisOptions options,
        IFileSystem fileSystem,
        ISymbolExtractionService? symbolService = null)
    {
        _logger = logger;
        _options = options;
        _fileSystem = fileSystem;
        _symbolService = symbolService;
        _syntaxHighlighter = new SyntaxHighlighter();
    }

    public Task<HighlightedCode> HighlightAsync(string code, Language language = Language.CSharp)
    {
        if (string.IsNullOrEmpty(code))
        {
            return Task.FromResult(HighlightedCode.CreateSuccess(string.Empty, string.Empty, language));
        }

        try
        {
            var html = _syntaxHighlighter.Highlight(code, language);
            return Task.FromResult(HighlightedCode.CreateSuccess(html, code, language));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to highlight code");
            return Task.FromResult(HighlightedCode.CreateFailure(code, language, ex.Message));
        }
    }

    public async Task<HighlightedCode> HighlightSymbolAsync(string xmlDocId, bool bodyOnly = false)
    {
        if (_symbolService == null)
        {
            return HighlightedCode.CreateFailure(
                string.Empty,
                Language.CSharp,
                "Symbol extraction service not available");
        }

        try
        {
            _logger.LogDebug("Highlighting symbol {XmlDocId}, bodyOnly: {BodyOnly}", xmlDocId, bodyOnly);

            var code = await _symbolService.ExtractCodeFragmentAsync(xmlDocId, bodyOnly);
            if (string.IsNullOrEmpty(code))
            {
                return HighlightedCode.CreateFailure(
                    string.Empty,
                    Language.CSharp,
                    $"Symbol not found: {xmlDocId}");
            }

            var html = _syntaxHighlighter.Highlight(code);
            var result = HighlightedCode.CreateSuccess(html, code, Language.CSharp);
            result.Metadata["XmlDocId"] = xmlDocId;
            result.Metadata["BodyOnly"] = bodyOnly.ToString();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to highlight symbol {XmlDocId}", xmlDocId);
            return HighlightedCode.CreateFailure(string.Empty, Language.CSharp, ex.Message);
        }
    }

    public async Task<HighlightedCode> HighlightFileAsync(string relativePath)
    {
        try
        {
            // Validate path to prevent directory traversal
            if (relativePath.Contains("..") || _fileSystem.Path.IsPathRooted(relativePath))
            {
                return HighlightedCode.CreateFailure(
                    string.Empty,
                    Language.CSharp,
                    "Invalid file path");
            }

            var solutionDir = _fileSystem.Path.GetDirectoryName(_options.SolutionPath);
            if (string.IsNullOrEmpty(solutionDir))
            {
                return HighlightedCode.CreateFailure(
                    string.Empty,
                    Language.CSharp,
                    "Solution directory not found");
            }

            var fullPath = _fileSystem.Path.Combine(solutionDir, relativePath);
            if (!_fileSystem.File.Exists(fullPath))
            {
                return HighlightedCode.CreateFailure(
                    string.Empty,
                    Language.CSharp,
                    $"File not found: {relativePath}");
            }

            var content = await _fileSystem.File.ReadAllTextAsync(fullPath);
            var language = DetectLanguageFromExtension(_fileSystem.Path.GetExtension(relativePath));

            var html = _syntaxHighlighter.Highlight(content, language);
            var result = HighlightedCode.CreateSuccess(html, content, language);
            result.Metadata["FilePath"] = relativePath;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to highlight file {FilePath}", relativePath);
            return HighlightedCode.CreateFailure(string.Empty, Language.CSharp, ex.Message);
        }
    }


    private static Language DetectLanguageFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".cs" => Language.CSharp,
            ".vb" => Language.VisualBasic,
            _ => Language.CSharp // Default to C# for unsupported languages
        };
    }
}