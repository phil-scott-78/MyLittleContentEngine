using System.IO.Abstractions;
using System.Collections.Concurrent;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Services.Content.CodeAnalysis.Configuration;
using MyLittleContentEngine.Services.Content.CodeAnalysis.Extensions;
using MyLittleContentEngine.Services.Content.CodeAnalysis.SolutionWorkspace;
using MyLittleContentEngine.Services.Infrastructure;

namespace MyLittleContentEngine.Services.Content.CodeAnalysis.SymbolAnalysis;

/// <summary>
/// Implementation of ISymbolExtractionService for extracting symbols from Roslyn solutions
/// </summary>
internal class SymbolExtractionService : ISymbolExtractionService
{
    private readonly ILogger<SymbolExtractionService> _logger;
    private readonly ISolutionWorkspaceService _workspaceService;
    private readonly CodeAnalysisOptions _options;
    private readonly LazyAndForgetful<IReadOnlyDictionary<string, SymbolInfo>> _lazySymbols;

    public SymbolExtractionService(ISolutionWorkspaceService workspaceService, CodeAnalysisOptions options,
        ILogger<SymbolExtractionService> logger, IFileSystem fileSystem, IContentEngineFileWatcher? fileWatcher = null)
    {
        _workspaceService = workspaceService;
        _options = options;
        _logger = logger;

        // Initialize lazy loading for symbols
        _lazySymbols = new LazyAndForgetful<IReadOnlyDictionary<string, SymbolInfo>>(
            async () => await LoadAllSymbolsAsync(), TimeSpan.FromMilliseconds(50));

        // Register file watching if available
        if (fileWatcher != null && _options.SolutionPath.HasValue && !_options.SolutionPath.Value.IsEmpty)
        {
            var solutionDir = _options.SolutionPath.Value.GetDirectory();
            if (!solutionDir.IsEmpty)
            {
                fileWatcher.AddPathWatch(solutionDir.Value, "*.cs", InvalidateFile);
            }
        }
    }

    public async Task<IReadOnlyDictionary<string, SymbolInfo>> ExtractSymbolsAsync(Solution solution)
    {
        _logger.LogInformation("Extracting symbols from solution");

        var symbols = new ConcurrentDictionary<string, SymbolInfo>();
        var projects = await _workspaceService.GetProjectsAsync();

        await Parallel.ForEachAsync(projects, async (project, _) =>
        {
            try
            {
                await ExtractProjectSymbols(project, symbols);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract symbols from project {ProjectName}", project.Name);
            }
        });

        _logger.LogInformation("Extracted {Count} symbols from solution", symbols.Count);
        return symbols;
    }

    public async Task<SymbolInfo?> FindSymbolAsync(string xmlDocId)
    {
        var symbols = await _lazySymbols.Value;
        return symbols.GetValueOrDefault(xmlDocId);
    }

    public async Task<string> ExtractCodeFragmentAsync(string xmlDocId, bool bodyOnly = false)
    {
        var symbolInfo = await FindSymbolAsync(xmlDocId);
        if (symbolInfo == null)
        {
            _logger.LogWarning("Symbol not found: {XmlDocId}", xmlDocId);
            return string.Empty;
        }

        try
        {
            // we need to get the syntax root so we can also find any leading whitespace 

            var syntaxRoot = await symbolInfo.Document.GetSyntaxRootAsync();
            if (syntaxRoot == null)
            {
                // theoretically we should always get the syntax root, but if not fallback to highlighting
                // only the symbol
                var backupFragment = await CodeFragmentExtractor.ExtractCodeFragmentAsync(
                    symbolInfo.Document,
                    symbolInfo.TextSpan,
                    symbolInfo.SourceText,
                    bodyOnly);
                
                return TextFormatter.NormalizeIndents(backupFragment);

            }
            var node = syntaxRoot.FindNode(symbolInfo.TextSpan);

            var leadingTrivia = node.GetLeadingTrivia();

            // Filter for only whitespace trivia
            var whitespaceTrivia = leadingTrivia
                .Reverse() // start from closest to the node
                .TakeWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia))
                .Reverse()
                .ToList();

            // Calculate the new span start by subtracting the length of the whitespace trivia
            var extraWhitespaceLength = whitespaceTrivia.Sum(t => t.Span.Length);
            var newStart = node.SpanStart - extraWhitespaceLength;

            // Clamp newStart to avoid going out of bounds
            newStart = Math.Max(0, newStart);

            var extendedSpan = TextSpan.FromBounds(newStart, symbolInfo.TextSpan.End);

            var sourceText = symbolInfo.SourceText;

            var fragment = await CodeFragmentExtractor.ExtractCodeFragmentAsync(
                symbolInfo.Document,
                extendedSpan,
                sourceText,
                bodyOnly);

            return TextFormatter.NormalizeIndents(fragment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract code fragment for {XmlDocId}", xmlDocId);
            return string.Empty;
        }
    }

    public void InvalidateFile(string filePath)
    {
        // Invalidate the lazy loader
        _lazySymbols.Refresh();
    }

    public void ClearCache()
    {
        _lazySymbols.Refresh();
    }

    private async Task<IReadOnlyDictionary<string, SymbolInfo>> LoadAllSymbolsAsync()
    {
        _logger.LogDebug("Loading all symbols from solution");

        var solution = await _workspaceService.LoadSolutionAsync(_options.SolutionPath!.Value.Value);
        return await ExtractSymbolsAsync(solution);
    }

    private async Task ExtractProjectSymbols(Project project, ConcurrentDictionary<string, SymbolInfo> symbols)
    {
        var compilation = await _workspaceService.GetCompilationAsync(project);
        if (compilation == null)
        {
            _logger.LogWarning("Failed to get compilation for project {ProjectName}", project.Name);
            return;
        }

        await Parallel.ForEachAsync(project.Documents, async (document, _) =>
        {
            try
            {
                await ExtractDocumentSymbols(document, compilation, symbols);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract symbols from document {DocumentPath}", document.FilePath);
            }
        });
    }

    private async Task ExtractDocumentSymbols(
        Document document,
        Compilation compilation,
        ConcurrentDictionary<string, SymbolInfo> symbols)
    {
        var syntaxTree = await document.GetSyntaxTreeAsync();
        if (syntaxTree == null)
        {
            return;
        }

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = await syntaxTree.GetRootAsync();
        var sourceText = await document.GetTextAsync();

        // Extract type declarations
        var typeDeclarations = root.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Where(t => t is ClassDeclarationSyntax or InterfaceDeclarationSyntax or StructDeclarationSyntax
                or RecordDeclarationSyntax);

        foreach (var typeDecl in typeDeclarations)
        {
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl);
            if (typeSymbol == null)
            {
                continue;
            }

            AddSymbol(symbols, typeSymbol, document, typeDecl, sourceText);

            // Extract members
            foreach (var member in typeDecl.Members)
            {
                ExtractMemberSymbol(member, semanticModel, document, sourceText, symbols);
            }
        }

        // Extract top-level members (for top-level programs)
        if (root is CompilationUnitSyntax compilationUnit)
        {
            foreach (var member in compilationUnit.Members.OfType<GlobalStatementSyntax>())
            {
                ExtractGlobalStatement(member, semanticModel, document, sourceText, symbols);
            }
        }
    }

    private void ExtractMemberSymbol(
        MemberDeclarationSyntax member,
        SemanticModel semanticModel,
        Document document,
        SourceText sourceText,
        ConcurrentDictionary<string, SymbolInfo> symbols)
    {
        ISymbol? symbol = member switch
        {
            MethodDeclarationSyntax method => semanticModel.GetDeclaredSymbol(method),
            PropertyDeclarationSyntax property => semanticModel.GetDeclaredSymbol(property),
            FieldDeclarationSyntax field => field.Declaration.Variables.FirstOrDefault() is { } variable
                ? semanticModel.GetDeclaredSymbol(variable)
                : null,
            EventDeclarationSyntax evt => semanticModel.GetDeclaredSymbol(evt),
            _ => null
        };

        if (symbol != null)
        {
            AddSymbol(symbols, symbol, document, member, sourceText);
        }
    }

    private void ExtractGlobalStatement(
        GlobalStatementSyntax globalStatement,
        SemanticModel semanticModel,
        Document document,
        SourceText sourceText,
        ConcurrentDictionary<string, SymbolInfo> symbols)
    {
        // Handle top-level methods in top-level programs
        if (globalStatement.Statement is LocalFunctionStatementSyntax localFunction)
        {
            var symbol = semanticModel.GetDeclaredSymbol(localFunction);
            if (symbol != null)
            {
                AddSymbol(symbols, symbol, document, localFunction, sourceText);
            }
        }
    }

    private void AddSymbol(
        ConcurrentDictionary<string, SymbolInfo> symbols,
        ISymbol symbol,
        Document document,
        SyntaxNode syntaxNode,
        SourceText sourceText)
    {
        var xmlDocId = symbol.GetDocumentationCommentId();
        if (string.IsNullOrEmpty(xmlDocId))
        {
            return;
        }

        var xmlDoc = GetXmlDocumentation(symbol);
        var textSpan = syntaxNode.Span;

        var symbolInfo = new SymbolInfo
        {
            Symbol = symbol,
            Document = document,
            SyntaxNode = syntaxNode,
            SourceText = sourceText,
            TextSpan = textSpan,
            XmlDocumentation = xmlDoc,
            Project = document.Project
        };

        symbols.TryAdd(xmlDocId, symbolInfo);
    }

    private static string? GetXmlDocumentation(ISymbol symbol)
    {
        var xmlString = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xmlString))
        {
            return null;
        }

        try
        {
            var xDoc = XDocument.Parse(xmlString);
            var summary = xDoc.Descendants("summary").FirstOrDefault()?.Value.Trim();
            return summary;
        }
        catch
        {
            return null;
        }
    }
}