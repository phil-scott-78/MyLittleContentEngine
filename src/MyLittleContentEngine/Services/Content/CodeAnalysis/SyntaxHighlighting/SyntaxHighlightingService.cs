using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Services.Content.CodeAnalysis.Assembly;
using MyLittleContentEngine.Services.Content.CodeAnalysis.Configuration;
using MyLittleContentEngine.Services.Content.CodeAnalysis.Execution;
using MyLittleContentEngine.Services.Content.CodeAnalysis.SolutionWorkspace;
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
    private readonly ICodeExecutionService? _executionService;
    private readonly IAssemblyLoadingService? _assemblyService;
    private readonly ISolutionWorkspaceService? _workspaceService;
    private readonly IFileSystem _fileSystem;

    private static readonly IReadOnlyCollection<Language> _supportedLanguages = Enum.GetValues<Language>();

    public SyntaxHighlightingService(
        ILogger<SyntaxHighlightingService> logger,
        CodeAnalysisOptions options,
        IFileSystem fileSystem,
        ISymbolExtractionService? symbolService = null,
        ICodeExecutionService? executionService = null,
        IAssemblyLoadingService? assemblyService = null,
        ISolutionWorkspaceService? workspaceService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _symbolService = symbolService;
        _executionService = executionService;
        _assemblyService = assemblyService;
        _workspaceService = workspaceService;
        _syntaxHighlighter = new SyntaxHighlighter();
    }

    public IReadOnlyCollection<Language> SupportedLanguages => _supportedLanguages;

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

            var html = _syntaxHighlighter.Highlight(code, Language.CSharp);
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
            if (relativePath.Contains("..") || Path.IsPathRooted(relativePath))
            {
                return HighlightedCode.CreateFailure(
                    string.Empty,
                    Language.CSharp,
                    "Invalid file path");
            }

            var solutionDir = Path.GetDirectoryName(_options.SolutionPath);
            if (string.IsNullOrEmpty(solutionDir))
            {
                return HighlightedCode.CreateFailure(
                    string.Empty,
                    Language.CSharp,
                    "Solution directory not found");
            }

            var fullPath = Path.Combine(solutionDir, relativePath);
            if (!_fileSystem.File.Exists(fullPath))
            {
                return HighlightedCode.CreateFailure(
                    string.Empty,
                    Language.CSharp,
                    $"File not found: {relativePath}");
            }

            var content = await _fileSystem.File.ReadAllTextAsync(fullPath);
            var language = DetectLanguageFromExtension(Path.GetExtension(relativePath));
            
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

    public async Task<HighlightedCode> HighlightExecutionAsync(string xmlDocId, string? attachmentName = null)
    {
        if (_symbolService == null || _executionService == null || _assemblyService == null)
        {
            return HighlightedCode.CreateFailure(
                string.Empty,
                Language.CSharp,
                "Required services not available for code execution");
        }

        try
        {
            _logger.LogDebug("Executing and highlighting {XmlDocId}", xmlDocId);

            // Find the symbol
            var symbolInfo = await _symbolService.FindSymbolAsync(xmlDocId);
            if (symbolInfo == null)
            {
                return HighlightedCode.CreateFailure(
                    string.Empty,
                    Language.CSharp,
                    $"Symbol not found: {xmlDocId}");
            }

            // Load the assembly for the project
            var loadedAssembly = await _assemblyService.LoadFromProjectAsync(symbolInfo.Project);

            // Execute the method
            var executionResult = await _executionService.ExecuteMethodAsync(xmlDocId, loadedAssembly.Assembly);

            if (!executionResult.Success)
            {
                return HighlightedCode.CreateFailure(
                    executionResult.ErrorOutput,
                    Language.CSharp,
                    executionResult.ErrorOutput);
            }

            // Format the output
            var output = executionResult.StandardOutput;
            if (!string.IsNullOrEmpty(attachmentName))
            {
                output = $"// {attachmentName}\n{output}";
            }

            // For execution output, we don't apply syntax highlighting
            var result = HighlightedCode.CreateSuccess(
                System.Web.HttpUtility.HtmlEncode(output),
                output,
                Language.CSharp);
                
            result.Metadata["XmlDocId"] = xmlDocId;
            if (!string.IsNullOrEmpty(attachmentName))
            {
                result.Metadata["AttachmentName"] = attachmentName;
            }
            if (executionResult.Duration.HasValue)
            {
                result.Metadata["ExecutionTime"] = executionResult.Duration.Value.TotalMilliseconds.ToString("F2") + "ms";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute and highlight {XmlDocId}", xmlDocId);
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