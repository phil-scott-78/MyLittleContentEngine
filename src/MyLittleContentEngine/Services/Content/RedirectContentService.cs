using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Text;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content.TableOfContents;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// Content service responsible for generating redirect HTML files from a _redirects.yml configuration file.
/// </summary>
/// <remarks>
/// <para>
/// This service reads a _redirects.yml file from the content root directory and generates
/// static HTML redirect files for each defined redirect mapping.
/// </para>
/// <para>
/// The YAML format expected:
/// <code>
/// redirects:
///   /old-page: /new-page
///   /archived: https://archive.example.com
/// </code>
/// </para>
/// <para>
/// For each redirect, generates an HTML file using meta refresh and includes a fallback link.
/// Source paths like "/old-page" generate files named "old-page.html" in the output directory.
/// </para>
/// </remarks>
internal class RedirectContentService(
    ContentEngineOptions contentOptions,
    IFileSystem fileSystem,
    FilePathOperations filePathOperations,
    ContentEngineOptions engineOptions) : IContentService
{
    private readonly IDeserializer _yamlDeserializer = engineOptions.FrontMatterDeserializer;

    public int SearchPriority { get; } = 0;



    public Task<ImmutableList<ContentToCreate>> GetContentToCreateAsync()
    {
        var root = contentOptions.ContentRootPath;
        var redirectFile = filePathOperations.Combine(root, "_redirects.yml");
        if (!filePathOperations.FileExists(redirectFile))
        {
            return Task.FromResult(ImmutableList<ContentToCreate>.Empty);
        }

        try
        {
            // Read file content
            var yamlContent = fileSystem.File.ReadAllText(redirectFile.Value);

            if (string.IsNullOrWhiteSpace(yamlContent))
            {
                return Task.FromResult(ImmutableList<ContentToCreate>.Empty);
            }

            // Deserialize YAML
            var config = _yamlDeserializer.Deserialize<RedirectsConfig>(yamlContent);

            if (config?.Redirects == null || config.Redirects.Count == 0)
            {
                return Task.FromResult(ImmutableList<ContentToCreate>.Empty);
            }

            // Process redirects
            var contentToCreate = ImmutableList<ContentToCreate>.Empty;

            foreach (var (sourcePath, targetUrl) in config.Redirects)
            {
                try
                {
                    // Validate entries
                    if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(targetUrl))
                    {
                        continue;
                    }

                    // Normalize source path - remove leading slash
                    var normalizedPath = sourcePath.TrimStart('/');

                    // Create target file path with .html extension
                    var targetPath = new FilePath($"{normalizedPath}.html");

                    // Generate redirect HTML
                    var redirectHtml = RedirectHelper.GetRedirectHtml(targetUrl);

                    // Convert to UTF-8 bytes
                    var htmlBytes = Encoding.UTF8.GetBytes(redirectHtml);

                    // Create ContentToCreate instance
                    var content = new ContentToCreate(targetPath, htmlBytes);
                    contentToCreate = contentToCreate.Add(content);
                }
                catch
                {
                    // Skip invalid entries and continue processing
                    continue;
                }
            }

            return Task.FromResult(contentToCreate);
        }
        catch (YamlException)
        {
            // YAML parsing error - return empty list
            return Task.FromResult(ImmutableList<ContentToCreate>.Empty);
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
    
    public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync() => Task.FromResult(ImmutableList<PageToGenerate>.Empty);

    public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() => Task.FromResult(ImmutableList<ContentTocItem>.Empty);

    public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() => Task.FromResult(ImmutableList<ContentToCopy>.Empty);

    public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() => Task.FromResult(ImmutableList<CrossReference>.Empty);
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