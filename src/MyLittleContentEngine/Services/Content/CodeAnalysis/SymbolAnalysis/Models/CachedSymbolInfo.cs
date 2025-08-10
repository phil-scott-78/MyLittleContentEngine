using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MyLittleContentEngine.Services.Content.CodeAnalysis.SymbolAnalysis.Models;

/// <summary>
/// Cached information about a symbol including its assembly
/// </summary>
internal record CachedSymbolInfo(
    Document Document,
    TextSpan TextSpan,
    SourceText SourceText,
    ISymbol Symbol,
    Lazy<Task<System.Reflection.Assembly>> LazyAssembly)
{
    /// <summary>
    /// Creates a CachedSymbolInfo from a SymbolInfo
    /// </summary>
    public static CachedSymbolInfo FromSymbolInfo(SymbolInfo symbolInfo, Lazy<Task<System.Reflection.Assembly>> lazyAssembly)
    {
        return new CachedSymbolInfo(
            symbolInfo.Document,
            symbolInfo.TextSpan,
            symbolInfo.SourceText,
            symbolInfo.Symbol,
            lazyAssembly);
    }
}