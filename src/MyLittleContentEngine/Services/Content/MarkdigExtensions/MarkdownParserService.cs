using System.IO.Abstractions;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Renderers;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.Navigation;
using YamlDotNet.Serialization;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions;

/// <summary>
/// Service for parsing and processing Markdown files with YAML front matter.
/// Provides caching, HTML conversion, and image path transformation capabilities.
/// </summary>
internal class MarkdownParserService
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IFileSystem _fileSystem;
    private readonly LinkService _linkService;
    private readonly ContentEngineOptions _options;
    private readonly MarkdownPipeline _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref = "MarkdownParserService"/> class.
    /// </summary>
    /// <param name = "logger">Logger for recording operational information</param>
    /// <param name = "serviceProvider">Service provider for dependency resolution</param>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="linkService">The link service.</param>
    /// <param name = "options">Options for configuring the Markdown processing behavior</param>
    public MarkdownParserService(ILogger<MarkdownParserService> logger, IServiceProvider serviceProvider, IFileSystem fileSystem, LinkService linkService,
        ContentEngineOptions options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _fileSystem = fileSystem;
        _linkService = linkService;
        _options = options;

        _pipeline = options.MarkdownPipelineBuilder.Invoke(serviceProvider);
    }

    /// <summary>
    /// Parses a Markdown file, extracts the YAML front matter into a strongly typed object,
    /// and returns the Markdown content (with front matter removed) and table of contents.
    /// </summary>
    /// <typeparam name="T">Type to deserialize the YAML front matter into. Must have a parameterless constructor.</typeparam>
    /// <param name="filePath">Path to the Markdown file to be processed.</param>
    /// <param name="yamlDeserializer">Custom YAML deserializer instance. If null, the one from <see cref="ContentEngineOptions"/> will be used.</param>
    /// <param name="preProcessFile">Optional function to preprocess the Markdown content before parsing.</param>
    /// <returns>
    /// Tuple containing:
    /// - The deserialized front matter of type T
    /// - The Markdown content (with front matter removed)
    /// - The table of contents
    /// </returns>
    internal async Task<(T frontMatter, string markdownContent, OutlineEntry[] outline)> ParseMarkdownFileAsync<T>(
    string filePath,
    IDeserializer? yamlDeserializer = null,
    Func<IServiceProvider, string, string>? preProcessFile = null
) where T : IFrontMatter, new()
    {
        // Check if the file exists
        if (!_fileSystem.File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {filePath}", filePath);
            return (new T(), string.Empty, []); // Use Array.Empty for clarity
        }

        yamlDeserializer ??= _options.FrontMatterDeserializer;
        var rawMarkdownContent = await _fileSystem.File.ReadAllTextAsync(filePath); // Renamed to distinguish from processed

        var processedMarkdownContent = rawMarkdownContent;
        // Apply pre-processing if a preprocessor function was provided
        if (preProcessFile != null)
        {
            processedMarkdownContent = preProcessFile(_serviceProvider, processedMarkdownContent);
        }

        // Parse the Markdown content (the one that might have been pre-processed)
        var document = Markdown.Parse(processedMarkdownContent, _pipeline);

        // Extract the YAML front matter block and process it
        var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
        T frontMatter;
        string markdownWithoutFrontMatter;

        if (yamlBlock == null)
        {
            // No front matter found, create the default instance
            frontMatter = new T();
            markdownWithoutFrontMatter = processedMarkdownContent; // The whole content is Markdown
        }
        else
        {
            // Extract the YAML content
            var frontMatterYaml = yamlBlock.Lines.ToString();

            try
            {
                // Deserialize the YAML content into the specified type
                frontMatter = yamlDeserializer.Deserialize<T>(frontMatterYaml);
            }
            catch (Exception e)
            {
                // Handle deserialization errors by using default values
                frontMatter = new T();
                _logger.LogWarning(
                    "Cannot deserialize YAML front matter in {file}. The default one will be used! Error: {exceptionMessage}",
                    filePath, e.Message + (e.InnerException != null ? " Inner: " + e.InnerException.Message : string.Empty)); // Improved error logging
            }

            // Get the Markdown content without the front matter using the original string
            // yamlBlock.Span.End gives the character index in 'processedMarkdownContent'
            // immediately *after* the YAML block.
            var contentStartIndex = yamlBlock.Span.End + 1;

            // We need to be careful: Span.End is the offset of the character *after* the block.
            // Typically, there's a newline after the closing '---' of the YAML block.
            // We want to get the content *after* this potential newline.
            // This will take the substring and then remove any leading whitespace
            // (like newlines that separated the YAML block from the content).
            markdownWithoutFrontMatter = contentStartIndex < processedMarkdownContent.Length
                ? processedMarkdownContent[contentStartIndex..].TrimStart()
                : string.Empty;

            // Remove the YAML block from the document object *before* generating outline
            // This ensures outline is generated only from the actual content.
            document.Remove(yamlBlock);
        }

        // Generate outline from the document *without* the front matter
        var outline = MarkdownOutlineGenerator.GenerateOutline(document);

        // Ensure the variable name in return matches the declared one (case sensitivity)
        return (frontMatter, markdownWithoutFrontMatter, outline);
    }
    /// <summary>
    /// Renders markdown content to HTML using the configured pipeline and base URL.
    /// </summary>
    /// <param name="markdownContent">The Markdown content to render.</param>
    /// <param name="baseUrl">The base URL for link rewriting.</param>
    /// <returns>The rendered HTML string.</returns>
    public string RenderMarkdownToHtml(string markdownContent, string baseUrl)
    {
        var document = Markdown.Parse(markdownContent, _pipeline);
        using var writer = new StringWriter();
        var htmlRenderer = new HtmlRenderer(writer)
        {
            LinkRewriter = s => LinkRewriter.RewriteUrl(s, baseUrl)
        };
        _pipeline.Setup(htmlRenderer);
        htmlRenderer.Render(document);
        writer.Flush();
        return writer.ToString();
    }
}