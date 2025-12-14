using System.Collections.Concurrent;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Services.Content.CodeAnalysis.Configuration;
using MyLittleContentEngine.Services.Infrastructure;

namespace MyLittleContentEngine.Services.Content.CodeAnalysis.SolutionWorkspace;

/// <summary>
/// Implementation of ISolutionWorkspaceService that manages MSBuild workspace operations
/// </summary>
internal class SolutionWorkspaceService : ISolutionWorkspaceService
{
    private readonly ILogger<SolutionWorkspaceService> _logger;
    private readonly CodeAnalysisOptions _options;
    private readonly IContentEngineFileWatcher _fileWatcher;
    private readonly FilePathOperations _filePathOps;
    private readonly Lock _lock = new();
    private readonly string _tempBuildPath;

    private MSBuildWorkspace? _workspace;
    private Solution? _solution;
    private readonly ConcurrentDictionary<ProjectId, Compilation> _compilationCache = new();
    private readonly ConcurrentQueue<string> _pendingUpdates = new();
    private bool _isDisposed;

    static SolutionWorkspaceService()
    {
        // Initialize MSBuild once
        if (!MSBuildLocator.IsRegistered)
        {
            var instance = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(x => x.Version).FirstOrDefault() ?? MSBuildLocator.RegisterDefaults();
            MSBuildLocator.RegisterInstance(instance);
        }
    }

    public SolutionWorkspaceService(
        CodeAnalysisOptions options,
        ILogger<SolutionWorkspaceService> logger,
        IContentEngineFileWatcher fileWatcher,
        FilePathOperations filePathOps)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
        _filePathOps = filePathOps ?? throw new ArgumentNullException(nameof(filePathOps));

        if (!_options.SolutionPath.HasValue || _options.SolutionPath.Value.IsEmpty)
        {
            throw new ArgumentException("Solution path must be specified in options", nameof(options));
        }

        // Create temp folder for build artifacts
        _tempBuildPath = Path.Combine(
            Path.GetTempPath(), 
            $"MyLittleContentEngine_Build_{Guid.NewGuid():N}"
        );
        Directory.CreateDirectory(_tempBuildPath);
        _logger.LogDebug("Created temp build folder: {Path}", _tempBuildPath);

        // Register for file watching
        RegisterFileWatching();
    }

    public async Task<Solution> LoadSolutionAsync(string solutionPath)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(SolutionWorkspaceService));

        lock (_lock)
        {
            if (_solution != null && _workspace != null)
            {
                ApplyPendingUpdates();
                return _solution;
            }
        }

        _logger.LogDebug("Loading solution from {SolutionPath}", solutionPath);

        // Configure MSBuild properties to use temp folder for build artifacts
        var properties = new Dictionary<string, string>
        {
            ["BaseIntermediateOutputPath"] = Path.Combine(_tempBuildPath, "obj") + Path.DirectorySeparatorChar,
            ["IntermediateOutputPath"] = Path.Combine(_tempBuildPath, "obj", "$(Configuration)") + Path.DirectorySeparatorChar,
            ["OutputPath"] = Path.Combine(_tempBuildPath, "bin", "$(Configuration)") + Path.DirectorySeparatorChar
        };

        var workspace = MSBuildWorkspace.Create(properties);
        workspace.RegisterWorkspaceFailedHandler(args =>
        {
            _logger.LogWarning("Workspace failed: {Diagnostic}", args.Diagnostic);

        });
        
        try
        {
            var solution = await workspace.OpenSolutionAsync(solutionPath);

            lock (_lock)
            {
                _workspace?.Dispose();
                _workspace = workspace;
                _solution = solution;
                _compilationCache.Clear();
            }

            _logger.LogDebug("Successfully loaded solution with {ProjectCount} projects", solution.Projects.Count());
            return solution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load solution from {SolutionPath}", solutionPath);
            workspace.Dispose();
            throw;
        }
    }

    public async Task<IEnumerable<Project>> GetProjectsAsync(Func<Project, bool>? filter = null)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(SolutionWorkspaceService));

        var solution = await LoadSolutionAsync(_options.SolutionPath!.Value.Value);
        var projects = solution.Projects;

        if (filter != null)
        {
            projects = projects.Where(filter);
        }

        // Apply configured filters
        if (_options.ProjectFilter != null)
        {
            projects = ApplyProjectFilter(projects, _options.ProjectFilter);
        }

        return projects.ToList();
    }

    public async Task<Compilation?> GetCompilationAsync(Project project)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(SolutionWorkspaceService));

        if (_compilationCache.TryGetValue(project.Id, out var cachedCompilation))
        {
            _logger.LogTrace("Compilation cache HIT for project {ProjectName} ({ProjectId})", project.Name, project.Id);
            return cachedCompilation;
        }

        _logger.LogTrace("Compilation cache MISS for project {ProjectName} ({ProjectId}) - compiling", project.Name, project.Id);

        try
        {
            _logger.LogDebug("Compiling project {ProjectName}", project.Name);
            var compilation = await project.GetCompilationAsync();

            if (compilation != null)
            {
                _compilationCache.TryAdd(project.Id, compilation);
                _logger.LogTrace("Compilation cached for project {ProjectName} ({ProjectId})", project.Name, project.Id);
            }

            return compilation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compile project {ProjectName}", project.Name);
            return null;
        }
    }

    public void InvalidateSolution()
    {
        _logger.LogWarning("InvalidateSolution called - THIS WILL CLEAR {QueuedCount} PENDING UPDATES!", _pendingUpdates.Count);

        lock (_lock)
        {
            var cachedProjectCount = _compilationCache.Count;
            var queuedCount = _pendingUpdates.Count;

            _logger.LogTrace("Invalidating solution cache (clearing {Count} cached compilations, {QueuedCount} queued updates)",
                cachedProjectCount, queuedCount);

            _solution = null;
            _workspace?.Dispose();
            _workspace = null;
            _compilationCache.Clear();

            // Clear pending updates - they're no longer relevant
            _pendingUpdates.Clear();

            _logger.LogTrace("Solution cache invalidated, workspace disposed");
        }
    }

    private void UpdateDocument(string filePath)
    {
        _logger.LogTrace("UpdateDocument called for {FilePath}", filePath);

        // Simply enqueue the file path for deferred processing
        _pendingUpdates.Enqueue(filePath);

        _logger.LogTrace("Enqueued document update for {FilePath} (queue depth: {Count})",
            filePath, _pendingUpdates.Count);
    }

    private void ApplyPendingUpdates()
    {
        // IMPORTANT: Must be called within _lock

        if (_solution == null)
        {
            _logger.LogTrace("No solution loaded, clearing pending updates queue");
            _pendingUpdates.Clear();
            return;
        }

        if (_pendingUpdates.IsEmpty)
        {
            return; // Fast path - no updates pending
        }

        _logger.LogTrace("Applying {Count} pending document updates", _pendingUpdates.Count);

        // Step 1: Dequeue all pending updates and deduplicate by file path
        var updatesByPath = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        while (_pendingUpdates.TryDequeue(out var filePath))
        {
            updatesByPath[filePath] = true; // Last write wins (latest entry kept)
        }

        _logger.LogTrace("Deduplicated to {Count} unique file(s)", updatesByPath.Count);

        // Step 2: Apply updates to solution
        var updatedSolution = _solution;
        var invalidatedProjects = new HashSet<ProjectId>();
        var successCount = 0;

        foreach (var filePath in updatesByPath.Keys)
        {
            try
            {
                // Find documents for this file path
                var documentIds = _solution.GetDocumentIdsWithFilePath(filePath);

                if (documentIds.IsEmpty)
                {
                    _logger.LogTrace("File {FilePath} not found in solution during deferred update", filePath);
                    continue;
                }

                // Read file content with sharing enabled (file may be locked by editor)
                using var fileStream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8);
                var fileContent = reader.ReadToEnd();
                var newText = Microsoft.CodeAnalysis.Text.SourceText.From(
                    fileContent, System.Text.Encoding.UTF8);

                // Apply update to all documents with this file path
                foreach (var docId in documentIds)
                {
                    var document = _solution.GetDocument(docId);
                    var projectName = document?.Project.Name ?? "Unknown";

                    _logger.LogTrace("Applying deferred update to document in project {ProjectName} for {FilePath}",
                        projectName, filePath);

                    updatedSolution = updatedSolution.WithDocumentText(docId, newText);
                    invalidatedProjects.Add(docId.ProjectId);
                }

                successCount++;
            }
            catch (FileNotFoundException)
            {
                _logger.LogTrace("File {FilePath} not found during update (may have been deleted)", filePath);
                // Not an error - file may have been deleted between queue and processing
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply deferred update for {FilePath}, will invalidate solution", filePath);

                // On any unexpected failure, invalidate entire solution for safety
                _solution = null;
                _workspace?.Dispose();
                _workspace = null;
                _compilationCache.Clear();
                return;
            }
        }

        // Step 3: Commit the batched update
        if (successCount > 0)
        {
            _solution = updatedSolution;

            // Invalidate compilation cache for affected projects
            foreach (var projectId in invalidatedProjects)
            {
                _compilationCache.TryRemove(projectId, out _);
            }

            _logger.LogTrace("Successfully applied {Count} deferred document update(s), invalidated {ProjectCount} project compilation(s)",
                successCount, invalidatedProjects.Count);
        }
        else
        {
            _logger.LogTrace("No updates were successfully applied");
        }
    }

    private void RegisterFileWatching()
    {
        var solutionDir = _filePathOps.GetDirectory(_options.SolutionPath!.Value).Value;
        if (string.IsNullOrEmpty(solutionDir))
        {
            return;
        }

        // Watch for project file changes - always invalidate
        _fileWatcher.AddPathWatch(solutionDir, "*.csproj", (path, changeType) =>
        {
            _logger.LogTrace("Project file watcher triggered: {ChangeType} for {Path}", changeType, path);
            _logger.LogDebug("Project file changed: {Path}", path);
            InvalidateSolution();
        });

        // Watch for solution file changes - always invalidate
        _fileWatcher.AddPathWatch(solutionDir, "*.sln", (path, changeType) =>
        {
            _logger.LogTrace("Solution file watcher triggered: {ChangeType} for {Path}", changeType, path);

            if (path.Equals(_options.SolutionPath!.Value.Value, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Solution file changed: {Path}", path);
                InvalidateSolution();
            }
            else
            {
                _logger.LogTrace("Solution file changed but not the configured solution, ignoring: {Path}", path);
            }
        });
        
        _fileWatcher.AddPathWatch(solutionDir, "*.slnx", (path, changeType) =>
        {
            _logger.LogTrace("Solution file watcher triggered: {ChangeType} for {Path}", changeType, path);

            if (path.Equals(_options.SolutionPath!.Value.Value, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Solution file changed: {Path}", path);
                InvalidateSolution();
            }
            else
            {
                _logger.LogTrace("Solution file changed but not the configured solution, ignoring: {Path}", path);
            }
        });

        // Watch for C# source file changes - smart handling based on change type
        _fileWatcher.AddPathWatch(solutionDir, "*.cs", (path, changeType) =>
        {
            _logger.LogTrace("C# source file watcher triggered: {ChangeType} for {Path}", changeType, path);

            switch (changeType)
            {
                case WatcherChangeTypes.Changed:
                    _logger.LogTrace("File content changed - calling UpdateDocument");
                    // File content changed - update in-memory document
                    UpdateDocument(path);
                    break;

                case WatcherChangeTypes.Created:
                case WatcherChangeTypes.Deleted:
                case WatcherChangeTypes.Renamed:
                    _logger.LogTrace("Structural change detected - calling InvalidateSolution");
                    // Structural change - full solution reload required
                    InvalidateSolution();
                    break;
            }
        });
    }

    private static IEnumerable<Project> ApplyProjectFilter(IEnumerable<Project> projects, ProjectFilter filter)
    {
        if (filter.IncludedProjects?.Count > 0)
        {
            projects = projects.Where(p => filter.IncludedProjects.Contains(p.Name));
        }

        if (filter.ExcludedProjects?.Count > 0)
        {
            projects = projects.Where(p => !filter.ExcludedProjects.Contains(p.Name));
        }

        return projects;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        lock (_lock)
        {
            _workspace?.Dispose();
            _compilationCache.Clear();
            _isDisposed = true;
        }

        // Clean up temp folder
        if (Directory.Exists(_tempBuildPath))
        {
            try
            {
                Directory.Delete(_tempBuildPath, recursive: true);
                _logger.LogDebug("Cleaned up temp build folder: {Path}", _tempBuildPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up temp build folder: {Path}", _tempBuildPath);
            }
        }
    }
}