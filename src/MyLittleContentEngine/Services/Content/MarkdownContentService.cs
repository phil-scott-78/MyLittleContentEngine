using System.Collections.Concurrent;
using System.Collections.Immutable;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content.MarkdigExtensions;
using MyLittleContentEngine.Services.Content.TableOfContents;
using MyLittleContentEngine.Services.Infrastructure;

namespace MyLittleContentEngine.Services.Content;

public interface IMarkdownContentService<TFrontMatter> : IContentService where TFrontMatter : class, IFrontMatter, new()
{
    /// <summary>
    /// Gets content by its URL and renders the Markdown to HTML. Returns null if not found.
    /// </summary>
    /// <param name="url">The URL identifier of the content page to retrieve</param>
    /// <returns>A tuple of the content page and rendered HTML, or null if not found</returns>
    Task<(MarkdownContentPage<TFrontMatter> Page, string HtmlContent)?>
        GetRenderedContentPageByUrlOrDefault(string url);

    /// <summary>
    /// Gets an immutable list of all content.
    /// </summary>
    /// <returns>An immutable list containing all parsed and processed content pages.</returns>
    Task<ImmutableList<MarkdownContentPage<TFrontMatter>>> GetAllContentPagesAsync();

    Task<(Tag Tag, ImmutableList<MarkdownContentPage<TFrontMatter>> ContentPages)?> GetTagByEncodedNameOrDefault(
        string encodedName);
}

/// <summary>
///     Content service responsible for managing Markdown-based content in a Blazor static site.
///     This service handles parsing markdown files, extracting front matter, generating HTML, and tracking tags.
/// </summary>
/// <typeparam name="TFrontMatter">
///     The type of front matter metadata used in content files.
///     Must implement IFrontMatter and have a parameterless constructor.
/// </typeparam>
internal class MarkdownContentService<TFrontMatter> : IDisposable, IMarkdownContentService<TFrontMatter> where TFrontMatter : class, IFrontMatter, new()
{
    /// <inheritdoc />
    public int SearchPriority => 10; // High priority for markdown content
    private readonly LazyAndForgetful<ConcurrentDictionary<string, MarkdownContentPage<TFrontMatter>>> _contentCache;
    private readonly MarkdownContentProcessor<TFrontMatter> _contentProcessor;
    private readonly TagService<TFrontMatter> _tagService;
    private readonly ContentFilesService<TFrontMatter> _contentFilesService;
    private readonly MarkdownParserService _markdownParserService;
    private bool _isDisposed; // To detect redundant calls

    /// <summary>
    ///     Initializes a new instance of the MarkdownContentService.
    /// </summary>
    /// <param name="engineContentOptions">Configuration options specific to content handling</param>
    /// <param name="fileWatcher">File watcher for hot-reload functionality</param>
    /// <param name="tagService">Service for handling tags</param>
    /// <param name="contentFilesService">Service for handling content files</param>
    /// <param name="markdownParserService">Service for handling Markdown parsing</param>
    /// <param name="contentProcessor">Service for processing Markdown content</param>
    public MarkdownContentService(
        ContentEngineContentOptions<TFrontMatter> engineContentOptions,
        IContentEngineFileWatcher fileWatcher,
        TagService<TFrontMatter> tagService,
        ContentFilesService<TFrontMatter> contentFilesService,
        MarkdownParserService markdownParserService,
        MarkdownContentProcessor<TFrontMatter> contentProcessor)
    {
        _tagService = tagService;
        _contentFilesService = contentFilesService;
        _contentProcessor = contentProcessor;
        _markdownParserService = markdownParserService;

        // Set up the Post cache
        _contentCache =
            new LazyAndForgetful<ConcurrentDictionary<string, MarkdownContentPage<TFrontMatter>>>(async () =>
                await _contentProcessor.ProcessContentFiles());

        // Set up file watching
        fileWatcher.AddPathsWatch([engineContentOptions.ContentPath], NeedsRefresh);
    }

    /// <summary>
    ///     Marks the Posts collection as needing a refresh.
    ///     This is called when content files change and during hot-reload events.
    /// </summary>
    private void NeedsRefresh() => _contentCache.Refresh();

    private async Task<MarkdownContentPage<TFrontMatter>?> GetContentPageByUrlOrDefault(string url)
    {
        var data = await _contentCache.Value;
        return data.GetValueOrDefault(url);
    }

    /// <summary>
    /// Gets content by its URL and renders the Markdown to HTML. Returns null if not found.
    /// </summary>
    /// <param name="url">The URL identifier of the content page to retrieve</param>
    /// <returns>A tuple of the content page and rendered HTML, or null if not found</returns>
    public async Task<(MarkdownContentPage<TFrontMatter> Page, string HtmlContent)?>
        GetRenderedContentPageByUrlOrDefault(string url)
    {
        var page = await GetContentPageByUrlOrDefault(url);
        if (page == null) return null;

        // Use the parser service to render Markdown to HTML
        // Use the page's NavigateUrl as the base URL for link rewriting
        var lastSlash = page.NavigateUrl.LastIndexOf('/');
        var pageUrl = lastSlash == -1 ? page.NavigateUrl : page.NavigateUrl[..lastSlash];

        var html = _markdownParserService.RenderMarkdownToHtml(page.MarkdownContent, pageUrl);
        return (page, html);
    }

    /// <summary>
    /// Gets an immutable list of all content.
    /// </summary>
    /// <returns>An immutable list containing all parsed and processed content pages.</returns>
    public async Task<ImmutableList<MarkdownContentPage<TFrontMatter>>> GetAllContentPagesAsync()
    {
        var data = await _contentCache.Value;
        return data.Values.ToImmutableList();
    }

    /// <summary>
    /// Retrieves a tag by its encoded name along with all associated content, or returns null if the tag is not found.
    /// </summary>
    /// <param name="encodedName">The encoded name of the tag to retrieve.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains a tuple with the tag and its associated content pages if found, or null if no tag matches.
    /// </returns>
    public async Task<(Tag Tag, ImmutableList<MarkdownContentPage<TFrontMatter>> ContentPages)?>
        GetTagByEncodedNameOrDefault(
            string encodedName)
    {
        var allPosts = await GetAllContentPagesAsync();
        var contentPagesForTag = _tagService.GetPostsByTag(allPosts, encodedName);

        if (contentPagesForTag.Count == 0) return null;

        var tag = _tagService.FindTagByEncodedName(allPosts, encodedName);
        if (tag == null) return null;

        return (tag, contentPagesForTag);
    }

    /// <inheritdoc />
    async Task<ImmutableList<PageToGenerate>> IContentService.GetPagesToGenerateAsync()
    {
        var allPosts = await GetAllContentPagesAsync();
        return _contentProcessor.CreatePagesToGenerate(allPosts);
    }

    /// <inheritdoc />
    async Task<ImmutableList<ContentTocItem>> IContentService.GetContentTocEntriesAsync()
    {
        var pages = await ((IContentService)this).GetPagesToGenerateAsync();
        return pages.Where(p => p.Metadata?.Title != null)
            .Select(p => new ContentTocItem(
                p.Metadata!.Title!,
                p.Url,
                p.Metadata.Order,
                CreateHierarchyParts(p.Url)))
            .ToImmutableList();
    }

    private static string[] CreateHierarchyParts(string url)
    {
        // Convert URL to hierarchy parts by splitting on '/' and removing empty entries
        return url.Trim('/').Split(['/'], StringSplitOptions.RemoveEmptyEntries);
    }

    /// <inheritdoc />
    Task<ImmutableList<ContentToCopy>> IContentService.GetContentToCopyAsync()
    {
        return Task.FromResult(_contentFilesService.GetContentToCopy());
    }

    /// <inheritdoc />
    public async Task<ImmutableList<CrossReference>> GetCrossReferencesAsync()
    {
        var data = await _contentCache.Value;
        if (data.IsEmpty)
        {
            return ImmutableList<CrossReference>.Empty;
        }

        var allContent = data.Values;
        return allContent.Select(i => new CrossReference()
        {
            Uid = i.FrontMatter.Uid,
            Title = i.FrontMatter.Title,
            Url = i.NavigateUrl
        }).ToImmutableList();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects).
                _contentCache.Dispose();
            }

            _isDisposed = true;
        }
    }

    ~MarkdownContentService()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
