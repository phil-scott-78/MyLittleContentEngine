using System.Collections.Immutable;
using System.IO.Abstractions;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content.TableOfContents;
using YamlDotNet.Core;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// Content service responsible for handling redirects from a _redirects.yml configuration file.
/// </summary>
/// <remarks>
/// <para>
/// This service reads a _redirects.yml file from the content root directory and exposes the
/// source-to-target mapping. At runtime, <see cref="Infrastructure.ContentEngineRedirectMiddleware"/>
/// uses this mapping to issue HTTP 301 responses. During static build,
/// <c>OutputGenerationService</c> detects those 301 responses and writes redirect HTML files.
/// </para>
/// <para>
/// The YAML format expected:
/// <code>
/// redirects:
///   /old-page: /new-page
///   /archived: https://archive.example.com
/// </code>
/// </para>
/// </remarks>
internal class RedirectContentService(
    ContentEngineOptions options,
    IFileSystem fileSystem,
    FilePathOperations filePathOperations) : IContentService
{
    private readonly AsyncLazy<IReadOnlyDictionary<string, string>> _mappingsCache =
        new(() => LoadMappingsAsync(options, fileSystem, filePathOperations));

    public int SearchPriority { get; } = 0;

    /// <summary>
    /// Returns the cached source-to-target redirect mapping loaded from _redirects.yml.
    /// Used by <see cref="Infrastructure.ContentEngineRedirectMiddleware"/> to resolve redirects at runtime.
    /// </summary>
    internal async Task<IReadOnlyDictionary<string, string>> GetRedirectMappingsAsync() => await _mappingsCache;

    /// <summary>
    /// Returns an empty list. Redirect HTML files are now written by <c>OutputGenerationService</c>
    /// when it intercepts the 301 responses issued by <see cref="Infrastructure.ContentEngineRedirectMiddleware"/>.
    /// </summary>
    public Task<ImmutableList<ContentToCreate>> GetContentToCreateAsync() =>
        Task.FromResult(ImmutableList<ContentToCreate>.Empty);

    /// <summary>
    /// Returns a <see cref="PageToGenerate"/> for each redirect source path so that the static
    /// generator crawls those URLs and captures the 301 responses produced by the middleware.
    /// </summary>
    public async Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync()
    {
        var mappings = await _mappingsCache;
        var pages = ImmutableList<PageToGenerate>.Empty;

        foreach (var sourcePath in mappings.Keys)
        {
            var normalizedPath = sourcePath.StartsWith('/') ? sourcePath : "/" + sourcePath;
            var outputFile = new FilePath($"{normalizedPath.TrimStart('/')}.html");
            pages = pages.Add(new PageToGenerate(new UrlPath(normalizedPath), outputFile));
        }

        return pages;
    }

    public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() =>
        Task.FromResult(ImmutableList<ContentTocItem>.Empty);

    public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() =>
        Task.FromResult(ImmutableList<ContentToCopy>.Empty);

    public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() =>
        Task.FromResult(ImmutableList<CrossReference>.Empty);

    private static Task<IReadOnlyDictionary<string, string>> LoadMappingsAsync(
        ContentEngineOptions options,
        IFileSystem fileSystem,
        FilePathOperations filePathOperations)
    {
        var root = options.ContentRootPath;
        var redirectFile = filePathOperations.Combine(root, "_redirects.yml");

        if (!filePathOperations.FileExists(redirectFile))
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>>(ImmutableDictionary<string, string>.Empty);
        }

        try
        {
            var yamlContent = fileSystem.File.ReadAllText(redirectFile.Value);

            if (string.IsNullOrWhiteSpace(yamlContent))
            {
                return Task.FromResult<IReadOnlyDictionary<string, string>>(ImmutableDictionary<string, string>.Empty);
            }

            var config = options.FrontMatterDeserializer.Deserialize<RedirectsConfig>(yamlContent);

            if (config.Redirects == null || config.Redirects.Count == 0)
            {
                return Task.FromResult<IReadOnlyDictionary<string, string>>(ImmutableDictionary<string, string>.Empty);
            }

            var result = new Dictionary<string, string>();

            foreach (var (sourcePath, targetUrl) in config.Redirects)
            {
                if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(targetUrl))
                {
                    continue;
                }

                var normalizedSource = sourcePath.StartsWith('/') ? sourcePath : "/" + sourcePath;
                result[normalizedSource] = targetUrl;
            }

            return Task.FromResult<IReadOnlyDictionary<string, string>>(result);
        }
        catch (YamlException)
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>>(ImmutableDictionary<string, string>.Empty);
        }
        catch (IOException ex)
        {
            throw new FileOperationException("Failed to read redirects file", redirectFile.Value, ex);
        }
        catch (Exception ex)
        {
            throw new FileOperationException("Error processing redirects file", redirectFile.Value, ex);
        }
    }
}

/// <summary>
/// Represents the structure of the _redirects.yml configuration file.
/// </summary>
internal class RedirectsConfig
{
    /// <summary>
    /// Gets or sets the dictionary mapping source paths to target URLs. Null if the redirects key is not present in the YAML.
    /// </summary>
    public Dictionary<string, string>? Redirects { get; set; }
}