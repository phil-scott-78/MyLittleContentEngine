using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MyLittleContentEngine.Services.Content.CodeAnalysis.SymbolAnalysis;

/// <summary>
/// Service responsible for extracting and analyzing symbols from a Roslyn solution
/// </summary>
public interface ISymbolExtractionService
{
    /// <summary>
    /// Extracts all symbols from the provided solution
    /// </summary>
    /// <param name="solution">The solution to analyze</param>
    /// <returns>Dictionary mapping XML documentation IDs to symbol information</returns>
    Task<IReadOnlyDictionary<string, SymbolInfo>> ExtractSymbolsAsync(Solution solution);

    /// <summary>
    /// Finds a specific symbol by its XML documentation ID
    /// </summary>
    /// <param name="xmlDocId">The XML documentation ID (e.g., "M:MyNamespace.MyClass.MyMethod")</param>
    /// <returns>Symbol information if found, null otherwise</returns>
    Task<SymbolInfo?> FindSymbolAsync(string xmlDocId);

    /// <summary>
    /// Extracts a code fragment for the specified symbol
    /// </summary>
    /// <param name="xmlDocId">The XML documentation ID</param>
    /// <param name="bodyOnly">If true, only extracts the method/property body</param>
    /// <returns>The extracted code fragment</returns>
    Task<string> ExtractCodeFragmentAsync(string xmlDocId, bool bodyOnly = false);

    /// <summary>
    /// Invalidates cached symbol for a specific file
    /// </summary>
    /// <param name="filePath">The file path that changed</param>
    void InvalidateFile(string filePath);

    /// <summary>
    /// Clears all cached symbols
    /// </summary>
    void ClearCache();
}

/// <summary>
/// Information about a symbol extracted from code
/// </summary>
public record SymbolInfo
{
    /// <summary>
    /// The Roslyn symbol
    /// </summary>
    public required ISymbol Symbol { get; init; }

    /// <summary>
    /// The document containing the symbol
    /// </summary>
    public required Document Document { get; init; }

    /// <summary>
    /// The syntax node for the symbol
    /// </summary>
    public required SyntaxNode? SyntaxNode { get; init; }

    /// <summary>
    /// The full source text of the document
    /// </summary>
    public required SourceText SourceText { get; init; }

    /// <summary>
    /// The text span of the symbol in the source
    /// </summary>
    public required TextSpan TextSpan { get; init; }

    /// <summary>
    /// XML documentation comment for the symbol
    /// </summary>
    public string? XmlDocumentation { get; init; }

    /// <summary>
    /// The project containing this symbol
    /// </summary>
    public required Project Project { get; init; }
}