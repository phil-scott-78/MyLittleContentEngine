using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Reflection;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Services.Infrastructure;

namespace MyLittleContentEngine.Services.Content.Roslyn;

internal record CachedXmlDocId(
    Document Document,
    TextSpan TextSpan,
    SourceText SourceText,
    ISymbol Symbol,
    Lazy<Task<Assembly>> LazyAssembly);

public interface IRoslynExampleCoordinator : IDisposable
{
    Task<string> GetCodeFragmentAsync(string xmlDocId, bool bodyOnly);
    Task<string> GetCodeResultAsync(string xmlDocId, string? attachmentName = null);

    /// <summary>
    /// Gets all symbols from the workspace for API documentation generation
    /// </summary>
    /// <returns>A dictionary of XmlDocId to symbol information</returns>
    Task<IReadOnlyDictionary<string, (ISymbol Symbol, Document Document, Assembly Assembly)>> GetAllSymbolsAsync();

    void InvalidateFile(string filePath);
}

/// <summary>
/// Coordinates loading of example assemblies and providing their source code and output.
/// </summary>
internal class RoslynExampleCoordinator : IRoslynExampleCoordinator
{
    private readonly ILogger _logger;
    private readonly MSBuildWorkspace _workspace;
    private readonly AssemblyLoaderService _assemblyLoaderService;
    private readonly IFileSystem _fileSystem;
    private readonly CodeExecutionService _codeExecutionService;
    private readonly LazyAndForgetful<ConcurrentDictionary<string, CachedXmlDocId>> _roslynCache;
    private readonly ConcurrentDictionary<string, Lazy<Task<Assembly>>> _projectAssemblyCache = new();
    private readonly HashSet<string> _pendingChanges = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _changeLock = new();
    private readonly ConnectedDotNetSolution _connectedSolution;
    private volatile bool _isRebuilding;
    private bool _disposed;
    private readonly string _tempBuildDirectory;

    /// <summary>
    /// Creates a new instance of the <see cref="RoslynExampleCoordinator"/> class.
    /// </summary>
    /// <param name="options">The options for roslyn highlighting.</param>
    /// <param name="assemblyLoaderService">The assembly loader service.</param>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="codeExecutionService">The code execution service.</param>
    /// <param name="logger">The logger.</param>
    public RoslynExampleCoordinator(RoslynHighlighterOptions options, AssemblyLoaderService assemblyLoaderService,
        IFileSystem fileSystem,
        CodeExecutionService codeExecutionService, ILogger<RoslynExampleCoordinator> logger)
    {
        _logger = logger;
        Debug.Assert(options.ConnectedSolution != null);
        _connectedSolution = options.ConnectedSolution;

        // Validate project filter options
        if (_connectedSolution is { IncludedProjects.Length: > 0, ExcludedProjects.Length: > 0 })
        {
            throw new InvalidOperationException(
                "Cannot specify both IncludedProjects and ExcludedProjects. Use one or the other, or neither for all projects.");
        }

        _assemblyLoaderService = assemblyLoaderService;
        _fileSystem = fileSystem;
        _codeExecutionService = codeExecutionService;

        // Set up MSBuild workspace
        if (!MSBuildLocator.IsRegistered)
        {
            var instances = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(instance => instance.Version)
                .ToArray();
            if (instances.Length == 0)
            {
                _logger.LogError("No MSBuild instances found. Make sure .NET SDK is installed.");
                throw new InvalidOperationException("No MSBuild instances found.");
            }

            // Attempt to find an instance related to .NET SDK first
            var sdkInstance = instances.FirstOrDefault(i => i.DiscoveryType == DiscoveryType.DotNetSdk);
            var msBuildInstance =
                sdkInstance ?? instances.First(); // Fallback to the latest version if no SDK specific one is found

            _logger.LogDebug(
                "MSBuildLocator selected instance: Name='{Name}', Version='{Version}', Path='{Path}', DiscoveryType='{DiscoveryType}'",
                msBuildInstance.Name, msBuildInstance.Version, msBuildInstance.MSBuildPath,
                msBuildInstance.DiscoveryType);
            MSBuildLocator.RegisterInstance(msBuildInstance);
        }
        else
        {
            _logger.LogDebug("MSBuildLocator already registered.");
        }

        // Create a temporary directory for build outputs
        _tempBuildDirectory = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "MyLittleContentEngine",
            Guid.NewGuid().ToString("N"));
        _fileSystem.Directory.CreateDirectory(_tempBuildDirectory);

        // Configure MSBuild properties to use temp directory for compilation outputs only
        // We use OutputPath and IntermediateOutputPath rather than Base* to preserve reference resolution
        var properties = new Dictionary<string, string>
        {
            // Redirect only the final output directory for this specific build
            ["OutputPath"] = _fileSystem.Path.Combine(_tempBuildDirectory, "bin") +
                             _fileSystem.Path.DirectorySeparatorChar,
            // Redirect intermediate files to temp location
            ["IntermediateOutputPath"] = _fileSystem.Path.Combine(_tempBuildDirectory, "obj") +
                                         _fileSystem.Path.DirectorySeparatorChar,
            // Optimize for faster loading
            ["DesignTimeBuild"] = "true",
            ["SkipCompilerExecution"] = "true"
        };

        _workspace = MSBuildWorkspace.Create(properties);
        _logger.LogDebug("MSBuildWorkspace created with temp build directory: {TempBuildDir}", _tempBuildDirectory);

        _workspace.WorkspaceFailed += (_, args) =>
        {
            _logger.LogWarning("Workspace load issue: {message} (DiagnosticKind: {kind})",
                args.Diagnostic.Message, args.Diagnostic.Kind);
        };

        _roslynCache =
            new LazyAndForgetful<ConcurrentDictionary<string, CachedXmlDocId>>(async () =>
            {
                _isRebuilding = true;
                try
                {
                    _logger.LogTrace("Populating XmlDocId cache");
                    var solution = _workspace.CurrentSolution.ProjectIds.Count == 0
                        ? await _workspace.OpenSolutionAsync(_connectedSolution.SolutionPath)
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

        var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.Pdb);

        _logger.LogTrace("Compiling assembly for {assemblyName}", project.Name);
        var assembly = await _assemblyLoaderService.GetProjectAssembly(project, emitOptions);
        sw.Stop();
        _logger.LogDebug("Build project {project} in {elapsed}", project.Name, sw.Elapsed);
        return assembly;
    }

    private Lazy<Task<Assembly>> GetOrCreateLazyAssembly(Project project)
    {
        var projectKey = project.Id.ToString();
        return _projectAssemblyCache.GetOrAdd(projectKey,
            _ => new Lazy<Task<Assembly>>(() => GetProjectAssembly(project)));
    }

    internal bool IsRebuilding => _isRebuilding;

    public void InvalidateFile(string filePath)
    {
        if (_isRebuilding)
        {
            _logger.LogTrace("Ignoring file change for {filePath} - rebuild in progress", filePath);
            return;
        }

        _logger.LogTrace("InvalidateFile called for {filePath}", filePath);
        _pendingChanges.Add(filePath);
        _assemblyLoaderService.ResetContext();
        _projectAssemblyCache.Clear(); // Clear lazy assembly cache
        _roslynCache.Refresh();
    }

    public async Task<string> GetCodeFragmentAsync(string xmlDocId, bool bodyOnly)
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

    public async Task<string> GetCodeResultAsync(string xmlDocId, string? attachmentName = null)
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

        var assembly = await id.LazyAssembly.Value;
        var result = _codeExecutionService.ExecuteMethod(assembly, (IMethodSymbol)id.Symbol);
        return result[attachmentName ?? string.Empty];
    }

    /// <summary>
    /// Gets all symbols from the workspace for API documentation generation
    /// </summary>
    /// <returns>A dictionary of XmlDocId to symbol information</returns>
    public async Task<IReadOnlyDictionary<string, (ISymbol Symbol, Document Document, Assembly Assembly)>>
        GetAllSymbolsAsync()
    {
        var data = await _roslynCache.Value;
        var result = new Dictionary<string, (ISymbol Symbol, Document Document, Assembly Assembly)>();

        foreach (var kvp in data)
        {
            var assembly = await kvp.Value.LazyAssembly.Value;
            result[kvp.Key] = (kvp.Value.Symbol, kvp.Value.Document, assembly);
        }

        return result;
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
                    {
                        // Check if any document in the project shares the same directory.
                        if (p.Documents.Any(d =>
                                d.FilePath != null && fileDir.Equals(_fileSystem.Path.GetDirectoryName(d.FilePath),
                                    StringComparison.OrdinalIgnoreCase)))
                        {
                            return true;
                        }

                        // Check if the file is within the project's directory structure.
                        if (p.FilePath == null) return false;

                        var projDir = _fileSystem.Path.GetDirectoryName(p.FilePath);

                        return projDir != null && fileDir.StartsWith(projDir, StringComparison.OrdinalIgnoreCase);
                    });

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

    private async Task<ConcurrentDictionary<string, CachedXmlDocId>>
        GetAllTypesAndMethodsInSolutionAsync(Solution solution)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogTrace("GetAllTypesAndMethodsInSolutionAsync started");
        var result = new ConcurrentDictionary<string, CachedXmlDocId>();

        var filteredProjects = FilterProjects(solution.Projects, _connectedSolution).ToArray();
        _logger.LogTrace("Processing {ProjectCount} projects after filtering", filteredProjects.Length);

        await Parallel.ForEachAsync(filteredProjects, async (project, _) =>
        {
            _logger.LogTrace("Getting types and methods {project}", project.FilePath);

            var lazyAssembly = GetOrCreateLazyAssembly(project);

            foreach (var item in await ProcessProjectDocumentsAsync(project))
            {
                result.TryAdd(item.xmlDocId,
                    new CachedXmlDocId(item.document, item.textSpan, item.sourceText, item.symbol, lazyAssembly));
            }
        });

        sw.Stop();
        _logger.LogTrace("Rebuilt roslyn cache in {elapsed}", sw.Elapsed);
        return result;
    }

    private static IEnumerable<Project> FilterProjects(
        IEnumerable<Project> projects,
        ConnectedDotNetSolution solution)
    {
        var projectList = projects.ToList();

        // If neither included nor excluded is specified, return all projects
        if (solution.IncludedProjects.Length == 0 && solution.ExcludedProjects.Length == 0)
        {
            return projectList;
        }

        // If included projects are specified, return only those
        if (solution.IncludedProjects.Length > 0)
        {
            var includedSet = new HashSet<string>(solution.IncludedProjects, StringComparer.OrdinalIgnoreCase);
            return projectList.Where(p => includedSet.Contains(p.Name));
        }

        // If excluded projects are specified, return all except those
        var excludedSet = new HashSet<string>(solution.ExcludedProjects, StringComparer.OrdinalIgnoreCase);
        return projectList.Where(p => !excludedSet.Contains(p.Name));
    }

    private async Task<IEnumerable<(string xmlDocId, ISymbol symbol, Document document, TextSpan textSpan, SourceText
            sourceText)>>
        ProcessProjectDocumentsAsync(Project project)
    {
        ConcurrentBag<(string xmlDocId, ISymbol symbol, Document document, TextSpan textSpan, SourceText sourceText)>
            allValues = [];
        _logger.LogTrace("ProcessProjectDocumentsAsync started for {project}", project.FilePath);
        await Parallel.ForEachAsync(project.Documents, async (document, token) =>
        {
            // Skip documents that aren't C# code files
            if (!document.SupportsSyntaxTree)
                return;

            _logger.LogTrace("Processing document {document}", document.FilePath);
            var syntaxTree = await document.GetSyntaxTreeAsync(token) ?? throw new NullReferenceException();
            var semanticModel = await document.GetSemanticModelAsync(token) ?? throw new NullReferenceException();
            var sourceText = await document.GetTextAsync(token);
            var rootSyntaxNode = await syntaxTree.GetRootAsync(token);
            
            var fileName = _fileSystem.Path.GetFileName(document.FilePath);
            foreach (var type in ProcessTypeDeclarationsAsync(document, rootSyntaxNode, semanticModel, sourceText))
            {
                _logger.LogTrace("Adding type XmlDocId {xmlDocId} in {document}", type.xmlDocId,
                    fileName);
                allValues.Add(type);
            }
        });

        _logger.LogTrace("ProcessProjectDocumentsAsync finished for {project}", project.FilePath);
        return allValues;
    }

    private  IEnumerable<(string xmlDocId, ISymbol, Document document, TextSpan textSpan, SourceText sourceText)>
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
            if (typeSymbol.DeclaredAccessibility != Accessibility.Public) continue;

            var xmlDocId = typeSymbol.GetDocumentationCommentId();
            if (string.IsNullOrEmpty(xmlDocId)) continue;

            var textSpan = CreateExtendedTextSpan(typeDeclaration);
            yield return (xmlDocId, typeSymbol, document, textSpan, sourceText);

            
            foreach (var method in ProcessMethodDeclarationsAsync(document, typeDeclaration, semanticModel, sourceText))
            {
                yield return method;
            }

            foreach (var property in ProcessPropertyDeclarationsAsync(document, typeDeclaration, semanticModel,
                         sourceText))
            {
                yield return property;
            }

            foreach (var field in ProcessFieldDeclarationsAsync(document, typeDeclaration, semanticModel, sourceText))
            {
                yield return field;
            }

            foreach (var eventItem in
                     ProcessEventDeclarationsAsync(document, typeDeclaration, semanticModel, sourceText))
            {
                yield return eventItem;
            }
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
            if (methodSymbol.DeclaredAccessibility != Accessibility.Public) continue; // Skip non-public methods

            var xmlDocId = methodSymbol.GetDocumentationCommentId();
            if (string.IsNullOrEmpty(xmlDocId)) continue;

            var textSpan = CreateExtendedTextSpan(methodDeclaration);

            // Add to the dictionary if not already present
            yield return (xmlDocId, methodSymbol, document, textSpan, sourceText);
        }
    }

    private static IEnumerable<(string xmlDocId, ISymbol symbol, Document document, TextSpan textSpan, SourceText
            sourceText)>
        ProcessPropertyDeclarationsAsync(Document document, SyntaxNode rootSyntaxNode,
            SemanticModel semanticModel, SourceText sourceText)
    {
        // Get all property declarations
        var propertyDeclarations = rootSyntaxNode.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>();

        foreach (var propertyDeclaration in propertyDeclarations)
        {
            var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration);
            if (propertySymbol == null) continue;
            if (propertySymbol.DeclaredAccessibility != Accessibility.Public) continue; 

            var xmlDocId = propertySymbol.GetDocumentationCommentId();
            if (string.IsNullOrEmpty(xmlDocId)) continue;

            var textSpan = CreateExtendedTextSpan(propertyDeclaration);
            yield return (xmlDocId, propertySymbol, document, textSpan, sourceText);
        }
    }

    private static IEnumerable<(string xmlDocId, ISymbol symbol, Document document, TextSpan textSpan, SourceText
            sourceText)>
        ProcessFieldDeclarationsAsync(Document document, SyntaxNode rootSyntaxNode,
            SemanticModel semanticModel, SourceText sourceText)
    {
        // Get all field declarations
        var fieldDeclarations = rootSyntaxNode.DescendantNodes()
            .OfType<FieldDeclarationSyntax>();

        foreach (var fieldDeclaration in fieldDeclarations)
        {
            // A field declaration can have multiple variables
            foreach (var variable in fieldDeclaration.Declaration.Variables)
            {
                var fieldSymbol = semanticModel.GetDeclaredSymbol(variable);
                if (fieldSymbol == null) continue;
                if (fieldSymbol.DeclaredAccessibility != Accessibility.Public) continue; 

                var xmlDocId = fieldSymbol.GetDocumentationCommentId();
                if (string.IsNullOrEmpty(xmlDocId)) continue;

                var textSpan = CreateExtendedTextSpan(fieldDeclaration);
                yield return (xmlDocId, fieldSymbol, document, textSpan, sourceText);
            }
        }
    }

    private static IEnumerable<(string xmlDocId, ISymbol symbol, Document document, TextSpan textSpan, SourceText
            sourceText)>
        ProcessEventDeclarationsAsync(Document document, SyntaxNode rootSyntaxNode,
            SemanticModel semanticModel, SourceText sourceText)
    {
        // Get all event declarations
        var eventDeclarations = rootSyntaxNode.DescendantNodes()
            .OfType<EventDeclarationSyntax>();

        foreach (var eventDeclaration in eventDeclarations)
        {
            var eventSymbol = semanticModel.GetDeclaredSymbol(eventDeclaration);
            if (eventSymbol == null) continue;
            if (eventSymbol.DeclaredAccessibility != Accessibility.Public) continue;

            var xmlDocId = eventSymbol.GetDocumentationCommentId();
            if (string.IsNullOrEmpty(xmlDocId)) continue;

            var textSpan = CreateExtendedTextSpan(eventDeclaration);
            yield return (xmlDocId, eventSymbol, document, textSpan, sourceText);
        }

        // Also handle event field declarations
        var eventFieldDeclarations = rootSyntaxNode.DescendantNodes()
            .OfType<EventFieldDeclarationSyntax>();

        foreach (var eventFieldDeclaration in eventFieldDeclarations)
        {
            foreach (var variable in eventFieldDeclaration.Declaration.Variables)
            {
                var eventSymbol = semanticModel.GetDeclaredSymbol(variable);
                if (eventSymbol == null) continue;

                var xmlDocId = eventSymbol.GetDocumentationCommentId();
                if (string.IsNullOrEmpty(xmlDocId)) continue;

                var textSpan = CreateExtendedTextSpan(eventFieldDeclaration);
                yield return (xmlDocId, eventSymbol, document, textSpan, sourceText);
            }
        }
    }

    private static TextSpan CreateExtendedTextSpan(CSharpSyntaxNode syntaxNode)
    {
        // Get the leading trivia and find whitespace
        var leadingTrivia = syntaxNode.GetLeadingTrivia();

        var leadingWhitespaceLength = 0;
        foreach (var trivia in leadingTrivia.Reverse())
        {
            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                leadingWhitespaceLength += trivia.Span.Length;
            }
            else
            {
                break;
            }
        }

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
            _projectAssemblyCache.Clear();

            // Clean up temporary build directory
            try
            {
                if (_fileSystem.Directory.Exists(_tempBuildDirectory))
                {
                    _fileSystem.Directory.Delete(_tempBuildDirectory, recursive: true);
                    _logger.LogDebug("Cleaned up temp build directory: {TempBuildDir}", _tempBuildDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up temp build directory: {TempBuildDir}", _tempBuildDirectory);
            }
        }

        _disposed = true;
    }

    /// <inheritdoc />
    ~RoslynExampleCoordinator()
    {
        Dispose(false);
    }
}