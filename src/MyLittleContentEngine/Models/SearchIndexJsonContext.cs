using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyLittleContentEngine.Models;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SearchIndex))]
[JsonSerializable(typeof(SearchIndexDocument))]
public partial class SearchIndexJsonContext : JsonSerializerContext
{
}