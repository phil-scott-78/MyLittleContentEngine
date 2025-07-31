using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Services.Content.CodeAnalysis.Configuration;
using MyLittleContentEngine.Services.Content.CodeAnalysis.SolutionWorkspace;

namespace MyLittleContentEngine.Services.Content.CodeAnalysis.Assembly;

/// <summary>
/// Implementation of IAssemblyLoadingService that manages assembly loading and unloading
/// </summary>
internal class AssemblyLoadingService : IAssemblyLoadingService
{
    private readonly ILogger<AssemblyLoadingService> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly ISolutionWorkspaceService? _workspaceService;
    private readonly CodeAnalysisOptions _options;
    private readonly ConcurrentDictionary<Guid, LoadedAssembly> _loadedAssemblies = new();
    private readonly ConcurrentDictionary<string, byte[]> _assemblyBytesCache = new();
    private readonly Lock _lock = new();
    private bool _disposed;

    public AssemblyLoadingService(
        ILogger<AssemblyLoadingService> logger,
        IFileSystem fileSystem,
        CodeAnalysisOptions options,
        ISolutionWorkspaceService? workspaceService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _workspaceService = workspaceService;
    }

    public Task<LoadedAssembly> LoadAssemblyAsync(byte[] assemblyBytes, byte[]? pdbBytes = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(AssemblyLoadingService));

        var context = new RoslynAssemblyLoadContext();
        
        try
        {
            using var assemblyStream = new MemoryStream(assemblyBytes);
            using var pdbStream = pdbBytes != null ? new MemoryStream(pdbBytes) : null;

            var assembly = pdbStream != null
                ? context.LoadFromStream(assemblyStream, pdbStream)
                : context.LoadFromStream(assemblyStream);

            var loadedAssembly = new LoadedAssembly
            {
                Assembly = assembly,
                Context = context,
                SourceInfo = "Direct byte load"
            };

            _loadedAssemblies.TryAdd(loadedAssembly.Id, loadedAssembly);
            _logger.LogDebug("Loaded assembly {AssemblyName} with ID {AssemblyId}", 
                assembly.GetName().Name, loadedAssembly.Id);

            return Task.FromResult(loadedAssembly);
        }
        catch (Exception ex)
        {
            context.Unload();
            _logger.LogError(ex, "Failed to load assembly from bytes");
            throw;
        }
    }

    public async Task<LoadedAssembly> LoadFromCompilationAsync(Compilation compilation, EmitOptions? emitOptions = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(AssemblyLoadingService));

        // Apply optimization settings
        var options = compilation.Options
            .WithOptimizationLevel(OptimizationLevel.Release)
            .WithConcurrentBuild(true);
        
        compilation = compilation.WithOptions(options);

        // Emit the assembly
        using var assemblyStream = new MemoryStream();
        using var pdbStream = new MemoryStream();

        var emitResult = compilation.Emit(
            peStream: assemblyStream,
            pdbStream: pdbStream,
            options: emitOptions ?? new EmitOptions());

        if (!emitResult.Success)
        {
            var errors = emitResult.Diagnostics
                .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                .Select(d => $"{d.Id}: {d.GetMessage()}");
            
            var errorMessage = string.Join(Environment.NewLine, errors);
            throw new InvalidOperationException($"Compilation failed: {errorMessage}");
        }

        // Load the assembly
        var assemblyBytes = assemblyStream.ToArray();
        var pdbBytes = pdbStream.ToArray();
        
        var loadedAssembly = await LoadAssemblyWithReferencesAsync(
            compilation, assemblyBytes, pdbBytes, 
            $"Compilation: {compilation.AssemblyName}");

        return loadedAssembly;
    }

    public async Task<LoadedAssembly> LoadFromProjectAsync(Project project, EmitOptions? emitOptions = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(AssemblyLoadingService));

        _logger.LogInformation("Loading assembly from project {ProjectName}", project.Name);

        var compilation = _workspaceService != null
            ? await _workspaceService.GetCompilationAsync(project)
            : await project.GetCompilationAsync();

        if (compilation == null)
        {
            throw new InvalidOperationException($"Could not get compilation for project {project.Name}");
        }

        var loadedAssembly = await LoadFromCompilationAsync(compilation, emitOptions);
        
        // Update source info with project details
        return loadedAssembly with { SourceInfo = $"Project: {project.Name}" };
    }

    public void UnloadAssembly(Guid assemblyId)
    {
        if (_loadedAssemblies.TryRemove(assemblyId, out var loadedAssembly))
        {
            _logger.LogDebug("Unloading assembly {AssemblyId}", assemblyId);
            
            try
            {
                loadedAssembly.Context.Unload();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error unloading assembly {AssemblyId}", assemblyId);
            }
        }
    }

    public void UnloadAll()
    {
        lock (_lock)
        {
            _logger.LogInformation("Unloading all {Count} assemblies", _loadedAssemblies.Count);

            foreach (var kvp in _loadedAssemblies)
            {
                try
                {
                    kvp.Value.Context.Unload();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error unloading assembly {AssemblyId}", kvp.Key);
                }
            }

            _loadedAssemblies.Clear();
            _assemblyBytesCache.Clear();
        }
    }

    public IReadOnlyCollection<LoadedAssemblyInfo> GetLoadedAssemblies()
    {
        return _loadedAssemblies.Values
            .Select(la => new LoadedAssemblyInfo
            {
                Id = la.Id,
                Name = la.Assembly.GetName().Name ?? "Unknown",
                LoadedAt = la.LoadedAt,
                SourceInfo = la.SourceInfo,
                IsLoaded = !IsUnloaded(la.Context)
            })
            .ToList();
    }

    private async Task<LoadedAssembly> LoadAssemblyWithReferencesAsync(
        Compilation compilation, 
        byte[] assemblyBytes, 
        byte[] pdbBytes,
        string sourceInfo)
    {
        var context = new RoslynAssemblyLoadContext();

        try
        {
            // Pre-load reference assemblies
            await PreloadReferencesAsync(context, compilation);

            // Load the main assembly
            using var assemblyStream = new MemoryStream(assemblyBytes);
            using var pdbStream = new MemoryStream(pdbBytes);
            
            var assembly = context.LoadFromStream(assemblyStream, pdbStream);

            var loadedAssembly = new LoadedAssembly
            {
                Assembly = assembly,
                Context = context,
                SourceInfo = sourceInfo
            };

            _loadedAssemblies.TryAdd(loadedAssembly.Id, loadedAssembly);
            _logger.LogDebug("Loaded assembly {AssemblyName} with ID {AssemblyId}", 
                assembly.GetName().Name, loadedAssembly.Id);

            return loadedAssembly;
        }
        catch (Exception ex)
        {
            context.Unload();
            _logger.LogError(ex, "Failed to load assembly with references");
            throw;
        }
    }

    private async Task PreloadReferencesAsync(AssemblyLoadContext context, Compilation compilation)
    {
        foreach (var reference in compilation.References.OfType<PortableExecutableReference>())
        {
            try
            {
                if (reference.FilePath == null || !_fileSystem.File.Exists(reference.FilePath))
                {
                    continue;
                }

                // Skip reference assemblies
                if (IsReferenceAssembly(reference.FilePath))
                {
                    continue;
                }

                var refName = _fileSystem.Path.GetFileNameWithoutExtension(reference.FilePath);
                
                // Check cache first
                if (!_assemblyBytesCache.TryGetValue(refName, out var refBytes))
                {
                    refBytes = await _fileSystem.File.ReadAllBytesAsync(reference.FilePath);
                    _assemblyBytesCache.TryAdd(refName, refBytes);
                }

                using var refStream = new MemoryStream(refBytes);
                context.LoadFromStream(refStream);
                
                _logger.LogTrace("Preloaded reference: {ReferenceName}", refName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to preload reference: {FilePath}", reference.FilePath);
            }
        }
    }

    private static bool IsReferenceAssembly(string filePath)
    {
        return filePath.Contains("Microsoft.NETCore.App.Ref") ||
               filePath.Contains(@".Ref\") ||
               filePath.Contains(@"\ref\") ||
               filePath.Contains(@"/ref/");
    }

    private static bool IsUnloaded(AssemblyLoadContext context)
    {
        try
        {
            // If we can access the assemblies, it's not unloaded
            _ = context.Assemblies.Any();
            return false;
        }
        catch
        {
            return true;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        UnloadAll();
        _disposed = true;
    }
}