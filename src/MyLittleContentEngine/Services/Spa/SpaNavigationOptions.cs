namespace MyLittleContentEngine.Services.Spa;

/// <summary>
/// Configuration options for SPA navigation.
/// </summary>
public class SpaNavigationOptions
{
    /// <summary>
    /// Gets or sets the URL path prefix for page data endpoints.
    /// Defaults to <c>"/_spa-data"</c>.
    /// </summary>
    public string DataPath { get; set; } = "/_spa-data";
}
