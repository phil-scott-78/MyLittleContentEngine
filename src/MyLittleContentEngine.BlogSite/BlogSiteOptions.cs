using Microsoft.AspNetCore.Components;
using System.Reflection;
using MonorailCss;

namespace MyLittleContentEngine.BlogSite;

/// <summary>
/// Configuration options for a blog site
/// </summary>
public class BlogSiteOptions
{
    /// <summary>
    /// The primary hue for the site's color scheme (0-360)
    /// </summary>
    public int PrimaryHue { get; init; } = 250;

    /// <summary>
    /// The title of the blog site
    /// </summary>
    public required string SiteTitle { get; init; }

    /// <summary>
    /// The description of the blog site
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The canonical base URL for the site (used for sitemap and RSS)
    /// </summary>
    public string? CanonicalBaseUrl { get; init; }

    /// <summary>
    /// Base color name for the MonorailCSS theme
    /// </summary>
    public string BaseColorName { get; init; } = ColorNames.Slate;

    /// <summary>
    /// Links to display in the header and footer for navigation. 
    /// </summary>
    public HeaderLink[] MainSiteLinks { get; init; } = [];

    /// <summary>
    /// Path to the content directory
    /// </summary>
    public string ContentRootPath { get; init; } = "Content";

    /// <summary>
    /// Path to the blog content directory (relative to ContentRootPath)
    /// </summary>
    public string BlogContentPath { get; init; } = "Blog";

    /// <summary>
    /// Base URL for the site (used for routing)
    /// </summary>
    public required string BaseUrl { get; init; }

    /// <summary>
    /// Base URL for blog posts (used for routing)
    /// </summary>
    public string BlogBaseUrl { get; init; } = "/blog";

    /// <summary>
    /// URL for the tags page
    /// </summary>
    public string TagsPageUrl { get; init; } = "/tags";

    /// <summary>
    /// Additional CSS styles to include
    /// </summary>
    public string? ExtraStyles { get; init; }

    /// <summary>
    /// Custom hero content for the home page
    /// </summary>
    public RenderFragment? HeroContent { get; init; }

    /// <summary>
    /// Custom font family for display elements
    /// </summary>
    public string? DisplayFontFamily { get; init; } 

    /// <summary>
    /// Custom font family for body text
    /// </summary>
    public string? BodyFontFamily { get; init; }

    /// <summary>
    /// Custom HTML for code to be injected into the head section, for example Google Fonts or DNS prefetch
    /// </summary>
    public string? AdditionalHtmlHeadContent { get; init; }

    /// <summary>
    /// List of additional assemblies to scan for routing. 
    /// </summary>
    public Assembly[] AdditionalRoutingAssemblies { get; init; } = [];

    /// <summary>
    /// Author name for the blog
    /// </summary>
    public string? AuthorName { get; init; }

    /// <summary>
    /// Author bio for the blog
    /// </summary>
    public string? AuthorBio { get; init; }

    /// <summary>
    /// Enable social images generation
    /// </summary>
    public bool EnableSocialImages { get; init; } = true;

    /// <summary>
    /// Enable RSS feed generation
    /// </summary>
    public bool EnableRss { get; init; } = true;

    /// <summary>
    /// Enable sitemap generation
    /// </summary>
    public bool EnableSitemap { get; init; } = true;

    /// <summary>
    /// Content for the "Work" sidebar section
    /// </summary>
    public RenderFragment? HomeSidebarContent { get; init; }
    
    /// <summary>
    /// Path to the solution file for API documentation generation
    /// </summary>
    public string? SolutionPath { get; init; }
}

public record HeaderLink(string Title, string Url);