namespace MyLittleContentEngine.Services.Content.CodeAnalysis.Configuration;

/// <summary>
/// Unified configuration options for code analysis services
/// </summary>
public record CodeAnalysisOptions
{
    /// <summary>
    /// Path to the solution file to analyze
    /// </summary>
    public FilePath? SolutionPath { get; init; }

    /// <summary>
    /// Filter for including/excluding projects
    /// </summary>
    public ProjectFilter? ProjectFilter { get; init; }
}

/// <summary>
/// Configuration for project filtering
/// </summary>
public record ProjectFilter
{
    /// <summary>
    /// Projects to include (if specified, only these projects are processed)
    /// </summary>
    public HashSet<string>? IncludedProjects { get; init; }

    /// <summary>
    /// Projects to exclude from processing
    /// </summary>
    public HashSet<string>? ExcludedProjects { get; init; }

    /// <summary>
    /// Creates a filter that includes only the specified projects
    /// </summary>
    public static ProjectFilter Include(params string[] projectNames) => new()
    {
        IncludedProjects = [..projectNames]
    };

    /// <summary>
    /// Creates a filter that excludes the specified projects
    /// </summary>
    public static ProjectFilter Exclude(params string[] projectNames) => new()
    {
        ExcludedProjects = [..projectNames]
    };

    /// <summary>
    /// Validates the filter configuration
    /// </summary>
    public void Validate()
    {
        if (IncludedProjects?.Count > 0 && ExcludedProjects?.Count > 0)
        {
            var overlap = IncludedProjects.Intersect(ExcludedProjects).ToList();
            if (overlap.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Projects cannot be both included and excluded: {string.Join(", ", overlap)}");
            }
        }
    }
}
