using MyLittleContentEngine.Services;

namespace MyLittleContentEngine.Models;

/// <summary>
///  A page that will be generated during a static build.
/// </summary>
/// <param name="Url">The URL pointing to the page.</param>
/// <param name="OutputFile">The relative path of the output file.</param>
/// <param name="Metadata">Additional file properties.</param>
/// <param name="IsBinary">Marks the page as binary.</param>
public record PageToGenerate(UrlPath Url, FilePath OutputFile, Metadata? Metadata = null, bool IsBinary = false);