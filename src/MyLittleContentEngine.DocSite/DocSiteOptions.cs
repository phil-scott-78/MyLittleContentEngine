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
    public int PrimaryHue { get; set; } = 235;

    /// <summary>
    /// Path to the solution file for API documentation generation
    /// </summary>
    public string? SolutionPath { get; set; }

    /// <summary>
    /// Options for API reference content generation
    /// </summary>
    public ApiReferenceContentOptions? ApiReferenceContentOptions { get; set; }

    /// <summary>
    /// The title of the documentation site
    /// </summary>
    public string SiteTitle { get; set; } = "Documentation Site";

    /// <summary>
    /// The description of the documentation site
    /// </summary>
    public string Description { get; set; } = "A documentation site built with My Little Content Engine";

    /// <summary>
    /// The canonical base URL for the site (used for sitemap and RSS)
    /// </summary>
    public string? CanonicalBaseUrl { get; set; }

    /// <summary>
    /// Custom logo/icon to display in the header
    /// </summary>
    public RenderFragment? HeaderIcon { get; set; }

    /// <summary>
    /// URL to the GitHub repository (optional)
    /// </summary>
    public string? GitHubUrl { get; set; }

    /// <summary>
    /// Custom header content (replaces the default site title)
    /// </summary>
    public RenderFragment? HeaderContent { get; set; }

    /// <summary>
    /// Base color name for the MonorailCSS theme
    /// </summary>
    public string BaseColorName { get; set; } = "Zinc";

    /// <summary>
    /// Path to the content directory
    /// </summary>
    public string ContentRootPath { get; set; } = "Content";

    /// <summary>
    /// Base URL for the site (used for routing)
    /// </summary>
    public string BaseUrl { get; set; } = "/";

    /// <summary>
    /// Additional CSS styles to include
    /// </summary>
    public string? ExtraStyles { get; set; }

    /// <summary>
    /// Namespaces to include in API documentation
    /// </summary>
    public string[]? IncludeNamespaces { get; set; }

    /// <summary>
    /// Namespaces to exclude from API documentation
    /// </summary>
    public string[]? ExcludeNamespaces { get; set; }
    
    /// <summary>
    /// Custom footer content (replaces the default site title)
    /// </summary>
    public RenderFragment? FooterContent { get; set; }
}