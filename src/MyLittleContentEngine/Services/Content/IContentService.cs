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
    /// Gets the collection of pages that should be generated for this content.
    /// </summary>
    /// <returns>
    /// An ImmutableList of PageToGenerate objects, each representing a page that
    /// should be processed by the <see cref="OutputGenerationService"/>.
    /// </returns>
    Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync();

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