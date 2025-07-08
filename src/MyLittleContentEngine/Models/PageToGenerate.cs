namespace MyLittleContentEngine.Models;

/// <summary>
///  A page that will be generated during a static build.
/// </summary>
/// <param name="Url">The URL pointing to the page.</param>
/// <param name="OutputFile">The relative path of the output file.</param>
/// <param name="Metadata">Additional file properties.</param>
public record PageToGenerate(string Url, string OutputFile, Metadata? Metadata = null);