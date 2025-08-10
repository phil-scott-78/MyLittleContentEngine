using MyLittleContentEngine.Services;

namespace MyLittleContentEngine;

/// <summary>
/// Configuration options for output generation in MyLittleContentEngine.
/// </summary>
/// <remarks>
/// <para>
/// This class provides configuration for output generation, including base URL handling and output folder path,
/// which are used for generating static sites and handling deployments.
/// </para>
/// </remarks>
public record OutputOptions
{
    /// <summary>
    /// Gets or sets the base URL for the published site.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This value is used for generating absolute URLs while taking into account the deployment might be in a child path or subdirectory,
    /// such as when deploying to GitHub Pages or a subfolder on a web server.
    /// </para>
    /// <para>
    /// Example format: "/MyLittleContentEngine" (automatically normalized, no need to worry about slashes)
    /// </para>
    /// <para>
    /// Generally, this should be an empty UrlPath for dev and set by an environment variable or configuration setting for production deployments.
    /// </para>
    /// </remarks>
    public UrlPath BaseUrl { get; init; } = UrlPath.Empty;

    /// <summary>
    /// Gets or sets the output directory path for generated static files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This path is relative to the project root directory. All generated static content
    /// will be placed in this directory during the build process.
    /// </para>
    /// <para>
    /// The default value is "output".
    /// </para>
    /// </remarks>
    public FilePath OutputFolderPath { get; init; } = new FilePath("output");
}