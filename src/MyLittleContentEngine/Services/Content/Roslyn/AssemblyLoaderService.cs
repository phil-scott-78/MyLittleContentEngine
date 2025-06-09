using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;

namespace MyLittleContentEngine.Services.Content.Roslyn;

/// <summary>
/// Handles loading of example assemblies.
/// </summary>
/// <param name="logger"></param>
public class AssemblyLoaderService(ILogger<AssemblyLoaderService> logger, IFileSystem fileSystem)
{
    private readonly ILogger _logger = logger;
    private RoslynAssemblyLoadContext? _loadContext;
    private bool _needsReset;
    private static readonly ConcurrentDictionary<string, byte[]> AssemblyBytesCache = new();

    internal void ResetContext()
    {
        _needsReset = true;
    }

    private RoslynAssemblyLoadContext GetOrCreateContext()
    {
        if (_needsReset)
        {
            _loadContext?.Unload();
            _loadContext = new RoslynAssemblyLoadContext();
            AssemblyBytesCache.Clear();
            _needsReset = false;
        }
        else
        {
            _loadContext ??= new RoslynAssemblyLoadContext();
        }

        return _loadContext;
    }

    internal async Task<Assembly> GetProjectAssembly(Project project, EmitOptions emitOptions)
    {
        var loadContext = GetOrCreateContext();
        var compilation = await project.GetCompilationAsync();
        if (compilation == null)
        {
            throw new Exception($"Could not get compilation for {project.FilePath}");
        }

        var options = compilation.Options
            .WithOptimizationLevel(OptimizationLevel.Release)
            .WithConcurrentBuild(true);

        compilation = compilation.WithOptions(options);

        _logger.LogDebug("--- Compilation Details for Project: {ProjectFilePath} ---", project.FilePath);

        await using var ms = new MemoryStream();
        var emitResult = compilation.Emit(peStream: ms, options: emitOptions);
        if (!emitResult.Success)
        {
            var failures =
                emitResult.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error);
            var errorMessage = string.Join(Environment.NewLine, failures.Select(d => $"{d.Id}: {d.GetMessage()}"));
            throw new Exception($"Compilation failed: {errorMessage}");
        }

        foreach (var reference in compilation.References.OfType<PortableExecutableReference>())
        {
            try
            {
                if (reference.FilePath == null || !fileSystem.File.Exists(reference.FilePath)) continue;
                if (reference.FilePath.Contains("Microsoft.NETCore.App.Ref") ||
                    reference.FilePath.Contains(@".Ref\") ||
                    reference.FilePath.Contains(@"\ref\"))
                {
                    continue;
                }

                var refName = fileSystem.Path.GetFileNameWithoutExtension(reference.FilePath);
                if (AssemblyBytesCache.ContainsKey(refName))
                {
                    continue;
                }

                var refBytes = await fileSystem.File.ReadAllBytesAsync(reference.FilePath);
                AssemblyBytesCache[refName] = refBytes;
                await using var refMs = new MemoryStream(refBytes);
                loadContext.LoadFromStream(refMs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to preload reference: {FilePath}", reference.FilePath);
            }
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assemblyBytes = ms.ToArray();
        await using var mainMs = new MemoryStream(assemblyBytes);
        var assembly = loadContext.LoadFromStream(mainMs);
        return assembly;
    }
}