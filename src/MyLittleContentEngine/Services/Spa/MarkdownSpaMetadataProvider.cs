using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;

namespace MyLittleContentEngine.Services.Spa;

/// <summary>
/// Metadata provider that extracts title and description from markdown front matter.
/// </summary>
public class MarkdownSpaMetadataProvider<TFrontMatter>(
    IMarkdownContentService<TFrontMatter> contentService) : ISpaPageMetadataProvider
    where TFrontMatter : class, IFrontMatter, new()
{
    /// <inheritdoc />
    public async Task<SpaPageMetadata?> GetMetadataAsync(string url)
    {
        var result = await contentService.GetRenderedContentPageByUrlOrDefault(url);
        if (result is null) return null;

        var fm = result.Value.Page.FrontMatter;
        var meta = fm.AsMetadata();
        return new SpaPageMetadata(fm.Title, meta.Description);
    }
}
