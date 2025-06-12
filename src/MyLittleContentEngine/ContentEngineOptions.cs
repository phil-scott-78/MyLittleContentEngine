using System.Collections.Immutable;
using Markdig;
using MyLittleContentEngine.Services.Content.MarkdigExtensions;
using Markdig.Extensions.AutoIdentifiers;
using Mdazor;
using Microsoft.Extensions.DependencyInjection;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content.Roslyn;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MyLittleContentEngine;

/// <summary>
/// Configuration options for the MyLittleContentEngine static site generation process.
/// </summary>
/// <remarks>
/// <para>
/// This class provides comprehensive configuration for controlling how MyLittleContentEngine
/// generates static websites from Blazor applications, including output paths,
/// content processing, and generation behavior.
/// </para>
/// </remarks>
public class ContentEngineOptions
{
    /// <summary>
    /// Gets or sets the title of the blog or website.
    /// </summary>
    /// <remarks>
    /// This value is typically used in page headers, metadata, and navigation elements.
    /// </remarks>
    public required string SiteTitle { get; init; }

    /// <summary>
    /// Gets or sets the description or tagline of the blog or website.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This value is typically used in page metadata, headers, and in SEO-related contexts
    /// such as social media previews and search engine results.
    /// </para>
    /// </remarks>
    public required string SiteDescription { get; init; }

    /// <summary>
    /// Gets or sets the base URL for the published site.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This value is used for generating absolute URLs in various contexts, including:
    /// </para>
    /// <list type="bullet">
    ///     <item><description>Sitemap.xml generation</description></item>
    ///     <item><description>RSS/Atom feed URLs</description></item>
    ///     <item><description>Open Graph and other social media metadata</description></item>
    ///     <item><description>Canonical URL generation</description></item>
    /// </list>
    /// <para>
    /// Example format: "https://example.com" (without a trailing slash)
    /// </para>
    /// </remarks>
    public required string BaseUrl { get; init; }

    /// <summary>
    /// Gets or sets the path to the content root directory. Defaults to "Content".
    /// </summary>
    public string ContentRootPath { get; init; } = "Content";

    /// <summary>
    /// Gets or sets the output directory path for generated static files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This path is relative to the project root directory. All generated static content
    /// will be placed in this directory during the build process.
    /// </para>
    /// <para>
    /// The default value is "output".
    /// </para>
    /// </remarks>
    public string OutputFolderPath { get; init; } = "output";

    /// <summary>
    /// Gets or sets the collection of pages that will be generated as static HTML files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This list allows you to explicitly define which routes should be pre-rendered
    /// as static HTML files during the build process.
    /// </para>
    /// <para>
    /// For pages with route parameters, you can specify the parameter values to use
    /// when generating the static HTML.
    /// </para>
    /// </remarks>
    public ImmutableList<PageToGenerate> PagesToGenerate { get; init; } = [];

    /// <summary>
    /// Gets or sets whether to automatically include non-parameterized Razor pages in the generation process.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set to true, the system will automatically discover and generate static HTML
    /// for all Razor pages that do not contain route parameters.
    /// </para>
    /// <para>
    /// Examples of page types:
    /// </para>
    /// <list type="bullet">
    ///     <item>
    ///         <description>Non-parameterized (will be included): @page "/about"</description>
    ///     </item>
    ///     <item>
    ///         <description>Parameterized (will not be included): @page "/docs/{slug}"</description>
    ///     </item>
    /// </list>
    /// <para>
    /// Default value is true.
    /// </para>
    /// </remarks>
    public bool AddPagesWithoutParameters { get; init; } = true;

    /// <summary>
    /// Gets or sets the filename to use for index pages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This setting controls the output filename for routes that represent directory indices.
    /// </para>
    /// <para>
    /// For example, with the default value of "index.html":
    /// </para>
    /// <list type="bullet">
    ///     <item>
    ///         <description>A route @page "/blog" will generate a file at "blog/index.html"</description>
    ///     </item>
    ///     <item>
    ///         <description>A route @page "/blog/about" will generate a file at "blog/about/index.html"</description>
    ///     </item>
    /// </list>
    /// <para>
    /// Default value is "index.html".
    /// </para>
    /// </remarks>
    public string IndexPageHtml { get; init; } = "index.html";

    /// <summary>
    /// Gets or sets paths that should be excluded when copying content to the output folder.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This list contains file or directory paths that should be skipped during the content copy process.
    /// Paths are specified relative to the destination location in the output folder, not the source location.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///             To ignore "wwwroot/app.css" when copying "wwwroot" to the root of the output folder,
    ///             add "app.css" to this list.
    ///         </description>
    ///     </item>
    /// </list>
    /// </remarks>
    public ImmutableList<string> IgnoredPathsOnContentCopy { get; init; } = [];

    /// <summary>
    /// Gets or sets the YAML deserializer used for parsing front matter in Markdown files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This deserializer converts YAML front matter sections in Markdown files into
    /// strongly typed objects for use in templates and rendering.
    /// </para>
    /// <para>
    /// The default configuration:
    /// </para>
    /// <list type="bullet">
    ///     <item><description>Uses camel case naming convention for property mapping</description></item>
    ///     <item><description>Ignores properties in the YAML that don't have matching class properties</description></item>
    /// </list>
    /// <para>
    /// You can customize this to use different naming conventions or handling strategies.
    /// </para>
    /// </remarks>
    public IDeserializer FrontMatterDeserializer { get; init; } = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithCaseInsensitivePropertyMatching()
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Gets or sets the function that builds the Markdown pipeline used for parsing and rendering markdown content.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This pipeline factory creates a configured Markdig pipeline that processes markdown content
    /// with the necessary extensions and features for your application.
    /// </para>
    /// <para>
    /// The default configuration:
    /// </para>
    /// <list type="bullet">
    ///     <item><description>Enables advanced extensions for enhanced Markdown features</description></item>
    ///     <item><description>Supports YAML front matter parsing in Markdown documents</description></item>
    /// </list>
    /// <para>
    /// You can customize this to add additional extensions or configure different parsing options.
    /// </para>
    /// </remarks>
    public Func<IServiceProvider, MarkdownPipeline> MarkdownPipelineBuilder { get; init; } = serviceProvider =>
    {
        var roslynHighlighter = serviceProvider.GetService<IRoslynHighlighterService>();
        var roslynHighlighterOptions = serviceProvider.GetService<RoslynHighlighterOptions>();

        var builder = new MarkdownPipelineBuilder()
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub) // This sets up GitHub-style header IDs
            .UseAdvancedExtensions()
            .UseDiagrams()
            .UseAutoLinks()
            .UseCustomAlertBlocks()
            .UseCustomContainers()
            .UseSyntaxHighlighting(roslynHighlighter, roslynHighlighterOptions?.CodeHighlightRenderOptionsFactory)
            .UseTabbedCodeBlocks(roslynHighlighterOptions?.TabbedCodeBlockRenderOptionsFactory)
            .UseYamlFrontMatter();

        // If the service provider has a component registry, enable Mdazor support
        if (serviceProvider.GetService<IComponentRegistry>() != null)
            builder = builder.UseMdazor(serviceProvider);

        return builder.Build();
    };

    /// <summary>
    /// Url to use as the canonical base URL for the site, specifically for sitemaps, RSS feeds, and Open Graph metadata.
    /// </summary>
    public string? CanonicalBaseUrl { get; init; }
}