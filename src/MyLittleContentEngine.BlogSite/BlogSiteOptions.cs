using Microsoft.AspNetCore.Components;
using System.Reflection;
using MonorailCss;
using MyLittleContentEngine.Models;

namespace MyLittleContentEngine.BlogSite;

/// <summary>
/// Configuration options for a blog site
/// </summary>
public record BlogSiteOptions
{
    public BlogSiteOptions(string[] args)
    {
        if (args.Length <= 0) return;
        if (args[0] != "build") return;
    
        if (args.Length > 1)
        {
            BaseUrl = args[1];
        }

        if (args.Length > 2)
        {
            OutputPath = args[2];
        }
        
    }

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
    public string BaseUrl { get; } = "/";

    /// <summary>
    /// The output path for the static site generation. Default to output.
    /// </summary>
    public string OutputPath { get; } = "output";

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
    public HeroContent? HeroContent { get; init; }

    /// <summary>
    /// Custom font family for display elements such as headers and navigation elements
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
    /// Enable RSS feed generation
    /// </summary>
    public bool EnableRss { get; init; } = true;

    /// <summary>
    /// Enable sitemap generation
    /// </summary>
    public bool EnableSitemap { get; init; } = true;

    /// <summary>
    /// Projects to include in the home page sidebar
    /// </summary>
    public Project[] MyWork { get; init; } = [];

    /// <summary>
    /// Social Media Links
    /// </summary>
    public SocialLink[] Socials { get; init; } = [];
    
    /// <summary>
    /// Path to the solution file for API documentation generation
    /// </summary>
    public string? SolutionPath { get; init; }

    /// <summary>
    /// Function to generate a URL to be used in social media metadata links 
    /// </summary>
    public Func<MarkdownContentPage<BlogSiteFrontMatter>, string>? SocialMediaImageUrlFactory { get; init; }
}

public record SocialLink(RenderFragment Icon, string Url);

public record HeaderLink(string Title, string Url);

public record Project(string Title, string Description, string Url);

public record HeroContent(string Title, string Description);