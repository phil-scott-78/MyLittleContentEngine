using System.IO.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.MarkdigExtensions;
using MyLittleContentEngine.Services.Content.Roslyn;
using MyLittleContentEngine.Services.Content.TableOfContents;
using MyLittleContentEngine.Services.Generation;
using MyLittleContentEngine.Services.Infrastructure;
using MyLittleContentEngine.Services.Web;

namespace MyLittleContentEngine;

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
    public static IServiceCollection AddContentEngineStaticContentService<TFrontMatter>(
        this IServiceCollection services,
        Func<IServiceProvider, ContentEngineContentOptions<TFrontMatter>>? configureOptions)
        where TFrontMatter : class, IFrontMatter, new()
    {
        if (configureOptions == null)
        {
            configureOptions = _ => new ContentEngineContentOptions<TFrontMatter>
            {
                ContentPath = "Content",
                BasePageUrl = string.Empty,
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
        services.AddTransient<SearchIndexService>();

        // Register interface implementations
        services.AddSingleton<IContentService>(provider =>
            provider.GetRequiredService<IMarkdownContentService<TFrontMatter>>());
        services.AddTransient<IContentOptions>(provider =>
            provider.GetRequiredService<ContentEngineContentOptions<TFrontMatter>>());

        return services;
    }

    /// <summary>
    /// Registers the core MyLittleContentEngine generation services for converting a Blazor application into static HTML, CSS, and JavaScript.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="configureOptions">Optional action to customize the static generation process.</param>
    /// <returns>The updated service collection for method chaining.</returns>
    public static IServiceCollection AddContentEngineService(this IServiceCollection services,
        Func<IServiceProvider, ContentEngineOptions> configureOptions)
    {
        // Register the main options for the content engine
        services.AddTransient(configureOptions);
        services.AddTransient<OutputGenerationService>();
        services.AddSingleton<IContentEngineFileWatcher, ContentEngineFileWatcher>();
        services.AddTransient<ITableOfContentService, TableOfContentService>();
        services.AddTransient<MarkdownParserService>();
        services.AddTransient<RoutesHelperService>();
        services.AddSingleton<IFileSystem>(new FileSystem());
        services.AddTransient<FileSystemUtilities>();
        services.AddTransient<LinkService>();

        // Register the Razor page content service
        services.AddTransient<RazorPageContentService>();
        services.AddTransient<IContentService>(provider => provider.GetRequiredService<RazorPageContentService>());

        return services;
    }

    /// <summary>
    /// Adds a RoslynHighlighterService to the application's service collection with support for Roslyn highlighter configuration.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="configureOptions">Action to configure the Roslyn highlighter options.</param>
    /// <returns>The updated service collection for method chaining.</returns>
    /// <remarks>
    /// This method registers the RoslynHighlighterService and its associated options as singleton services.
    /// The RoslynHighlighterService facilitates syntax highlighting
    /// using Roslyn configuration provided in the specified options.
    /// </remarks>
    public static IServiceCollection AddRoslynService(this IServiceCollection services,
        Func<IServiceProvider, RoslynHighlighterOptions>? configureOptions = null)
    {
        if (configureOptions == null)
        {
            services.AddSingleton(new RoslynHighlighterOptions());
        }
        else
        {
            services.AddTransient(configureOptions);

            var options = configureOptions.Invoke(services.BuildServiceProvider());
            if (options.ConnectedSolution != null)
            {
                services.AddTransient<IRoslynHighlighterService, RoslynHighlighterService>();
                services.AddTransient<IRoslynExampleCoordinator, RoslynExampleCoordinator>();
                services.AddTransient<CodeExecutionService>();
                services.AddSingleton<AssemblyLoaderService>();
            }
        }

        return services;
    }

    /// <summary>
    /// Adds an ApiReferenceContentService to the application's service collection for generating API documentation.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="func"></param>
    /// <returns>The updated service collection for method chaining.</returns>
    /// <remarks>
    /// This method registers the ApiReferenceContentService as both a singleton service and as an IContentService.
    /// The ApiReferenceContentService requires RoslynExampleCoordinator to be registered first via AddRoslynService.
    /// </remarks>
    public static IServiceCollection AddApiReferenceContentService(this IServiceCollection services,
        Func<IServiceProvider, ApiReferenceContentOptions> func)
    {
        services.AddTransient(func);
        // Register the API reference content service
        services.AddTransient<ApiReferenceContentService>();

        // Register as IContentService (this allows multiple IContentService implementations)
        services.AddTransient<IContentService>(provider => provider.GetRequiredService<ApiReferenceContentService>());

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
                new PhysicalFileProvider(fileSystem.Path.Combine(currentDirectory, engineOptions.ContentRootPath)),
            RequestPath = "",
            ServeUnknownFileTypes = false,
        });

        foreach (var option in optionList)
        {
            var combine = fileSystem.Path.Combine(currentDirectory, option.ContentPath);
            string optionPageUrl;
            if (string.IsNullOrWhiteSpace(option.BasePageUrl))
                optionPageUrl = string.Empty;
            else if (option.BasePageUrl.StartsWith('/'))
                optionPageUrl = option.BasePageUrl;
            else
                optionPageUrl = '/' + option.BasePageUrl;

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(combine),
                RequestPath = optionPageUrl,
                ServeUnknownFileTypes = true,
            });
        }

        app.UseWordBreaking();
        app.MapContentEngineSitemapRss();
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