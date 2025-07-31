using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace MyLittleContentEngine.Services.Content.CodeAnalysis.Assembly;

/// <summary>
/// Service responsible for loading and managing assemblies from Roslyn compilations
/// </summary>
public interface IAssemblyLoadingService : IDisposable
{
    /// <summary>
    /// Loads an assembly from raw bytes
    /// </summary>
    /// <param name="assemblyBytes">The assembly bytes</param>
    /// <param name="pdbBytes">Optional PDB bytes for debugging</param>
    /// <returns>Information about the loaded assembly</returns>
    Task<LoadedAssembly> LoadAssemblyAsync(byte[] assemblyBytes, byte[]? pdbBytes = null);

    /// <summary>
    /// Loads an assembly from a Roslyn compilation
    /// </summary>
    /// <param name="compilation">The compilation to emit and load</param>
    /// <param name="emitOptions">Options for emitting the assembly</param>
    /// <returns>Information about the loaded assembly</returns>
    Task<LoadedAssembly> LoadFromCompilationAsync(Compilation compilation, EmitOptions? emitOptions = null);

    /// <summary>
    /// Loads an assembly from a project
    /// </summary>
    /// <param name="project">The project to compile and load</param>
    /// <param name="emitOptions">Options for emitting the assembly</param>
    /// <returns>Information about the loaded assembly</returns>
    Task<LoadedAssembly> LoadFromProjectAsync(Project project, EmitOptions? emitOptions = null);

    /// <summary>
    /// Unloads a previously loaded assembly
    /// </summary>
    /// <param name="assemblyId">The ID of the assembly to unload</param>
    void UnloadAssembly(Guid assemblyId);

    /// <summary>
    /// Unloads all loaded assemblies
    /// </summary>
    void UnloadAll();

    /// <summary>
    /// Gets information about currently loaded assemblies
    /// </summary>
    IReadOnlyCollection<LoadedAssemblyInfo> GetLoadedAssemblies();
}

/// <summary>
/// Represents a loaded assembly with its context
/// </summary>
public record LoadedAssembly
{
    /// <summary>
    /// Unique identifier for this loaded assembly
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The loaded assembly
    /// </summary>
    public required System.Reflection.Assembly Assembly { get; init; }

    /// <summary>
    /// The assembly load context (for unloading)
    /// </summary>
    public required System.Runtime.Loader.AssemblyLoadContext Context { get; init; }

    /// <summary>
    /// When the assembly was loaded
    /// </summary>
    public DateTimeOffset LoadedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional metadata about the source
    /// </summary>
    public string? SourceInfo { get; init; }
}

/// <summary>
/// Information about a loaded assembly
/// </summary>
public record LoadedAssemblyInfo
{
    /// <summary>
    /// Assembly ID
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Assembly name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// When loaded
    /// </summary>
    public required DateTimeOffset LoadedAt { get; init; }

    /// <summary>
    /// Source information
    /// </summary>
    public string? SourceInfo { get; init; }

    /// <summary>
    /// Whether the assembly is still loaded
    /// </summary>
    public required bool IsLoaded { get; init; }
}