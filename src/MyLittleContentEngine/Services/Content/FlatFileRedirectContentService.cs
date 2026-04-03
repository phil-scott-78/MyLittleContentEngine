using System.Collections.Immutable;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content.TableOfContents;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// Optional content service that generates <c>filename.html</c> redirect files for pages
/// using folder-based output (<c>folder/index.html</c>). This provides backward compatibility
/// for sites that migrated from flat-file to folder-based URL output.
/// </summary>
/// <remarks>
/// <para>
/// For each page whose output file ends with <c>index.html</c> (e.g., <c>about/index.html</c>),
/// this service creates a sibling <c>about.html</c> file containing a meta-refresh redirect
/// to the canonical folder URL (<c>about/</c>).
/// </para>
/// <para>
/// Uses <see cref="IServiceProvider"/> for lazy resolution of content services to avoid a
/// circular dependency, following the same pattern as
/// <see cref="Spa.SpaNavigationContentService"/>.
/// </para>
/// </remarks>
internal class FlatFileRedirectContentService(IServiceProvider serviceProvider) : IContentService
{
    private const string IndexHtml = "index.html";

    /// <inheritdoc />
    public int SearchPriority => 0;

    /// <inheritdoc />
    public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync() =>
        Task.FromResult(ImmutableList<PageToGenerate>.Empty);

    /// <inheritdoc />
    public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync() =>
        Task.FromResult(ImmutableList<ContentTocItem>.Empty);

    /// <inheritdoc />
    public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() =>
        Task.FromResult(ImmutableList<ContentToCopy>.Empty);

    /// <inheritdoc />
    public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() =>
        Task.FromResult(ImmutableList<CrossReference>.Empty);

    /// <inheritdoc />
    public async Task<ImmutableList<ContentToCreate>> GetContentToCreateAsync()
    {
        var contentServices = serviceProvider.GetServices<IContentService>();
        var allPages = new List<PageToGenerate>();
        var existingFlatFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var service in contentServices)
        {
            if (service is FlatFileRedirectContentService) continue;

            var pages = await service.GetPagesToGenerateAsync();
            foreach (var page in pages)
            {
                allPages.Add(page);

                // Track non-folder output files so we don't generate colliding redirects
                if (!IsFolderBasedOutput(page.OutputFile))
                {
                    existingFlatFiles.Add(NormalizePath(page.OutputFile.Value));
                }
            }
        }

        var builder = ImmutableList.CreateBuilder<ContentToCreate>();
        var emittedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var page in allPages)
        {
            if (!IsFolderBasedOutput(page.OutputFile)) continue;

            var flatFilePath = GetFlatFilePath(page.OutputFile);
            if (flatFilePath is null) continue;

            var normalizedFlatPath = NormalizePath(flatFilePath);

            // Skip if another service already produces this file, or we already emitted it
            if (existingFlatFiles.Contains(normalizedFlatPath)) continue;
            if (!emittedPaths.Add(normalizedFlatPath)) continue;

            var redirectUrl = GetRelativeRedirectUrl(page.OutputFile);
            var html = RedirectHelper.GetRedirectHtml(redirectUrl);
            var bytes = Encoding.UTF8.GetBytes(html);

            builder.Add(new ContentToCreate(new FilePath(flatFilePath), bytes));
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Determines whether an output file uses the folder-based pattern (ends with <c>index.html</c>).
    /// </summary>
    internal static bool IsFolderBasedOutput(FilePath outputFile)
    {
        var normalized = NormalizePath(outputFile.Value);
        return normalized.EndsWith($"/{IndexHtml}", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals(IndexHtml, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Converts a folder-based output path to a flat file path.
    /// Returns <c>null</c> for the root <c>index.html</c>.
    /// </summary>
    /// <example>
    /// <c>about/index.html</c> becomes <c>about.html</c>;
    /// <c>docs/intro/index.html</c> becomes <c>docs/intro.html</c>.
    /// </example>
    internal static string? GetFlatFilePath(FilePath outputFile)
    {
        var normalized = NormalizePath(outputFile.Value);

        // Root index.html has no flat-file equivalent
        if (normalized.Equals(IndexHtml, StringComparison.OrdinalIgnoreCase))
            return null;

        // Strip /index.html suffix and append .html
        var folder = normalized[..^(IndexHtml.Length + 1)]; // remove "/index.html"
        return $"{folder}.html";
    }

    /// <summary>
    /// Computes the relative redirect URL for a folder-based output file.
    /// Uses the last path segment with a trailing slash so the redirect works
    /// regardless of the site's base URL.
    /// </summary>
    /// <example>
    /// For <c>about/index.html</c>, returns <c>about/</c>.
    /// For <c>docs/intro/index.html</c>, returns <c>intro/</c>.
    /// </example>
    internal static string GetRelativeRedirectUrl(FilePath outputFile)
    {
        var normalized = NormalizePath(outputFile.Value);

        // Strip /index.html to get the folder path
        var folder = normalized[..^(IndexHtml.Length + 1)];

        // Take the last segment as the relative redirect target
        var lastSlash = folder.LastIndexOf('/');
        var segment = lastSlash >= 0 ? folder[(lastSlash + 1)..] : folder;

        return $"{segment}/";
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/').TrimStart('/');
}
