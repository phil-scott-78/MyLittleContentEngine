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
    private readonly Lock _lock = new();
    private readonly string _tempBuildPath;

    private MSBuildWorkspace? _workspace;
    private Solution? _solution;
    private readonly ConcurrentDictionary<ProjectId, Compilation> _compilationCache = new();
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
        IContentEngineFileWatcher fileWatcher)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));

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
                return _solution;
            }
        }

        _logger.LogInformation("Loading solution from {SolutionPath}", solutionPath);

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

            _logger.LogInformation("Successfully loaded solution with {ProjectCount} projects", solution.Projects.Count());
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
        _logger.LogTrace("InvalidateSolution called");

        lock (_lock)
        {
            var cachedProjectCount = _compilationCache.Count;
            _logger.LogTrace("Invalidating solution cache (clearing {Count} cached compilations)", cachedProjectCount);

            _solution = null;
            _workspace?.Dispose();
            _workspace = null;
            _compilationCache.Clear();

            _logger.LogTrace("Solution cache invalidated, workspace disposed");
        }
    }

    private void UpdateDocument(string filePath)
    {
        _logger.LogTrace("UpdateDocument called for {FilePath}", filePath);

        lock (_lock)
        {
            if (_solution == null)
            {
                _logger.LogTrace("Solution not loaded, skipping document update for {FilePath}", filePath);
                return;
            }

            var documentIds = _solution.GetDocumentIdsWithFilePath(filePath);
            _logger.LogTrace("Found {Count} document(s) for {FilePath}", documentIds.Length, filePath);

            if (documentIds.IsEmpty)
            {
                _logger.LogTrace("File {FilePath} not found in solution, may be a new file", filePath);
                return;
            }

            try
            {
                // Use FileShare.ReadWrite to allow reading even when file is locked by editor
                using var fileStream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8);
                var fileContent = reader.ReadToEnd();
                var newText = Microsoft.CodeAnalysis.Text.SourceText.From(fileContent, System.Text.Encoding.UTF8);

                var updatedSolution = _solution;

                foreach (var docId in documentIds)
                {
                    var document = _solution.GetDocument(docId);
                    var projectName = document?.Project.Name ?? "Unknown";

                    _logger.LogTrace("Updating document in project {ProjectName} for {FilePath}", projectName, filePath);
                    updatedSolution = updatedSolution.WithDocumentText(docId, newText);

                    _compilationCache.TryRemove(docId.ProjectId, out _);
                    _logger.LogTrace("Invalidated compilation cache for project {ProjectName} ({ProjectId})", projectName, docId.ProjectId);
                }

                _solution = updatedSolution;
                _logger.LogTrace("Successfully updated {Count} document(s) in-memory for {FilePath}", documentIds.Length, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Failed to update document {FilePath}, falling back to full solution invalidation", filePath);
                InvalidateSolution();
            }
        }
    }

    private void RegisterFileWatching()
    {
        var solutionDir = _options.SolutionPath!.Value.GetDirectory().Value;
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