using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services;

namespace MyLittleContentEngine.Services.Infrastructure;

/// <summary>
/// Service that resolves cross-references (xref) by looking up UIDs across all registered IContentService instances.
/// Uses AsyncLazy caching to efficiently manage cross-reference lookups.
/// </summary>
public interface IXrefResolver
{
    /// <summary>
    /// Resolves an xref UID to its corresponding URL.
    /// </summary>
    /// <param name="uid">The UID to resolve (e.g., "System.String")</param>
    /// <returns>The resolved URL if found, null otherwise</returns>
    Task<string?> ResolveAsync(string uid);

    /// <summary>
    /// Resolves an xref UID to its corresponding CrossReference containing both URL and title.
    /// </summary>
    /// <param name="uid">The UID to resolve (e.g., "System.String")</param>
    /// <returns>The resolved CrossReference if found, null otherwise</returns>
    Task<CrossReference?> ResolveToReferenceAsync(string uid);
}

/// <summary>
/// Implementation of IXrefResolver that caches cross-references from all content services.
/// </summary>
public class XrefResolver : IXrefResolver, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<XrefResolver> _logger;
    private readonly AsyncLazy<ImmutableDictionary<string, CrossReference>> _crossReferencesCache;
    private bool _disposed;

    public XrefResolver(
        IServiceProvider serviceProvider,
        ILogger<XrefResolver> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Initialize the cache - AsyncLazy handles thread-safe initialization
        _crossReferencesCache = new AsyncLazy<ImmutableDictionary<string, CrossReference>>(
            BuildCrossReferenceDictionaryAsync,
            AsyncLazyFlags.RetryOnFailure);
    }

    /// <summary>
    /// Resolves an xref UID to its corresponding URL.
    /// </summary>
    /// <param name="uid">The UID to resolve</param>
    /// <returns>The resolved URL if found, null otherwise</returns>
    public async Task<string?> ResolveAsync(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid))
            return null;

        try
        {
            var crossReferences = await _crossReferencesCache;
            var crossRef = CollectionExtensions.GetValueOrDefault(crossReferences, uid);
            return crossRef?.Url;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve xref: {Uid}", uid);
            return null;
        }
    }

    /// <summary>
    /// Resolves an xref UID to its corresponding CrossReference containing both URL and title.
    /// </summary>
    /// <param name="uid">The UID to resolve</param>
    /// <returns>The resolved CrossReference if found, null otherwise</returns>
    public async Task<CrossReference?> ResolveToReferenceAsync(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid))
            return null;

        try
        {
            var crossReferences = await _crossReferencesCache;
            return CollectionExtensions.GetValueOrDefault(crossReferences, uid);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve xref: {Uid}", uid);
            return null;
        }
    }

    /// <summary>
    /// Builds the cross-reference dictionary by collecting from all registered IContentService instances.
    /// </summary>
    private async Task<ImmutableDictionary<string, CrossReference>> BuildCrossReferenceDictionaryAsync()
    {
        _logger.LogDebug("Building cross-reference dictionary from all content services");

        var builder = ImmutableDictionary.CreateBuilder<string, CrossReference>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Get all registered content services
            var contentServices = _serviceProvider.GetServices<IContentService>();

            // Collect cross-references from all services
            var tasks = contentServices.Select(async service =>
            {
                try
                {
                    var crossRefs = await service.GetCrossReferencesAsync();
                    return crossRefs;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get cross-references from content service {ServiceType}",
                        service.GetType().Name);
                    return ImmutableList<CrossReference>.Empty;
                }
            });

            var allCrossRefLists = await Task.WhenAll(tasks);

            // Flatten and build dictionary
            foreach (var crossRefList in allCrossRefLists)
            {
                foreach (var crossRef in crossRefList)
                {
                    if (!string.IsNullOrWhiteSpace(crossRef.Uid))
                    {
                        // If there are duplicates, the last one wins
                        // This allows content services with higher priority to override others
                        builder[crossRef.Uid] = crossRef;
                    }
                }
            }

            var result = builder.ToImmutable();
            _logger.LogDebug("Built cross-reference dictionary with {Count} entries", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build cross-reference dictionary");
            return ImmutableDictionary<string, CrossReference>.Empty;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        // AsyncLazy doesn't need explicit disposal
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}