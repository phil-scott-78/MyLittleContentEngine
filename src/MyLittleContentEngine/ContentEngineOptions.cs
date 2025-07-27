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
    /// Url to use as the canonical base URL for the site, specifically for sitemaps, RSS feeds, and Open Graph metadata.
    /// </summary>
    /// <para>
    /// Example format: "https://example.com" (without a trailing slash)
    /// </para>
    public string? CanonicalBaseUrl { get; init; }
    
    /// <summary>
    /// Gets or sets the path to the content root directory. Defaults to "Content".
    /// </summary>
    public string ContentRootPath { get; init; } = "Content";


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
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
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
            .UseAlertBlocks()
            .UseAbbreviations()
            .UseCitations()
            .UseCustomContainers()
            .UseDefinitionLists()
            .UseEmphasisExtras()
            .UseFigures()
            .UseFooters()
            .UseFootnotes()
            .UseGridTables()
            .UseMathematics()
            .UseMediaLinks()
            .UsePipeTables()
            .UseListExtras()
            .UseTaskLists()
            .UseAutoLinks()
            .UseGenericAttributes()
            .UseDiagrams()
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
}