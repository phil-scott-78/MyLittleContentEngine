using System.Collections.Concurrent;
using System.Collections.Immutable;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content.MarkdigExtensions;
using MyLittleContentEngine.Services.Content.TableOfContents;

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
    private readonly AsyncLazy<ConcurrentDictionary<string, MarkdownContentPage<TFrontMatter>>> _contentCache;
    private readonly MarkdownContentProcessor<TFrontMatter> _contentProcessor;
    private readonly TagService<TFrontMatter> _tagService;
    private readonly ContentFilesService<TFrontMatter> _contentFilesService;
    private readonly MarkdownParserService _markdownParserService;
    private readonly MarkdownContentOptions<TFrontMatter> _markdownContentOptions;

    private bool _isDisposed; // To detect redundant calls

    /// <summary>
    ///     Initializes a new instance of the MarkdownContentService.
    /// </summary>
    /// <param name="markdownContentOptions">Configuration options specific to content handling</param>
    /// <param name="tagService">Service for handling tags</param>
    /// <param name="contentFilesService">Service for handling content files</param>
    /// <param name="markdownParserService">Service for handling Markdown parsing</param>
    /// <param name="contentProcessor">Service for processing Markdown content</param>
    public MarkdownContentService(
        MarkdownContentOptions<TFrontMatter> markdownContentOptions,
        TagService<TFrontMatter> tagService,
        ContentFilesService<TFrontMatter> contentFilesService,
        MarkdownParserService markdownParserService,
        MarkdownContentProcessor<TFrontMatter> contentProcessor)
    {
        _markdownContentOptions = markdownContentOptions;
        _tagService = tagService;
        _contentFilesService = contentFilesService;
        _contentProcessor = contentProcessor;
        _markdownParserService = markdownParserService;

        // Set up the Post cache - AsyncLazy handles thread-safe initialization
        _contentCache = new AsyncLazy<ConcurrentDictionary<string, MarkdownContentPage<TFrontMatter>>>(
            async () => await _contentProcessor.ProcessContentFiles(),
            AsyncLazyFlags.RetryOnFailure);
    }


    private async Task<MarkdownContentPage<TFrontMatter>?> GetContentPageByUrlOrDefault(string url)
    {
        var data = await _contentCache;
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

        // Check if this is a redirect page
        if (!string.IsNullOrEmpty(page.FrontMatter.RedirectUrl))
        {
            var redirectHtml = RedirectHelper.GetRedirectHtml(page.FrontMatter.RedirectUrl);

            return (page, redirectHtml);
        }

        // Use the parser service to render Markdown to HTML
        // Use the page's Url as the base URL for link rewriting
        var lastSlash = page.Url.LastIndexOf('/');
        var pageUrl = lastSlash == -1 ? page.Url : page.Url[..lastSlash];

        var html = _markdownParserService.RenderMarkdownToHtml(page.MarkdownContent, pageUrl);
        return (page, html);
    }

    /// <summary>
    /// Gets an immutable list of all content.
    /// </summary>
    /// <returns>An immutable list containing all parsed and processed content pages.</returns>
    public async Task<ImmutableList<MarkdownContentPage<TFrontMatter>>> GetAllContentPagesAsync()
    {
        var data = await _contentCache;
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
        var allContentPages = await GetAllContentPagesAsync();

        var defaultSection = _markdownContentOptions.TableOfContentsSectionKey;
    
        return pages.Where(p => p.Metadata?.Title != null)
            .Where(p =>
            {
                // Exclude redirect pages from table of contents
                var contentPage = allContentPages.FirstOrDefault(cp => cp.Url == p.Url);
                return contentPage?.FrontMatter.RedirectUrl == null;
            })
            .Select(p =>
            {
                var contentPage = allContentPages.FirstOrDefault(cp => cp.Url == p.Url);
                var section = contentPage?.FrontMatter.Section ?? defaultSection;
                return new ContentTocItem(
                    p.Metadata!.Title!,
                    p.Url,
                    p.Metadata.Order,
                    CreateHierarchyParts(p.Url),
                    section);
            })
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
        var data = await _contentCache;
        if (data.IsEmpty)
        {
            return ImmutableList<CrossReference>.Empty;
        }

        var allContent = data.Values;
        return allContent.Select(i => new CrossReference()
        {
            Uid = i.FrontMatter.Uid,
            Title = i.FrontMatter.Title,
            Url = i.Url
        }).ToImmutableList();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // AsyncLazy doesn't need explicit disposal
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
