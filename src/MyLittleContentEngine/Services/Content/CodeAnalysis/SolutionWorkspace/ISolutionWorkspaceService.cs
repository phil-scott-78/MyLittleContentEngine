using Microsoft.CodeAnalysis;

namespace MyLittleContentEngine.Services.Content.CodeAnalysis.SolutionWorkspace;

/// <summary>
/// Service responsible for managing MSBuild workspace operations and solution loading
/// </summary>
public interface ISolutionWorkspaceService : IDisposable
{
    /// <summary>
    /// Loads a solution from the specified path
    /// </summary>
    /// <param name="solutionPath">Path to the .sln file</param>
    /// <returns>The loaded solution</returns>
    Task<Solution> LoadSolutionAsync(string solutionPath);

    /// <summary>
    /// Gets projects from the loaded solution with optional filtering
    /// </summary>
    /// <param name="filter">Optional filter predicate for projects</param>
    /// <returns>Filtered collection of projects</returns>
    Task<IEnumerable<Project>> GetProjectsAsync(Func<Project, bool>? filter = null);

    /// <summary>
    /// Gets the compilation for a specific project
    /// </summary>
    /// <param name="project">The project to compile</param>
    /// <returns>The compilation or null if compilation fails</returns>
    Task<Compilation?> GetCompilationAsync(Project project);

    /// <summary>
    /// Invalidates the cached solution, forcing a reload on next access
    /// </summary>
    void InvalidateSolution();
}