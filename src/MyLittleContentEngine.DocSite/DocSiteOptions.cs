using System.Reflection;
using Microsoft.AspNetCore.Components;
using MonorailCss;
using MyLittleContentEngine.Services.Content.Roslyn;

namespace MyLittleContentEngine.DocSite;

/// <summary>
/// Configuration options for a documentation site
/// </summary>
public class DocSiteOptions
{
    /// <summary>
    /// The primary hue for the site's color scheme (0-360)
    /// </summary>
    public int PrimaryHue { get; init; } = 235;

    /// <summary>
    /// Path to the solution file for API documentation generation
    /// </summary>
    public string? SolutionPath { get; init; }

    /// <summary>
    /// Options for API reference content generation
    /// </summary>
    public ApiReferenceContentOptions? ApiReferenceContentOptions { get; init; }

    /// <summary>
    /// The title of the documentation site
    /// </summary>
    public required string SiteTitle { get; init; }

    /// <summary>
    /// The description of the documentation site
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The canonical base URL for the site (used for sitemap and RSS)
    /// </summary>
    public string? CanonicalBaseUrl { get; init; }

    /// <summary>
    /// Custom logo/icon to display in the header
    /// </summary>
    public RenderFragment? HeaderIcon { get; init; }

    /// <summary>
    /// URL to the GitHub repository (optional)
    /// </summary>
    public string? GitHubUrl { get; init; }

    /// <summary>
    /// Custom header content (replaces the default site title)
    /// </summary>
    public RenderFragment? HeaderContent { get; init; }

    /// <summary>
    /// Base color name for the MonorailCSS theme
    /// </summary>
    public string BaseColorName { get; init; } = "Zinc";

    /// <summary>
    /// Path to the content directory
    /// </summary>
    public string ContentRootPath { get; init; } = "Content";

    /// <summary>
    /// Base URL for the site (used for routing)
    /// </summary>
    public required string BaseUrl { get; init; }

    /// <summary>
    /// Additional CSS styles to include
    /// </summary>
    public string? ExtraStyles { get; init; }

    /// <summary>
    /// Namespaces to include in API documentation
    /// </summary>
    public string[]? IncludeNamespaces { get; init; }

    /// <summary>
    /// Namespaces to exclude from API documentation
    /// </summary>
    public string[]? ExcludeNamespaces { get; init; }
    
    /// <summary>
    /// Custom footer content (replaces the default site title)
    /// </summary>
    public RenderFragment? FooterContent { get; init; }
    
    /// <summary>
    /// List of additional assemblies to scan for routing
    /// </summary>
    public Assembly[] AdditionalRoutingAssemblies { get; init; } = [];
}
