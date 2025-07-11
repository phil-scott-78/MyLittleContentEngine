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
    public int PrimaryHue { get; set; } = 250;

    /// <summary>
    /// The title of the blog site
    /// </summary>
    public string SiteTitle { get; set; } = "My Blog";

    /// <summary>
    /// The description of the blog site
    /// </summary>
    public string Description { get; set; } = "A blog site built with My Little Content Engine";

    /// <summary>
    /// The canonical base URL for the site (used for sitemap and RSS)
    /// </summary>
    public string? CanonicalBaseUrl { get; set; }

    /// <summary>
    /// Base color name for the MonorailCSS theme
    /// </summary>
    public string BaseColorName { get; set; } = ColorNames.Slate;

    /// <summary>
    /// Links to display in the header and footer for navigation. 
    /// </summary>
    public HeaderLink[] MainSiteLinks { get; set; } = [];

    /// <summary>
    /// Path to the content directory
    /// </summary>
    public string ContentRootPath { get; set; } = "Content";

    /// <summary>
    /// Path to the blog content directory (relative to ContentRootPath)
    /// </summary>
    public string BlogContentPath { get; set; } = "Blog";

    /// <summary>
    /// Base URL for the site (used for routing)
    /// </summary>
    public string BaseUrl { get; set; } = "/";

    /// <summary>
    /// Base URL for blog posts (used for routing)
    /// </summary>
    public string BlogBaseUrl { get; set; } = "/blog";

    /// <summary>
    /// URL for the tags page
    /// </summary>
    public string TagsPageUrl { get; set; } = "/tags";

    /// <summary>
    /// Additional CSS styles to include
    /// </summary>
    public string? ExtraStyles { get; set; }

    /// <summary>
    /// Custom footer content
    /// </summary>
    public RenderFragment? FooterContent { get; set; }

    /// <summary>
    /// Custom hero content for the home page
    /// </summary>
    public RenderFragment? HeroContent { get; set; }

    /// <summary>
    /// Additional header buttons (e.g., social media links)
    /// </summary>
    public RenderFragment[]? ExtraHeaderButtons { get; set; }

    /// <summary>
    /// Custom font family for display elements
    /// </summary>
    public string? DisplayFontFamily { get; set; } = "-apple-system, BlinkMacSystemFont, avenir next, avenir, segoe ui, helvetica neue, Adwaita Sans, Cantarell, Ubuntu, roboto, noto, helvetica, arial, sans-serif;";

    /// <summary>
    /// Custom font family for body text
    /// </summary>
    public string? BodyFontFamily { get; set; } = "-apple-system, BlinkMacSystemFont, avenir next, avenir, segoe ui, helvetica neue, Adwaita Sans, Cantarell, Ubuntu, roboto, noto, helvetica, arial, sans-serif;";

    /// <summary>
    /// Custom HTML for code to be injected into the head section, for example Google Fonts or DNS prefetch
    /// </summary>
    public string? AdditionalHtmlHeadContent { get; set; }

    /// <summary>
    /// List of additional assemblies to scan for routing. 
    /// </summary>
    public Assembly[] AdditionalRoutingAssemblies { get; set; } = [];

    /// <summary>
    /// Author name for the blog
    /// </summary>
    public string? AuthorName { get; set; }

    /// <summary>
    /// Author bio for the blog
    /// </summary>
    public string? AuthorBio { get; set; }

    /// <summary>
    /// Enable social images generation
    /// </summary>
    public bool EnableSocialImages { get; set; } = true;

    /// <summary>
    /// Enable RSS feed generation
    /// </summary>
    public bool EnableRss { get; set; } = true;

    /// <summary>
    /// Enable sitemap generation
    /// </summary>
    public bool EnableSitemap { get; set; } = true;

    /// <summary>
    /// Content for the "Stay up to date" sidebar section
    /// </summary>
    public RenderFragment? StayUpToDateContent { get; set; }

    /// <summary>
    /// Content for the "Work" sidebar section
    /// </summary>
    public RenderFragment? WorkContent { get; set; }
}

public record HeaderLink(string Title, string Url);