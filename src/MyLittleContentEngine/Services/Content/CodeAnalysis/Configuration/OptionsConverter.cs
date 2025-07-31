namespace MyLittleContentEngine.Services.Content.CodeAnalysis.Configuration;

/// <summary>
/// Converts between old RoslynHighlighterOptions and new CodeAnalysisOptions
/// </summary>
internal static class OptionsConverter
{
    /// <summary>
    /// Converts RoslynHighlighterOptions to CodeAnalysisOptions
    /// </summary>
    public static CodeAnalysisOptions ToCodeAnalysisOptions(this RoslynHighlighterOptions roslynOptions)
    {
        ProjectFilter? projectFilter = null;
        
        if (roslynOptions.ConnectedSolution != null)
        {
            var included = roslynOptions.ConnectedSolution.IncludedProjects?.Length > 0
                ? new HashSet<string>(roslynOptions.ConnectedSolution.IncludedProjects)
                : null;
                
            var excluded = roslynOptions.ConnectedSolution.ExcludedProjects?.Length > 0
                ? new HashSet<string>(roslynOptions.ConnectedSolution.ExcludedProjects)
                : null;
                
            if (included != null || excluded != null)
            {
                projectFilter = new ProjectFilter
                {
                    IncludedProjects = included,
                    ExcludedProjects = excluded
                };
            }
        }

        return new CodeAnalysisOptions
        {
            SolutionPath = roslynOptions.ConnectedSolution?.SolutionPath,
            ProjectFilter = projectFilter,
            Highlighting = new HighlightingOptions
            {
                CodeHighlightRenderOptionsFactory = roslynOptions.CodeHighlightRenderOptionsFactory,
                TabbedCodeBlockRenderOptionsFactory = roslynOptions.TabbedCodeBlockRenderOptionsFactory
            }
        };
    }

    /// <summary>
    /// Creates RoslynHighlighterOptions from CodeAnalysisOptions (for backward compatibility)
    /// </summary>
    public static RoslynHighlighterOptions ToRoslynHighlighterOptions(this CodeAnalysisOptions analysisOptions)
    {
        ConnectedDotNetSolution? connectedSolution = null;
        
        if (!string.IsNullOrEmpty(analysisOptions.SolutionPath))
        {
            connectedSolution = new ConnectedDotNetSolution
            {
                SolutionPath = analysisOptions.SolutionPath,
                IncludedProjects = analysisOptions.ProjectFilter?.IncludedProjects?.ToArray() ?? [],
                ExcludedProjects = analysisOptions.ProjectFilter?.ExcludedProjects?.ToArray() ?? []
            };
        }

        return new RoslynHighlighterOptions
        {
            ConnectedSolution = connectedSolution,
            CodeHighlightRenderOptionsFactory = analysisOptions.Highlighting.CodeHighlightRenderOptionsFactory,
            TabbedCodeBlockRenderOptionsFactory = analysisOptions.Highlighting.TabbedCodeBlockRenderOptionsFactory
        };
    }
}