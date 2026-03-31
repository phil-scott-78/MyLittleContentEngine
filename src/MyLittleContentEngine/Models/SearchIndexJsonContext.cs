using System.Text.Json.Serialization;

namespace MyLittleContentEngine.Models;

/// <summary>
/// Source-generated JSON serializer context for <see cref="SearchIndex"/> types.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SearchIndex))]
[JsonSerializable(typeof(SearchIndexDocument))]
public partial class SearchIndexJsonContext : JsonSerializerContext
{
}