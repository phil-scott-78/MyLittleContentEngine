using System.Collections.Immutable;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Generation;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// A Content Service responsible for parsing and handling content.
/// </summary>
public interface IContentService
{
    /// <summary>
    /// Gets the search priority for this content service. Higher values appear first in search results.
    /// Default priority is 1. Recommended values: 1 (low), 5 (medium), 10 (high).
    /// </summary>
    int SearchPriority { get; }
    /// <summary>
    /// Gets the collection of pages that should be generated for this content.
    /// </summary>
    /// <returns>
    /// An ImmutableList of PageToGenerate objects, each representing a page that
    /// should be processed by the <see cref="OutputGenerationService"/>.
    /// </returns>
    Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync();

    /// <summary>
    /// Gets the collection of pages that should appear in the Table of Contents.
    /// This is typically a subset of the pages returned by GetPagesToGenerateAsync().
    /// For example, API documentation might generate thousands of pages but only
    /// want to show the root "/api/" entry in the TOC.
    /// </summary>
    /// <returns>
    /// An ImmutableList of PageToGenerate objects that should be included in the
    /// Table of Contents navigation.
    /// </returns>
    Task<ImmutableList<PageToGenerate>> GetTocEntriesToGenerateAsync();

    /// <summary>
    /// Gets the collection of content that should be copied to the output directory.
    /// </summary>
    /// <returns>
    /// An ImmutableList of ContentToCopy objects, each representing a file or directory
    /// that should be copied by the <see cref="OutputGenerationService"/>.
    /// </returns>
    Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync();

    /// <summary>
    /// Gets the cross-references used in the content system, such as those found in the Table of Contents (ToC) or xref links.
    /// </summary>
    /// <returns>
    /// An ImmutableList of CrossReference objects, each representing a cross-reference in the content system.
    /// </returns>
    public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync();
}