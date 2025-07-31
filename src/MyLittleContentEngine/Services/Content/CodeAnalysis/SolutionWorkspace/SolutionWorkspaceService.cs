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
    
    private MSBuildWorkspace? _workspace;
    private Solution? _solution;
    private readonly ConcurrentDictionary<ProjectId, Compilation> _compilationCache = new();
    private bool _isDisposed;

    static SolutionWorkspaceService()
    {
        // Initialize MSBuild once
        if (!MSBuildLocator.IsRegistered)
        {
            var instance = MSBuildLocator.QueryVisualStudioInstances()
                .OrderByDescending(x => x.Version)
                .FirstOrDefault() ?? MSBuildLocator.RegisterDefaults();
            
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

        if (string.IsNullOrEmpty(_options.SolutionPath))
        {
            throw new ArgumentException("Solution path must be specified in options", nameof(options));
        }

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

        var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (sender, args) =>
        {
            _logger.LogWarning("Workspace failed: {Diagnostic}", args.Diagnostic);
        };

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

        var solution = await LoadSolutionAsync(_options.SolutionPath!);
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
            return cachedCompilation;
        }

        try
        {
            _logger.LogDebug("Compiling project {ProjectName}", project.Name);
            var compilation = await project.GetCompilationAsync();

            if (compilation != null)
            {
                _compilationCache.TryAdd(project.Id, compilation);
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
        lock (_lock)
        {
            _logger.LogDebug("Invalidating solution cache");
            _solution = null;
            _workspace?.Dispose();
            _workspace = null;
            _compilationCache.Clear();
        }
    }

    private void RegisterFileWatching()
    {
        if (!_options.Caching?.EnableFileWatching ?? false)
        {
            return;
        }

        var solutionDir = Path.GetDirectoryName(_options.SolutionPath);
        if (string.IsNullOrEmpty(solutionDir))
        {
            return;
        }

        // Watch for project file changes
        _fileWatcher.AddPathWatch(solutionDir, "*.csproj", path =>
        {
            _logger.LogDebug("Project file changed: {Path}", path);
            InvalidateSolution();
        });

        // Watch for solution file changes  
        _fileWatcher.AddPathWatch(solutionDir, "*.sln", path =>
        {
            if (path.Equals(_options.SolutionPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Solution file changed: {Path}", path);
                InvalidateSolution();
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
    }
}