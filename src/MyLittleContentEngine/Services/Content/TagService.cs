using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Models;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// Service for managing and processing tags in a MyLittleContentEngine site.
/// </summary>
/// <typeparam name="TFrontMatter">The type of front matter used in content.</typeparam>
public class TagService<TFrontMatter>
    where TFrontMatter : class, IFrontMatter
{
    private readonly ContentEngineContentOptions<TFrontMatter> _engineContentOptions;
    private readonly ILogger<TagService<TFrontMatter>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagService{TFrontMatter}"/> class.
    /// </summary>
    /// <param name="engineContentOptions">Content options containing tag configuration.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public TagService(
        ContentEngineContentOptions<TFrontMatter> engineContentOptions,
        ILogger<TagService<TFrontMatter>> logger)
    {
        _engineContentOptions = engineContentOptions;
        _logger = logger;
    }

    private Tag BuildTag(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            _logger.LogWarning("Attempted to build a tag with null or empty name");
            throw new ArgumentException("Tag name cannot be null, empty, or whitespace", nameof(tagName));
        }

        try
        {
            var tagEncodedName = _engineContentOptions.Tags.TagEncodeFunc(tagName);

            return new Tag
            {
                Name = tagName,
                EncodedName = tagEncodedName,
                NavigateUrl = $"{_engineContentOptions.Tags.TagsPageUrl}/{tagEncodedName}",
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building tag for '{TagName}'", tagName);
            throw new ContentEngineException($"Failed to build tag for '{tagName}'", ex);
        }
    }

    /// <summary>
    /// Extracts tags from the front matter if it supports tags.
    /// </summary>
    /// <param name="frontMatter">The front matter to extract tags from.</param>
    /// <returns>An immutable list of extracted tags.</returns>
    /// <exception cref="ArgumentNullException">Thrown when frontMatter is null.</exception>
    internal ImmutableList<Tag> ExtractTagsFromFrontMatter(TFrontMatter frontMatter)
    {
        if (frontMatter == null)
        {
            throw new ArgumentNullException(nameof(frontMatter), "Front matter cannot be null");
        }

        try
        {
            // Filter out any empty or null tags before processing
            var validTags = frontMatter.Tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .ToArray();

            _logger.LogTrace("Extracted {Count} tags from front matter", validTags.Length);
            return validTags.Select(BuildTag).ToImmutableList();
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            _logger.LogError(ex, "Error extracting tags from front matter");
            // Return an empty list instead of throwing to avoid breaking content processing
            return [];
        }
    }

    /// <summary>
    /// Gets all unique tags from a collection of content pages.
    /// </summary>
    /// <param name="contentPages">The collection of content pages to extract tags from.</param>
    /// <returns>A collection of unique tags.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="contentPages"/> is null.</exception>
    internal IEnumerable<Tag> GetUniqueTagsFromContentPages(IEnumerable<MarkdownContentPage<TFrontMatter>> contentPages)
    {
        if (contentPages == null)
        {
            throw new ArgumentNullException(nameof(contentPages), "Posts collection cannot be null");
        }

        try
        {
            var uniqueTags = contentPages
                .SelectMany(page => page.Tags)
                .DistinctBy(tag => tag.EncodedName)
                .ToList();

            _logger.LogTrace("Found {Count} unique tags across all content pages", uniqueTags.Count);
            return uniqueTags;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unique tags from content pages");
            return [];
        }
    }

    /// <summary>
    /// Finds posts matching a specific tag.
    /// </summary>
    /// <param name="posts">The collection of posts to search.</param>
    /// <param name="encodedTagName">The encoded tag name to match.</param>
    /// <returns>A collection of posts that have the specified tag.</returns>
    internal ImmutableList<MarkdownContentPage<TFrontMatter>> GetPostsByTag(
        IEnumerable<MarkdownContentPage<TFrontMatter>> posts,
        string encodedTagName)
    {
        ArgumentException.ThrowIfNullOrEmpty(encodedTagName);

        try
        {
            var matchingPosts = posts
                .Where(post => post.Tags.Any(tag => tag.EncodedName == encodedTagName))
                .ToImmutableList();

            _logger.LogDebug("Found {Count} posts with tag '{EncodedTagName}'", matchingPosts.Count, encodedTagName);
            return matchingPosts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts by tag '{EncodedTagName}'", encodedTagName);
            return ImmutableList<MarkdownContentPage<TFrontMatter>>.Empty;
        }
    }

    /// <summary>
    /// Finds a tag by its encoded name in a collection of posts.
    /// </summary>
    /// <param name="posts">The collection of posts to search for the tag.</param>
    /// <param name="encodedTagName">The encoded tag name to find.</param>
    /// <returns>The tag if found, otherwise null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="posts" /> is null or encodedTagName is null or empty.</exception>
    internal Tag? FindTagByEncodedName(IEnumerable<MarkdownContentPage<TFrontMatter>> posts, string encodedTagName)
    {
        ArgumentException.ThrowIfNullOrEmpty(encodedTagName);

        try
        {
            var tag = posts
                .SelectMany(post => post.Tags)
                .FirstOrDefault(tag => tag.EncodedName == encodedTagName);

            if (tag == null)
            {
                _logger.LogWarning("No tag found with encoded name '{EncodedTagName}'", encodedTagName);
            }

            return tag;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding tag by encoded name '{EncodedTagName}'", encodedTagName);
            return null;
        }
    }
}