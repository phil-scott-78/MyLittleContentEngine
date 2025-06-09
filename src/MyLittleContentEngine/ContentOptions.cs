using System.Collections.Immutable;
using MyLittleContentEngine.Models;

namespace MyLittleContentEngine;

/// <summary>
/// Defines the contract for Blazor Static content configuration options.
/// </summary>
internal interface IContentOptions
{
    /// <summary>
    /// Gets the path where content files are stored.
    /// </summary>
    string ContentPath { get; init; }

    /// <summary>
    /// Gets the URL path component for the page that displays content.
    /// </summary>
    string BasePageUrl { get; init; }
}

/// <summary>
/// Provides configuration options for processing markdown files with front matter in Blazor Static sites.
/// </summary>
/// <typeparam name="TFrontMatter">The type that represents the front matter data. Must implement IFrontMatter.</typeparam>
/// <remarks>
/// <para>
/// This class defines how markdown content files are processed, where they're located,
/// and how they're transformed before being rendered by Blazor components.
/// </para>
/// </remarks>
public class ContentEngineContentOptions<TFrontMatter> : IContentOptions
    where TFrontMatter : class, IFrontMatter
{
    /// <summary>
    /// Gets or sets the folder path relative to the project root where content files are stored.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default value is "Content/Blog".
    /// </para>
    /// </remarks>
    public string ContentPath { get; init; } = "Content/Blog";

    /// <summary>
    /// Gets or sets the file pattern used to identify content files in the ContentPath.
    /// </summary>
    /// <remarks>
    /// The default value is "*.md" to match all Markdown files.
    /// </remarks>
    public string PostFilePattern { get; init; } = "*.md";

    /// <summary>
    /// Gets or sets the URL path component for the page that displays the content.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This value should correspond to the route specified in your Blazor page.
    /// For example, if your page is defined with @page "/blog", set PageUrl to "blog".
    /// </para>
    /// <para>
    /// This value also serves as the generated folder name for static content.
    /// Using this property in code helps avoid magic strings in .razor files.
    /// </para>
    /// <para>
    /// The default value is "blog".
    /// </para>
    /// </remarks>
    public string BasePageUrl { get; init; } = "blog";

    /// <summary>
    /// Gets or sets a hook to process markdown content before it is rendered as HTML.
    /// </summary>
    /// <remarks>
    /// This function takes an IServiceProvider and the raw Markdown string
    /// and returns the processed Markdown string.
    /// </remarks>
    public Func<IServiceProvider, string, string> PreProcessMarkdown { get; init; } = (_, s) => s;

    /// <summary>
    /// Gets or sets a hook to process the front matter and HTML after Markdown parsing but before passing to Razor.
    /// </summary>
    /// <remarks>
    /// This function takes an IServiceProvider, the parsed front matter, and the HTML content,
    /// and returns a tuple containing potentially modified versions of both.
    /// </remarks>
    public Func<IServiceProvider, TFrontMatter, string, (TFrontMatter, string)> PostProcessHtml { get; init; } =
        (_, frontMatter, html) => (frontMatter, html);

    /// <summary>
    /// Gets a list of routes to exclude from static content generation.
    /// </summary>
    /// <remarks>
    /// Routes specified here will not be included in the generated static output.
    /// </remarks>
    public ImmutableList<string> ExcludedMapRoutes { get; init; } = [];

    /// <summary>
    /// Gets or sets the options related to tag functionality.
    /// </summary>
    public TagsOptions Tags { get; init; } = new();
}

/// <summary>
/// Provides configuration options for tag-based content navigation.
/// </summary>
/// <remarks>
/// Controls how tags are processed, displayed, and linked throughout the site.
/// </remarks>
public class TagsOptions
{
    /// <summary>
    /// Gets or sets the URL path component for the page that displays all tags.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This value should correspond to the route specified in your Blazor page.
    /// For example, if your "tags" page is defined with @page "/tags", set TagsPageUrl to "tags".
    /// </para>
    /// <para>
    /// Using this property in code helps avoid magic strings in .razor files.
    /// </para>
    /// <para>
    /// The default value is "tags".
    /// </para>
    /// </remarks>
    public string TagsPageUrl { get; init; } = "tags";

    /// <summary>
    /// Gets or sets the function used to encode tag strings into URL-friendly formats.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This function transforms a raw tag string into a format suitable for use in URLs and filenames.
    /// </para>
    /// <para>
    /// Important: The same encoding function must be used consistently when creating tag links
    /// throughout the application to ensure proper navigation.
    /// </para>
    /// <para>
    /// The default implementation uses the Slugify() extension method to create URL-friendly strings.
    /// </para>
    /// </remarks>
    public Func<string, string> TagEncodeFunc { get; init; } = s => s.Slugify();
}