using MyLittleContentEngine.Services.Content.CodeAnalysis.SyntaxHighlighting;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.Tabs;

namespace MyLittleContentEngine.Services.Content.CodeAnalysis.Configuration;

/// <summary>
/// Unified configuration options for code analysis services
/// </summary>
public record CodeAnalysisOptions
{
    /// <summary>
    /// Path to the solution file to analyze
    /// </summary>
    public string? SolutionPath { get; init; }

    /// <summary>
    /// Filter for including/excluding projects
    /// </summary>
    public ProjectFilter? ProjectFilter { get; init; }

    /// <summary>
    /// Options for syntax highlighting
    /// </summary>
    public HighlightingOptions Highlighting { get; init; } = new();

    /// <summary>
    /// Options for caching behavior
    /// </summary>
    public CachingOptions Caching { get; init; } = new();

    /// <summary>
    /// Options for code execution
    /// </summary>
    public ExecutionOptions Execution { get; init; } = new();

    /// <summary>
    /// Validates the options and throws if invalid
    /// </summary>
    public void Validate()
    {
        if (!string.IsNullOrEmpty(SolutionPath) && !File.Exists(SolutionPath))
        {
            throw new FileNotFoundException($"Solution file not found: {SolutionPath}");
        }

        ProjectFilter?.Validate();
    }
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
        IncludedProjects = new HashSet<string>(projectNames)
    };

    /// <summary>
    /// Creates a filter that excludes the specified projects
    /// </summary>
    public static ProjectFilter Exclude(params string[] projectNames) => new()
    {
        ExcludedProjects = new HashSet<string>(projectNames)
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

/// <summary>
/// Options for syntax highlighting behavior
/// </summary>
public record HighlightingOptions
{
    /// <summary>
    /// Factory for creating code highlight render options
    /// </summary>
    public Func<CodeHighlightRenderOptions> CodeHighlightRenderOptionsFactory { get; init; } = 
        () => CodeHighlightRenderOptions.Default;

    /// <summary>
    /// Factory for creating tabbed code block render options
    /// </summary>
    public Func<TabbedCodeBlockRenderOptions> TabbedCodeBlockRenderOptionsFactory { get; init; } = 
        () => TabbedCodeBlockRenderOptions.Default;

    /// <summary>
    /// Default language for code blocks without language specification
    /// </summary>
    public Language DefaultLanguage { get; init; } = Language.CSharp;
}

/// <summary>
/// Options for caching behavior
/// </summary>
public record CachingOptions
{
    /// <summary>
    /// Whether to enable file watching for cache invalidation
    /// </summary>
    public bool EnableFileWatching { get; init; } = true;

    /// <summary>
    /// Maximum size of the symbol cache
    /// </summary>
    public int MaxSymbolCacheSize { get; init; } = 1000;

    /// <summary>
    /// Maximum size of the compilation cache
    /// </summary>
    public int MaxCompilationCacheSize { get; init; } = 10;

    /// <summary>
    /// Debounce time for file watching events in milliseconds
    /// </summary>
    public int FileWatchDebounceMs { get; init; } = 500;
}

/// <summary>
/// Options for code execution behavior
/// </summary>
public record ExecutionOptions
{
    /// <summary>
    /// Timeout for code execution in milliseconds
    /// </summary>
    public int TimeoutMs { get; init; } = 30000;

    /// <summary>
    /// Maximum output size in characters
    /// </summary>
    public int MaxOutputSize { get; init; } = 100000;

    /// <summary>
    /// Whether to capture console error output
    /// </summary>
    public bool CaptureErrorOutput { get; init; } = true;

    /// <summary>
    /// Whether to allow unsafe code execution
    /// </summary>
    public bool AllowUnsafe { get; init; } = false;
}