using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Reflection;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Services.Infrastructure;

namespace MyLittleContentEngine.Services.Content.Roslyn;

internal record CachedCompiledXmlDocId(
    Document Document,
    TextSpan TextSpan,
    SourceText SourceText,
    ISymbol Symbol,
    Assembly Assembly);

/// <summary>
/// Coordinates loading of example assemblies and providing their source code and output.
/// </summary>
public class RoslynExampleCoordinator : IDisposable
{
    private readonly ILogger _logger;
    private readonly MSBuildWorkspace _workspace;
    private readonly AssemblyLoaderService _assemblyLoaderService;
    private readonly IFileSystem _fileSystem;
    private readonly CodeExecutionService _codeExecutionService;
    private readonly LazyAndForgetful<ConcurrentDictionary<string, CachedCompiledXmlDocId>> _roslynCache;
    private readonly HashSet<string> _pendingChanges = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _changeLock = new();
    private volatile bool _isRebuilding;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of the <see cref="RoslynExampleCoordinator"/> class.
    /// </summary>
    /// <param name="options">The options for roslyn highlighting.</param>
    /// <param name="assemblyLoaderService">The assembly loader service.</param>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="codeExecutionService">The code execution service.</param>
    /// <param name="logger">The logger.</param>
    public RoslynExampleCoordinator(RoslynHighlighterOptions options, AssemblyLoaderService assemblyLoaderService, IFileSystem fileSystem,
        CodeExecutionService codeExecutionService, ILogger<RoslynExampleCoordinator> logger)
    {
        _logger = logger;
        Debug.Assert(options.ConnectedSolution != null);
   
        _assemblyLoaderService = assemblyLoaderService;
        _fileSystem = fileSystem;
        _codeExecutionService = codeExecutionService;

        // Set up MSBuild workspace
        if (!MSBuildLocator.IsRegistered)
        {
            var instances = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(instance => instance.Version).ToArray();
            if (instances.Length == 0)
            {
                _logger.LogError("No MSBuild instances found. Make sure .NET SDK is installed.");
                throw new InvalidOperationException("No MSBuild instances found.");
            }

            // Attempt to find an instance related to .NET SDK first
            var sdkInstance = instances.FirstOrDefault(i => i.DiscoveryType == DiscoveryType.DotNetSdk);
            var msBuildInstance = sdkInstance ?? instances.First(); // Fallback to the latest version if no SDK specific one is found

            _logger.LogDebug("MSBuildLocator selected instance: Name='{Name}', Version='{Version}', Path='{Path}', DiscoveryType='{DiscoveryType}'",
                                   msBuildInstance.Name, msBuildInstance.Version, msBuildInstance.MSBuildPath, msBuildInstance.DiscoveryType);
            MSBuildLocator.RegisterInstance(msBuildInstance);
        }
        else
        {
            _logger.LogDebug("MSBuildLocator already registered.");
        }

        _workspace = MSBuildWorkspace.Create();
        _logger.LogDebug("MSBuildWorkspace created");

        _workspace.LoadMetadataForReferencedProjects = true;
        _workspace.WorkspaceFailed += (_, args) =>
        {
            _logger.LogWarning("Workspace load issue: {message} (DiagnosticKind: {kind})",
                args.Diagnostic.Message, args.Diagnostic.Kind);
        };

        _roslynCache =
            new LazyAndForgetful<ConcurrentDictionary<string, CachedCompiledXmlDocId>>(async () =>
            {
                _isRebuilding = true;
                try
                {
                    _logger.LogTrace("Populating XmlDocId cache");
                    var solution = _workspace.CurrentSolution.ProjectIds.Count == 0
                        ? await _workspace.OpenSolutionAsync(options.ConnectedSolution.SolutionPath)
                        : await ApplyPendingChangesToSolutionAsync(_workspace.CurrentSolution);
                    _logger.LogTrace("Solution loaded for XmlDocId cache");
                    var result = await GetAllTypesAndMethodsInSolutionAsync(solution);
                    _logger.LogTrace("XmlDocId cache population complete");
                    return result;
                }
                finally
                {
                    _isRebuilding = false;
                }
            }, TimeSpan.FromMilliseconds(500));
    }

    private async Task<Assembly> GetProjectAssembly(Project project)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogTrace("Getting compilation for {project}", project.FilePath);

        var compilation = await project.GetCompilationAsync();
        if (compilation == null)
        {
            _logger.LogWarning("Compilation is null for {project}", project.FilePath);
            throw new Exception($"Could not get compilation for {project.FilePath}");
        }

        // Check for source texts without encoding and prevent debug info emission in those cases
        var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded);
        var hasSourcesWithoutEncoding = false;
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var sourceText = await syntaxTree.GetTextAsync();
            if (sourceText.Encoding == null)
            {
                hasSourcesWithoutEncoding = true;
                _logger.LogWarning("Source file {path} has no encoding information", syntaxTree.FilePath);
            }
        }

        if (hasSourcesWithoutEncoding)
        {
            _logger.LogWarning("Disabling embedded debug information due to source texts without encoding");
            emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.Pdb);
        }

        _logger.LogTrace("Compiling assembly for {assemblyName}", project.Name);
        var assembly = await _assemblyLoaderService.GetProjectAssembly(project, emitOptions);
        sw.Stop();
        _logger.LogDebug("Build project {project} in {elapsed}", project.Name, sw.Elapsed);
        return assembly;
    }

    internal bool IsRebuilding => _isRebuilding;

    internal void InvalidateFile(string filePath)
    {
        if (_isRebuilding)
        {
            _logger.LogTrace("Ignoring file change for {filePath} - rebuild in progress", filePath);
            return;
        }

        _logger.LogTrace("InvalidateFile called for {filePath}", filePath);
        _pendingChanges.Add(filePath);
        _assemblyLoaderService.ResetContext();
        _roslynCache.Refresh();
    }

    internal async Task<string> GetCodeFragmentAsync(string xmlDocId, bool bodyOnly)
    {
        _logger.LogTrace("Getting code fragment for {xmlDocId}", xmlDocId);

        var data = await _roslynCache.Value;
        if (data.TryGetValue(xmlDocId, out var id))
        {
            _logger.LogTrace("Code fragment found in cache for {xmlDocId}", xmlDocId);
            return await CodeFragmentExtractor.ExtractCodeFragmentAsync(id.Document, id.TextSpan, id.SourceText,
                bodyOnly);
        }
        
        _logger.LogWarning("Failed to find {sanitizedXmlDocId}", xmlDocId);
        return "Code not found for specified documentation ID.";
    }

    internal async Task<string> GetCodeResultAsync(string xmlDocId, string? attachmentName = null)
    {
        var data = await _roslynCache.Value;
        if (!data.TryGetValue(xmlDocId, out var id))
        {
            return "";
        }


        var syntaxTree = await id.Document.GetSyntaxTreeAsync();
        if (syntaxTree == null)
        {
            _logger.LogWarning("Failed to get syntax tree for {xmlDocId}", xmlDocId);
            return "Failed to get syntax tree for method.";
        }

        var semanticModel = await id.Document.GetSemanticModelAsync();
        if (semanticModel == null)
        {
            _logger.LogWarning("Failed to get semantic model for {xmlDocId}", xmlDocId);
            return "Failed to get semantic model for method.";
        }

        var result = _codeExecutionService.ExecuteMethod(id.Assembly, (IMethodSymbol) id.Symbol);
        return result[attachmentName ?? string.Empty];
    }

    private async Task<Solution> ApplyPendingChangesToSolutionAsync(Solution solution)
    {
        List<string> changedFiles;
        lock (_changeLock)
        {
            _logger.LogTrace("Applying {count} pending changes.", _pendingChanges.Count);
            changedFiles = _pendingChanges.ToList();
            _pendingChanges.Clear();
        }

        foreach (var filePath in changedFiles)
        {
            var docIds = solution.GetDocumentIdsWithFilePath(filePath).ToList();

            if (_fileSystem.File.Exists(filePath))
            {
                _logger.LogTrace("Reading file for update: {filePath}", filePath);
                var fileContent = await _fileSystem.File.ReadAllTextAsync(filePath);
                var text = SourceText.From(fileContent, System.Text.Encoding.UTF8);
                if (docIds.Count == 0)
                {
                    // New file: try to add to the correct project
                    var fileDir = _fileSystem.Path.GetDirectoryName(filePath)!;

                    var project = solution.Projects.FirstOrDefault(p =>
                        p.Documents.Any(d => d.FilePath != null && _fileSystem.Path.GetDirectoryName(d.FilePath) != null && fileDir.Equals(_fileSystem.Path.GetDirectoryName(d.FilePath), StringComparison.OrdinalIgnoreCase))
                        || (p.FilePath != null && _fileSystem.Path.GetDirectoryName(p.FilePath) is { } projDir && fileDir.StartsWith(projDir, StringComparison.OrdinalIgnoreCase)));

                    if (project != null)
                    {
                        var docName = _fileSystem.Path.GetFileName(filePath);
                        _logger.LogTrace("Adding new document {docName} to project {project}", docName,
                            project.Name);
                        var newDocId = DocumentId.CreateNewId(project.Id);
                        solution = solution.AddDocument(newDocId, docName, text, filePath: filePath);
                    }
                    else
                    {
                        _logger.LogWarning("Could not find project for new file: {filePath}", filePath);
                    }
                }
                else
                {
                    // Existing file: update text
                    foreach (var docId in docIds)
                    {
                        solution = solution.WithDocumentText(docId, text);
                    }
                }
            }
            else
            {
                // File does not exist: remove document(s)
                foreach (var docId in docIds)
                {
                    _logger.LogTrace("Removing document for missing file: {filePath}", filePath);
                    solution = solution.RemoveDocument(docId);
                }
            }
        }

        _workspace.TryApplyChanges(solution);
        _logger.LogTrace("ApplyPendingChangesToSolutionAsync finished");
        return solution;
    }

    private async Task<ConcurrentDictionary<string, CachedCompiledXmlDocId>>
        GetAllTypesAndMethodsInSolutionAsync(Solution solution)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogTrace("GetAllTypesAndMethodsInSolutionAsync started");
        var result = new ConcurrentDictionary<string, CachedCompiledXmlDocId>();

        await Parallel.ForEachAsync(solution.Projects, async (project, ctx) =>
        {
            var withoutAnalyzers = project.WithAnalyzerReferences(ImmutableArray<AnalyzerReference>.Empty);

            _logger.LogTrace("Getting types and methods {project}", project.FilePath);
            if (project.FilePath?.Contains("blog-projects") != true)
            {
                _logger.LogTrace("Skipping {project}", project.FilePath);
                return;
            }

            var assembly = await GetProjectAssembly(withoutAnalyzers);

            await foreach (var item in ProcessProjectDocumentsAsync(withoutAnalyzers).WithCancellation(ctx))
            {
                _logger.LogTrace("Adding XmlDocId {xmlDocId} for project {project}", item.xmlDocId,
                    project.FilePath);
                result.TryAdd(item.xmlDocId, new CachedCompiledXmlDocId(item.document, item.textSpan, item.sourceText, item.symbol, assembly));
            }
        });

        sw.Stop();
        _logger.LogTrace("Rebuilt roslyn cache in {elapsed}", sw.Elapsed);
        return result;
    }

    private async
        IAsyncEnumerable<(string xmlDocId, ISymbol symbol, Document document, TextSpan textSpan, SourceText sourceText)>
        ProcessProjectDocumentsAsync(Project project)
    {
        _logger.LogTrace("ProcessProjectDocumentsAsync started for {project}", project.FilePath);
        foreach (var document in project.Documents)
        {
            // Skip documents that aren't C# code files
            if (!document.SupportsSyntaxTree)
                continue;

            _logger.LogTrace("Processing document {document}", document.FilePath);
            var syntaxTree = await document.GetSyntaxTreeAsync() ?? throw new NullReferenceException();
            var semanticModel = await document.GetSemanticModelAsync() ?? throw new NullReferenceException();
            var sourceText = await document.GetTextAsync();
            var rootSyntaxNode = await syntaxTree.GetRootAsync();

            foreach (var type in ProcessTypeDeclarationsAsync(document, rootSyntaxNode, semanticModel, sourceText))
            {
                _logger.LogTrace("Yielding type XmlDocId {xmlDocId} in {document}", type.xmlDocId,
                    document.FilePath);
                yield return type;
            }

            foreach (var method in ProcessMethodDeclarationsAsync(document, rootSyntaxNode, semanticModel, sourceText))
            {
                _logger.LogTrace("Yielding method XmlDocId {xmlDocId} in {document}", method.xmlDocId,
                    document.FilePath);
                yield return method;
            }
        }

        _logger.LogTrace("ProcessProjectDocumentsAsync finished for {project}", project.FilePath);
    }

    private static IEnumerable<(string xmlDocId, ISymbol, Document document, TextSpan textSpan, SourceText sourceText)>
        ProcessTypeDeclarationsAsync(Document document, SyntaxNode rootSyntaxNode,
            SemanticModel semanticModel, SourceText sourceText)
    {
        // Get all type declarations (classes, structs, interfaces, etc.)
        var typeDeclarations = rootSyntaxNode.DescendantNodes()
            .OfType<TypeDeclarationSyntax>();

        foreach (var typeDeclaration in typeDeclarations)
        {
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration);
            if (typeSymbol == null) continue;

            var xmlDocId = typeSymbol.GetDocumentationCommentId();
            if (string.IsNullOrEmpty(xmlDocId)) continue;

            var textSpan = CreateExtendedTextSpan(typeDeclaration);
            yield return (xmlDocId, typeSymbol, document, textSpan, sourceText);
        }
    }

    private static IEnumerable<(string xmlDocId, ISymbol symbol, Document document, TextSpan textSpan, SourceText
            sourceText)>
        ProcessMethodDeclarationsAsync(Document document, SyntaxNode rootSyntaxNode,
            SemanticModel semanticModel, SourceText sourceText)
    {
        // Get all method declarations
        var methodDeclarations = rootSyntaxNode.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var methodDeclaration in methodDeclarations)
        {
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
            if (methodSymbol == null) continue;

            var xmlDocId = methodSymbol.GetDocumentationCommentId();
            if (string.IsNullOrEmpty(xmlDocId)) continue;

            var textSpan = CreateExtendedTextSpan(methodDeclaration);

            // Add to the dictionary if not already present
            yield return (xmlDocId, methodSymbol, document, textSpan, sourceText);
        }
    }

    private static TextSpan CreateExtendedTextSpan(CSharpSyntaxNode syntaxNode)
    {
        // Get the leading trivia and find whitespace
        var leadingTrivia = syntaxNode.GetLeadingTrivia();
        var leadingWhitespaceLength = leadingTrivia
            .Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia))
            .Sum(t => t.Span.Length);

        // Create a new text span that includes the leading whitespace
        var extendedStartPosition = syntaxNode.SpanStart - leadingWhitespaceLength;
        var extendedLength = syntaxNode.Span.Length + leadingWhitespaceLength;

        return new TextSpan(extendedStartPosition, extendedLength);
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
            _roslynCache.Dispose();
            _workspace.Dispose();
        }

        _disposed = true;
    }

    /// <inheritdoc />
    ~RoslynExampleCoordinator()
    {
        Dispose(false);
    }
}