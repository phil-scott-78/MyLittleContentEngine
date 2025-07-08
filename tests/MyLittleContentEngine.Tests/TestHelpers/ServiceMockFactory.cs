using System.Collections.Immutable;
using Moq;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;
using MyLittleContentEngine.Services.Generation;

namespace MyLittleContentEngine.Tests.TestHelpers;

/// <summary>
/// Factory for creating common service mocks used in testing scenarios.
/// </summary>
/// <remarks>
/// This factory provides pre-configured mocks for frequently used services,
/// reducing boilerplate code in test setup and ensuring consistent mock behavior.
/// </remarks>
public static class ServiceMockFactory
{
    /// <summary>
    /// Creates a mock IContentService with predefined pages.
    /// </summary>
    /// <param name="pages">Array of page tuples (title, url, order).</param>
    /// <returns>A mock IContentService.</returns>
    public static Mock<IContentService> CreateContentService(params (string title, string url, int order)[] pages)
    {
        var mock = new Mock<IContentService>();
        var pageList = pages.Select(p => new PageToGenerate(
            p.url,
            p.url,
            new Metadata { Title = p.title, Order = p.order }
        )).ToImmutableList();

        mock.Setup(x => x.GetPagesToGenerateAsync())
            .ReturnsAsync(pageList);
        
        mock.Setup(x => x.GetContentTocEntriesAsync())
            .ReturnsAsync(pageList.Select(p => new ContentTocItem(p.Metadata!.Title!, p.Url, p.Metadata.Order, p.Url.Trim('/').Split(['/'], StringSplitOptions.RemoveEmptyEntries))).ToImmutableList());
        
        mock.Setup(x => x.GetContentToCopyAsync())
            .ReturnsAsync(ImmutableList<ContentToCopy>.Empty);
        
        mock.Setup(x => x.GetCrossReferencesAsync())
            .ReturnsAsync(ImmutableList<CrossReference>.Empty);

        return mock;
    }

    /// <summary>
    /// Creates a mock IContentService that returns empty collections.
    /// </summary>
    /// <returns>A mock IContentService with no content.</returns>
    public static Mock<IContentService> CreateEmptyContentService()
    {
        var mock = new Mock<IContentService>();
        
        mock.Setup(x => x.GetPagesToGenerateAsync())
            .ReturnsAsync(ImmutableList<PageToGenerate>.Empty);
        
        mock.Setup(x => x.GetContentTocEntriesAsync())
            .ReturnsAsync(ImmutableList<ContentTocItem>.Empty);
        
        mock.Setup(x => x.GetContentToCopyAsync())
            .ReturnsAsync(ImmutableList<ContentToCopy>.Empty);
        
        mock.Setup(x => x.GetCrossReferencesAsync())
            .ReturnsAsync(ImmutableList<CrossReference>.Empty);

        return mock;
    }

    /// <summary>
    /// Creates a mock IContentService with content to copy.
    /// </summary>
    /// <param name="contentToCopy">Array of content to copy items.</param>
    /// <returns>A mock IContentService with static content.</returns>
    public static Mock<IContentService> CreateContentServiceWithStaticContent(params ContentToCopy[] contentToCopy)
    {
        var mock = CreateEmptyContentService();
        
        mock.Setup(x => x.GetContentToCopyAsync())
            .ReturnsAsync(contentToCopy.ToImmutableList());

        return mock;
    }

    /// <summary>
    /// Creates a mock IContentService with cross-references.
    /// </summary>
    /// <param name="crossReferences">Array of cross-reference items.</param>
    /// <returns>A mock IContentService with cross-references.</returns>
    public static Mock<IContentService> CreateContentServiceWithCrossReferences(params CrossReference[] crossReferences)
    {
        var mock = CreateEmptyContentService();
        
        mock.Setup(x => x.GetCrossReferencesAsync())
            .ReturnsAsync(crossReferences.ToImmutableList());

        return mock;
    }

    /// <summary>
    /// Creates a mock TableOfContentService with predefined content services.
    /// </summary>
    /// <param name="contentServices">Array of content services to include.</param>
    /// <returns>A configured TableOfContentService.</returns>
    internal static TableOfContentService CreateTableOfContentService(params IContentService[] contentServices)
    {
        return new TableOfContentService(contentServices.ToList());
    }

    /// <summary>
    /// Creates a collection of mock content services for multi-service scenarios.
    /// </summary>
    /// <param name="serviceConfigs">Array of service configurations (name, pages).</param>
    /// <returns>Array of mock IContentService instances.</returns>
    public static IContentService[] CreateMultipleContentServices(params (string serviceName, (string title, string url, int order)[] pages)[] serviceConfigs)
    {
        return serviceConfigs
            .Select(config => CreateContentService(config.pages).Object)
            .ToArray();
    }

    /// <summary>
    /// Creates a mock OutputGenerationService for testing page generation.
    /// </summary>
    /// <returns>A mock OutputGenerationService.</returns>
    internal static Mock<OutputGenerationService> CreateOutputGenerationService()
    {
        var mock = new Mock<OutputGenerationService>();
        return mock;
    }

    /// <summary>
    /// Helper class for creating PageToGenerate instances with minimal configuration.
    /// </summary>
    public static class PageBuilder
    {
        /// <summary>
        /// Creates a simple PageToGenerate with basic metadata.
        /// </summary>
        /// <param name="title">Page title.</param>
        /// <param name="url">Page URL.</param>
        /// <param name="order">Page order.</param>
        /// <returns>A PageToGenerate instance.</returns>
        public static PageToGenerate Create(string title, string url, int order = 1)
        {
            return new PageToGenerate(
                url,
                url,
                new Metadata { Title = title, Order = order }
            );
        }

        /// <summary>
        /// Creates a PageToGenerate with rich metadata.
        /// </summary>
        /// <param name="title">Page title.</param>
        /// <param name="url">Page URL.</param>
        /// <param name="order">Page order.</param>
        /// <param name="tags">Page tags.</param>
        /// <param name="isDraft">Whether the page is a draft.</param>
        /// <returns>A PageToGenerate instance.</returns>
        public static PageToGenerate CreateRich(
            string title, 
            string url, 
            int order = 1, 
            string[]? tags = null, 
            bool isDraft = false)
        {
            return new PageToGenerate(
                url,
                url,
                new Metadata 
                { 
                    Title = title, 
                    Order = order
                }
            );
        }
    }

    /// <summary>
    /// Helper class for creating ContentToCopy instances.
    /// </summary>
    public static class ContentToCopyBuilder
    {
        /// <summary>
        /// Creates a ContentToCopy instance for static file copying.
        /// </summary>
        /// <param name="sourceFile">Source file path.</param>
        /// <param name="destinationFile">Destination file path.</param>
        /// <returns>A ContentToCopy instance.</returns>
        public static ContentToCopy Create(string sourceFile, string destinationFile)
        {
            return new ContentToCopy(sourceFile, destinationFile);
        }

        /// <summary>
        /// Creates multiple ContentToCopy instances from path pairs.
        /// </summary>
        /// <param name="pathPairs">Array of (source, destination) path pairs.</param>
        /// <returns>Array of ContentToCopy instances.</returns>
        public static ContentToCopy[] CreateMultiple(params (string source, string destination)[] pathPairs)
        {
            return pathPairs
                .Select(pair => Create(pair.source, pair.destination))
                .ToArray();
        }
    }

    /// <summary>
    /// Helper class for creating CrossReference instances.
    /// </summary>
    public static class CrossReferenceBuilder
    {
        /// <summary>
        /// Creates a CrossReference instance.
        /// </summary>
        /// <param name="uid">Unique identifier for the cross-reference.</param>
        /// <param name="title">Title of the cross-reference.</param>
        /// <param name="url">URL of the cross-reference.</param>
        /// <returns>A CrossReference instance.</returns>
        public static CrossReference Create(string uid, string title, string url)
        {
            return new CrossReference { Uid = uid, Title = title, Url = url };
        }

        /// <summary>
        /// Creates multiple CrossReference instances.
        /// </summary>
        /// <param name="references">Array of (uid, title, url) tuples.</param>
        /// <returns>Array of CrossReference instances.</returns>
        public static CrossReference[] CreateMultiple(params (string uid, string title, string url)[] references)
        {
            return references
                .Select(r => Create(r.uid, r.title, r.url))
                .ToArray();
        }
    }
}