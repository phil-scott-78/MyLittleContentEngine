using System.Text.Json.Serialization;

namespace MyLittleContentEngine.Services.Spa;

/// <summary>
/// JSON envelope returned per-page for client-side SPA navigation.
/// Contains the page title, description, and named HTML island contents.
/// </summary>
public record SpaPageEnvelope
{
    /// <summary>Gets the page title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the page description, if set.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the named island contents. Each key maps to a <c>data-spa-island</c> element
    /// in the layout; the value is the HTML to inject.
    /// </summary>
    public required Dictionary<string, string> Islands { get; init; }
}

[JsonSerializable(typeof(SpaPageEnvelope))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class SpaPageEnvelopeJsonContext : JsonSerializerContext;
