using Microsoft.Extensions.DependencyInjection;
using MyLittleContentEngine.Services;

namespace MyLittleContentEngine;

/// <summary>
/// Extension methods for configuring OutputOptions.
/// </summary>
public static class OutputOptionsExtensions
{
    /// <summary>
    /// Adds OutputOptions to the service collection with command line argument parsing.
    /// </summary>
    /// <param name="services">The service collection to add the options to.</param>
    /// <param name="args">Command line arguments array.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method checks for command line arguments in the format: "build [baseUrl] [outputFolderPath]"
    /// If the first argument is "build", the second argument is used as the BaseUrl and the third as OutputFolderPath.
    /// If no matching arguments are found, it falls back to the "BaseHref" environment variable for BaseUrl.
    /// OutputFolderPath defaults to "output" if not specified.
    /// </para>
    /// </remarks>
    internal static IServiceCollection AddOutputOptions(this IServiceCollection services, string[] args)
    {
        var (baseUrl, outputFolderPath) = GetOutputOptionsFromArgs(args.Skip(1).ToArray());
        return services.AddSingleton<OutputOptions>(_ => new OutputOptions { BaseUrl = baseUrl, OutputFolderPath = outputFolderPath });
    }

    /// <summary>
    /// Extracts the output options from command line arguments or environment variables.
    /// </summary>
    /// <param name="args">Command line arguments array.</param>
    /// <returns>A tuple containing the base URL and output folder path.</returns>
    private static (UrlPath baseUrl, FilePath outputFolderPath) GetOutputOptionsFromArgs(string[] args)
    {
        UrlPath baseUrl = UrlPath.Empty;
        FilePath outputFolderPath = new FilePath("output");

        // Check if the first argument is "build"
        if (args.Length >= 1 && args[0].Equals("build", StringComparison.OrdinalIgnoreCase))
        {
            // The second argument is BaseUrl
            if (args.Length >= 2)
            {
                // UrlPath automatically handles normalization
                baseUrl = new UrlPath(args[1]).EnsureLeadingSlash().EnsureTrailingSlash();
            }

            // The third argument is OutputFolderPath
            if (args.Length >= 3)
            {
                outputFolderPath = new FilePath(args[2]);
            }
        }

        // Fall back to environment variable for BaseUrl only (no env var for OutputFolderPath)
        if (baseUrl.IsEmpty)
        {
            var envValue = Environment.GetEnvironmentVariable("BaseHref");
            if (!string.IsNullOrWhiteSpace(envValue))
            {
                baseUrl = new UrlPath(envValue);
            }
        }

        return (baseUrl, outputFolderPath);
    }
}