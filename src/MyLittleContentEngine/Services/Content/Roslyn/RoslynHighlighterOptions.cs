using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.Tabs;

namespace MyLittleContentEngine.Services.Content.Roslyn;

/// <summary>
/// Configuration options for the Roslyn syntax highlighting service.
/// </summary>
/// <remarks>
/// This class provides configuration settings for the <see cref="RoslynHighlighterService"/>
/// to specify the paths required for syntax highlighting of code blocks in Markdown content.
/// </remarks>
public record RoslynHighlighterOptions
{
    /// <summary>
    /// Gets or initializes the solutions to connect the <see cref="RoslynHighlighterService"/> to for highlighting.
    /// </summary>
    public ConnectedDotNetSolution? ConnectedSolution { get; init; }

    public Func<CodeHighlightRenderOptions> CodeHighlightRenderOptionsFactory { get; init; } = () => CodeHighlightRenderOptions.MonorailColorful;
    public Func<TabbedCodeBlockRenderOptions> TabbedCodeBlockRenderOptionsFactory { get; init; } = () => TabbedCodeBlockRenderOptions.MonorailColorful;

}

/// <summary>
/// Solution connected to the <see cref="RoslynHighlighterOptions"/>.
/// </summary>
public record ConnectedDotNetSolution
{
    /// <summary>
    /// Gets or sets the path to the solution file (.sln) that contains the projects
    /// to be used for syntax highlighting.
    /// </summary>
    /// <remarks>
    /// The path can be absolute or relative to the application's execution directory.
    /// </remarks>
    public required string SolutionPath { get; init; }

    /// <summary>
    /// Gets or sets the project names to include for syntax highlighting.
    /// </summary>
    /// <remarks>
    /// Project names are matched case-insensitively. If specified, only these projects
    /// will be used for highlighting. Cannot be used together with ExcludedProjects.
    /// </remarks>
    public string[] IncludedProjects { get; init; } = [];

    /// <summary>
    /// Gets or sets the project names to exclude from syntax highlighting.
    /// </summary>
    /// <remarks>
    /// Project names are matched case-insensitively. If specified, all projects except
    /// these will be used for highlighting. Cannot be used together with IncludedProjects.
    /// </remarks>
    public string[] ExcludedProjects { get; init; } = [];
}