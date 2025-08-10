using System.Collections;
using System.IO.Abstractions;
using Testably.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.MarkdigExtensions;
using MyLittleContentEngine.Services.Content.CodeAnalysis.Configuration;
using MyLittleContentEngine.Services.Content.CodeAnalysis.SyntaxHighlighting;
using MyLittleContentEngine.Services.Content.CodeAnalysis.SymbolAnalysis;
using MyLittleContentEngine.Services.Content.CodeAnalysis.SolutionWorkspace;
using MyLittleContentEngine.Services.Content.TableOfContents;
using MyLittleContentEngine.Services.Generation;
using MyLittleContentEngine.Services.Infrastructure;
using MyLittleContentEngine.Services.Web;

namespace MyLittleContentEngine;

public interface IConfiguredContentEngineServiceCollection : IServiceCollection;
public class ConfiguredContentEngineServiceCollection(
    IServiceCollection configuredContentEngineServiceCollectionImplementation)
    : IConfiguredContentEngineServiceCollection
{
    public IEnumerator<ServiceDescriptor> GetEnumerator() => configuredContentEngineServiceCollectionImplementation.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)configuredContentEngineServiceCollectionImplementation).GetEnumerator();

    public void Add(ServiceDescriptor item) => configuredContentEngineServiceCollectionImplementation.Add(item);

    public void Clear() => configuredContentEngineServiceCollectionImplementation.Clear();

    public bool Contains(ServiceDescriptor item) => configuredContentEngineServiceCollectionImplementation.Contains(item);

    public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => configuredContentEngineServiceCollectionImplementation.CopyTo(array, arrayIndex);

    public bool Remove(ServiceDescriptor item) => configuredContentEngineServiceCollectionImplementation.Remove(item);

    public int Count => configuredContentEngineServiceCollectionImplementation.Count;

    public bool IsReadOnly => configuredContentEngineServiceCollectionImplementation.IsReadOnly;

    public int IndexOf(ServiceDescriptor item) => configuredContentEngineServiceCollectionImplementation.IndexOf(item);

    public void Insert(int index, ServiceDescriptor item) => configuredContentEngineServiceCollectionImplementation.Insert(index, item);

    public void RemoveAt(int index) => configuredContentEngineServiceCollectionImplementation.RemoveAt(index);

    public ServiceDescriptor this[int index]
    {
        get => configuredContentEngineServiceCollectionImplementation[index];
        set => configuredContentEngineServiceCollectionImplementation[index] = value;
    }
}

/// <summary>
/// Provides extension methods for configuring and using MyLittleContentEngine services within an ASP.NET Core application.
/// These methods facilitate static site generation with Blazor, including content management and file processing.
/// </summary>
public static class ContentEngineExtensions
{
    /// <summary>
    /// Adds a ContentFilesService to the application's service collection with custom front matter support.
    /// </summary>
    /// <typeparam name="TFrontMatter">The type used for Post metadata. Must implement IFrontMatter.</typeparam>
    /// <param name="services">The application's service collection.</param>
    /// <param name="configureOptions">Action to customize the content service options.</param>
    /// <returns>The updated service collection for method chaining.</returns>
    public static IConfiguredContentEngineServiceCollection WithMarkdownContentService<TFrontMatter>(
        this IConfiguredContentEngineServiceCollection services,
        Func<IServiceProvider, MarkdownContentOptions<TFrontMatter>>? configureOptions)
        where TFrontMatter : class, IFrontMatter, new()
    {
        if (configureOptions == null)
        {
            configureOptions = _ => new MarkdownContentOptions<TFrontMatter>
            {
                ContentPath = new FilePath("Content"),
                BasePageUrl = UrlPath.Empty,
            };
        }

        // Register options
        services.AddTransient(configureOptions);

        // Register specialized services
        services.AddTransient<TagService<TFrontMatter>>();
        services.AddTransient<ContentFilesService<TFrontMatter>>();
        services.AddTransient<MarkdownContentProcessor<TFrontMatter>>();

        // Register the primary service
        services.AddSingleton<IMarkdownContentService<TFrontMatter>, MarkdownContentService<TFrontMatter>>();
        services.AddTransient<SitemapRssService>();
        services.AddHttpClient();
        services.AddTransient<ILocalHttpClient, LocalHttpClient>();
        services.AddTransient<SearchIndexService>();

        // Register interface implementations
        services.AddSingleton<IContentService>(provider =>
            provider.GetRequiredService<IMarkdownContentService<TFrontMatter>>());
        services.AddTransient<IContentOptions>(provider =>
            provider.GetRequiredService<MarkdownContentOptions<TFrontMatter>>());

        return services;
    }

    /// <summary>
    /// Registers the core MyLittleContentEngine generation services for converting a Blazor application into static HTML, CSS, and JavaScript.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="configureOptions">Optional action to customize the static generation process.</param>
    /// <returns>The updated service collection for method chaining.</returns>
    public static IConfiguredContentEngineServiceCollection AddContentEngineService(this IServiceCollection services,
        Func<IServiceProvider, ContentEngineOptions> configureOptions)
    {
        // Register the main options for the content engine
        services.AddTransient(configureOptions);
        services.AddTransient<OutputGenerationService>();
        services.AddSingleton<IContentEngineFileWatcher, ContentEngineFileWatcher>();
        services.AddTransient<ITableOfContentService, TableOfContentService>();
        services.AddTransient<MarkdownParserService>();
        services.AddTransient<RoutesHelperService>();
        services.AddSingleton<IFileSystem>(new RealFileSystem());
        services.AddTransient<FileSystemUtilities>();
        services.AddSingleton<IXrefResolver, XrefResolver>();
        services.AddSyntaxHighlightingService();

        // Register the Razor page content service
        services.AddSingleton<RazorPageContentService>();
        services.AddSingleton<IContentService>(provider => provider.GetRequiredService<RazorPageContentService>());
        services.AddOutputOptions(Environment.GetCommandLineArgs());

        return new ConfiguredContentEngineServiceCollection(services);
    }

    /// <summary>
    /// Adds syntax highlighting services to the application's service collection.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The updated service collection for method chaining.</returns>
    /// <remarks>
    /// This method always registers the SyntaxHighlightingService with HighlightingOptions.
    /// Use AddConnectedRoslynSolution to add solution workspace services for advanced features.
    /// </remarks>
    private static IServiceCollection AddSyntaxHighlightingService(this IServiceCollection services)
    {
        services.AddTransient<ISyntaxHighlightingService, SyntaxHighlightingService>();

        return services;
    }

    /// <summary>
    /// Adds connected Roslyn solution services for advanced code analysis and symbol extraction.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="configureOptions">Action to configure the connected solution options.</param>
    /// <returns>The updated service collection for method chaining.</returns>
    /// <remarks>
    /// This method registers solution workspBace and symbol extraction services.
    /// Call AddRoslynService first to register basic syntax highlighting.
    /// This method overrides the CodeAnalysisOptions from AddRoslynService.
    /// </remarks>
    public static IConfiguredContentEngineServiceCollection WithConnectedRoslynSolution(this IConfiguredContentEngineServiceCollection services,
        Func<IServiceProvider, CodeAnalysisOptions> configureOptions)
    {
        // Remove the existing CodeAnalysisOptions registration from AddRoslynService
        var existingDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(CodeAnalysisOptions));
        if (existingDescriptor != null)
        {
            services.Remove(existingDescriptor);
        }
        
        // Register the new code analysis options
        services.AddSingleton(configureOptions);
        
        // Register connected solution services
        services.AddSingleton<ISolutionWorkspaceService, SolutionWorkspaceService>();
        services.AddSingleton<ISymbolExtractionService, SymbolExtractionService>();

        return services;
    }

    /// <summary>
    /// Adds an ApiReferenceContentService to the application's service collection for generating API documentation.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="func">Configuration function for API reference options.</param>
    /// <returns>The updated service collection for method chaining.</returns>
    /// <remarks>
    /// This method registers the ApiReferenceContentService as both a singleton service and as an IContentService.
    /// The ApiReferenceContentService requires solution workspace services, so this method automatically
    /// calls AddConnectedRoslynSolution if not already registered.
    /// Call AddRoslynService first to register basic syntax highlighting.
    /// </remarks>
    public static IConfiguredContentEngineServiceCollection WithApiReferenceContentService(this IConfiguredContentEngineServiceCollection services,
        Func<IServiceProvider, ApiReferenceContentOptions> func)
    {
        services.AddTransient(func);
        
        // Ensure connected solution services are registered
        // Check if ISolutionWorkspaceService is already registered
        var solutionServiceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISolutionWorkspaceService));
        if (solutionServiceDescriptor == null)
        {
            // Need to register connected solution services
            // Build a temporary service provider to get the API reference options
            var tempServiceProvider = services.BuildServiceProvider();
            var apiOptions = tempServiceProvider.GetRequiredService<ApiReferenceContentOptions>();
            
            if (apiOptions.SolutionPath == null || apiOptions.SolutionPath.Value.IsEmpty)
            {
                throw new InvalidOperationException(
                    "ApiReferenceContentService requires a solution path. Set SolutionPath in ApiReferenceContentOptions.");
            }
            
            services.WithConnectedRoslynSolution(_ => new CodeAnalysisOptions
            {
                SolutionPath = apiOptions.SolutionPath.Value.Value
            });
        }
        
        // Register the API reference content service
        services.AddSingleton<ApiReferenceContentService>();

        // Register as IContentService (this allows multiple IContentService implementations)
        services.AddSingleton<IContentService>(provider => provider.GetRequiredService<ApiReferenceContentService>());

        return services;
    }

    private static void MapContentEngineStaticAssets(this WebApplication app)
    {
        var optionList = app.Services.GetServices<IContentOptions>().ToList();
        var engineOptions = app.Services.GetRequiredService<ContentEngineOptions>();
        if (optionList.Count == 0)
        {
            throw new InvalidOperationException(
                "No IContentOptions registered. Call AddContentEngineStaticContentService<TFrontMatter> first.");
        }

        var fileSystem = app.Services.GetRequiredService<IFileSystem>();
        var currentDirectory = fileSystem.Directory.GetCurrentDirectory();

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider =
                new PhysicalFileProvider(fileSystem.Path.Combine(currentDirectory, engineOptions.ContentRootPath.Value)),
            RequestPath = "",
            ServeUnknownFileTypes = false,
        });

        foreach (var option in optionList)
        {
            var combine = fileSystem.Path.Combine(currentDirectory, option.ContentPath.Value);
            var optionPageUrl = option.BasePageUrl.EnsureLeadingSlash().RemoveTrailingSlash();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(combine),
                RequestPath = optionPageUrl.Value,
                ServeUnknownFileTypes = true,
            });
        }

        app.UseBaseUrlRewriting();

        if (app.Services.GetService<SitemapRssService>() != null)
        {
            app.MapContentEngineSitemapRss();
        }
    }

    /// <summary>
    /// Adds sitemap.xml and RSS feed endpoints to the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    private static void MapContentEngineSitemapRss(this WebApplication app)
    {
        // Map the sitemap.xml endpoint
        app.MapGet("/sitemap.xml", async (SitemapRssService service) =>
        {
            var sitemap = await service.GenerateSitemap();
            // Set the content type and return the sitemap
            return Results.Content(sitemap, "application/xml");
        });

        // Map the rss.xml endpoint
        app.MapGet("/rss.xml", async (SitemapRssService service) =>
        {
            var rss = await service.GenerateRssFeed();
            return Results.Content(rss, "text/xml");
        });

        // Map the search index endpoint
        app.MapGet("/search-index.json", async (SearchIndexService service, HttpContext context) =>
        {
            var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
            var searchIndex = await service.GenerateSearchIndexAsync(baseUrl);
            return Results.Content(searchIndex, "application/json");
        });
    }

    /// <summary>
    /// Executes the static site generation process, converting the Blazor application to static files.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>A Task representing the asynchronous generation operation.</returns>
    /// <remarks>
    /// <para>This method performs the complete static generation process:</para>
    /// <list type="number">
    ///     <item><description>Loads and parses all content from registered content services</description></item>
    ///     <item><description>Copies static web assets (from wwwroot and other static sources) to the output</description></item>
    ///     <item><description>Renders and saves all application routes as static HTML</description></item>
    /// </list>
    /// <para>Call this method after configuring all required MyLittleContentEngine services and during application startup.
    /// The generation uses the first URL from the application's configured URLs list as the base address.</para>
    /// </remarks>
    private static async Task UseContentEngineStaticGenerator(this WebApplication app)
    {
        var contentEngine = app.Services.GetRequiredService<OutputGenerationService>();
        await contentEngine.GenerateStaticPages(app.Urls.First());
    }

    /// <summary>
    /// Conditionally runs the application or generates a static build based on command-line arguments.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="args">Command-line arguments passed to the application.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public static async Task RunOrBuildContent(this WebApplication app, string[] args)
    {
        // Enable static web assets in all environments (suppresses warnings about RCL assets)
        StaticWebAssetsLoader.UseStaticWebAssets(app.Environment, app.Configuration);

        app.MapContentEngineStaticAssets();

        if (args.Length > 0 && args[0].Equals("build", StringComparison.OrdinalIgnoreCase))
        {
            await app.StartAsync();
            await app.UseContentEngineStaticGenerator();
            await app.StopAsync();
        }
        else
        {
            await app.RunAsync();
        }
    }
}