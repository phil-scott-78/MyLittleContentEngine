using System.Collections;
using System.Collections.Immutable;
using System.IO.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.CodeAnalysis.Configuration;
using MyLittleContentEngine.Services.Content.CodeAnalysis.SolutionWorkspace;
using MyLittleContentEngine.Services.Content.CodeAnalysis.SymbolAnalysis;
using MyLittleContentEngine.Services.Content.CodeAnalysis.SyntaxHighlighting;
using MyLittleContentEngine.Services.Content.MarkdigExtensions;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;
using MyLittleContentEngine.Services.Content.TableOfContents;
using MyLittleContentEngine.Services.Generation;
using MyLittleContentEngine.Services.Infrastructure;
using MyLittleContentEngine.Services.Web;
using Testably.Abstractions;

namespace MyLittleContentEngine;

/// <summary>
/// Marker interface for a service collection that has been configured with content engine services.
/// </summary>
public interface IConfiguredContentEngineServiceCollection : IServiceCollection;

/// <summary>
/// Wraps an <see cref="IServiceCollection"/> to indicate content engine services have been registered.
/// </summary>
public class ConfiguredContentEngineServiceCollection(
    IServiceCollection configuredContentEngineServiceCollectionImplementation)
    : IConfiguredContentEngineServiceCollection
{
    /// <inheritdoc />
    public IEnumerator<ServiceDescriptor> GetEnumerator() => configuredContentEngineServiceCollectionImplementation.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)configuredContentEngineServiceCollectionImplementation).GetEnumerator();

    /// <inheritdoc />
    public void Add(ServiceDescriptor item) => configuredContentEngineServiceCollectionImplementation.Add(item);

    /// <inheritdoc />
    public void Clear() => configuredContentEngineServiceCollectionImplementation.Clear();

    /// <inheritdoc />
    public bool Contains(ServiceDescriptor item) => configuredContentEngineServiceCollectionImplementation.Contains(item);

    /// <inheritdoc />
    public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => configuredContentEngineServiceCollectionImplementation.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public bool Remove(ServiceDescriptor item) => configuredContentEngineServiceCollectionImplementation.Remove(item);

    /// <inheritdoc />
    public int Count => configuredContentEngineServiceCollectionImplementation.Count;

    /// <inheritdoc />
    public bool IsReadOnly => configuredContentEngineServiceCollectionImplementation.IsReadOnly;

    /// <inheritdoc />
    public int IndexOf(ServiceDescriptor item) => configuredContentEngineServiceCollectionImplementation.IndexOf(item);

    /// <inheritdoc />
    public void Insert(int index, ServiceDescriptor item) => configuredContentEngineServiceCollectionImplementation.Insert(index, item);

    /// <inheritdoc />
    public void RemoveAt(int index) => configuredContentEngineServiceCollectionImplementation.RemoveAt(index);

    /// <inheritdoc />
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
        services.AddFileWatched<MarkdownContentProcessor<TFrontMatter>>();

        // Register the primary service
        services.AddFileWatched<IMarkdownContentService<TFrontMatter>, MarkdownContentService<TFrontMatter>>();
        services.AddTransient<SitemapRssService>();
        services.AddHttpClient();
        services.AddTransient<ILocalHttpClient, LocalHttpClient>();
        services.AddTransient<SearchIndexService>();

        // Register interface implementations
        services.AddTransient<IContentService>(provider =>
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
        services.AddTransient<LinkVerificationService>();
        services.AddSingleton<IContentEngineFileWatcher, ContentEngineFileWatcher>();

        // Register TextMate language registry and highlighter
        services.AddTransient(provider =>
        {
            var options = provider.GetRequiredService<ContentEngineOptions>();
            return new TextMateLanguageRegistry(options.ConfigureTextMate);
        });
        services.AddTransient<ITextMateHighlighter, TextMateHighlighter>();

        // Register code highlighter service
        services.AddTransient<ICodeHighlighter, CodeHighlighterService>();

        services.AddTransient<MarkdownParserService>();
        services.AddTransient<RoutesHelperService>();
        services.AddTransient<IFileSystem, RealFileSystem>();
        services.AddTransient<FilePathOperations>();
        services.AddTransient<FileSystemUtilities>();
        services.AddSyntaxHighlightingService();

        // Register OutputOptions first so it's available to all dependent services
        services.AddOutputOptions(Environment.GetCommandLineArgs());

        // Register the Razor page content service with file-watch invalidation
        var configuredServices = new ConfiguredContentEngineServiceCollection(services);
        configuredServices.AddFileWatched<RazorPageContentService>();
        configuredServices.AddFileWatched<RedirectContentService>();
        configuredServices.AddTransient<IContentService>(provider => provider.GetRequiredService<RazorPageContentService>());
        configuredServices.AddTransient<IContentService>(provider => provider.GetRequiredService<RedirectContentService>());

        // Register XrefResolver with file-watch invalidation
        configuredServices.AddFileWatched<IXrefResolver, XrefResolver>();

        configuredServices.AddFileWatched<ITableOfContentService, TableOfContentService>();

        // Register URL rewriting as a response processor
        services.AddSingleton<IResponseProcessor, BaseUrlRewritingProcessor>();

        return configuredServices;
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
    /// Adds a service with file-watch invalidated lifetime. The service instance will be cached
    /// until file changes are detected by the content engine, providing optimal performance
    /// while ensuring fresh instances when content changes.
    /// </summary>
    /// <typeparam name="T">The service type to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection for method chaining.</returns>
    /// <remarks>
    /// This method directly registers the service with file-watch invalidation capabilities.
    /// Requires IContentEngineFileWatcher to be registered first.
    /// </remarks>
    public static IConfiguredContentEngineServiceCollection AddFileWatched<T>(
        this IConfiguredContentEngineServiceCollection services) where T : class
    {
        return services.AddFileWatched<T, T>();
    }

    /// <summary>
    /// Adds a service with file-watch invalidated lifetime, with separate service and implementation types.
    /// The service instance will be cached until file changes are detected by the content engine.
    /// </summary>
    /// <typeparam name="TService">The service type to register.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection for method chaining.</returns>
    /// <remarks>
    /// This method directly registers the service interface with file-watch invalidation capabilities.
    /// Requires IContentEngineFileWatcher to be registered first.
    /// </remarks>
    public static IConfiguredContentEngineServiceCollection AddFileWatched<TService, TImplementation>(
        this IConfiguredContentEngineServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        // Register the factory for the implementation type
        services.AddSingleton<FileWatchDependencyFactory<TImplementation>>(provider =>
        {
            var fileWatcher = provider.GetRequiredService<IContentEngineFileWatcher>();
            var logger = provider.GetRequiredService<ILogger<FileWatchDependencyFactory<TImplementation>>>();

            return new FileWatchDependencyFactory<TImplementation>(fileWatcher, ServiceFactory, provider, logger);

            // Create a factory function that creates instances using ActivatorUtilities
            TImplementation ServiceFactory(IServiceProvider serviceProvider) => ActivatorUtilities.CreateInstance<TImplementation>(serviceProvider);
        });

        // Register the service interface as singleton, retrieved through the factory
        services.AddTransient<TService>(provider =>
            provider.GetRequiredService<FileWatchDependencyFactory<TImplementation>>().GetInstance());

        return services;
    }

    /// <summary>
    /// Registers markdown content services with locale-aware coordination.
    /// Always registers <see cref="ILocalizedContentService{TFrontMatter}"/> which acts as
    /// a pass-through for single-locale sites and provides full i18n support when
    /// <see cref="LocalizationOptions"/> is configured on <see cref="ContentEngineOptions"/>.
    /// </summary>
    /// <typeparam name="TFrontMatter">The front matter type.</typeparam>
    /// <param name="services">The configured service collection.</param>
    /// <param name="configureOptions">Factory for the default locale's content options.</param>
    /// <returns>The updated service collection for method chaining.</returns>
    public static IConfiguredContentEngineServiceCollection WithLocalizedMarkdownContent<TFrontMatter>(
        this IConfiguredContentEngineServiceCollection services,
        Func<IServiceProvider, MarkdownContentOptions<TFrontMatter>> configureOptions)
        where TFrontMatter : class, IFrontMatter, new()
    {
        // Wrap the options factory to inject locale metadata for the default locale
        services.WithMarkdownContentService(sp =>
        {
            var baseOptions = configureOptions(sp);
            var engineOptions = sp.GetRequiredService<ContentEngineOptions>();
            var localization = engineOptions.Localization;
            var defaultLocale = localization?.DefaultLocale ?? "en";

            // Exclude non-default locale subfolders from the default locale's content discovery
            var excludedSubfolders = localization?.Locales.Keys
                .Where(l => !string.Equals(l, defaultLocale, StringComparison.OrdinalIgnoreCase))
                .ToImmutableList() ?? [];

            return baseOptions with { Locale = defaultLocale, ExcludedSubfolders = excludedSubfolders };
        });

        // Register the localized content coordination service
        services.AddSingleton<ILocalizedContentService<TFrontMatter>>(sp =>
        {
            var engineOptions = sp.GetRequiredService<ContentEngineOptions>();
            var baseOptions = configureOptions(sp);
            var defaultService = sp.GetRequiredService<IMarkdownContentService<TFrontMatter>>();

            // Use configured localization, or synthesize a single-locale default
            var localization = engineOptions.Localization ?? new LocalizationOptions
            {
                DefaultLocale = "en",
                Locales = ImmutableDictionary<string, LocaleInfo>.Empty
                    .Add("en", new LocaleInfo("English"))
            };

            var localeServices = new Dictionary<string, IMarkdownContentService<TFrontMatter>>(StringComparer.OrdinalIgnoreCase)
            {
                [localization.DefaultLocale] = defaultService
            };

            // Create per-locale content services for non-default locales
            foreach (var locale in localization.Locales.Keys)
            {
                if (string.Equals(locale, localization.DefaultLocale, StringComparison.OrdinalIgnoreCase))
                    continue;

                var localeOptions = baseOptions with
                {
                    ContentPath = new FilePath(Path.Combine(baseOptions.ContentPath.Value, locale)),
                    BasePageUrl = new UrlPath(locale),
                    Locale = locale,
                };

                var service = CreateLocaleContentService(sp, localeOptions);
                localeServices[locale] = service;
            }

            return new LocalizedContentService<TFrontMatter>(localeServices, localization);
        });

        // Register non-default locale services as IContentService for TOC and generation
        services.AddTransient<IContentService>(sp =>
        {
            var localizedService = sp.GetRequiredService<ILocalizedContentService<TFrontMatter>>();
            var engineOptions = sp.GetRequiredService<ContentEngineOptions>();
            var localization = engineOptions.Localization;

            // Single-locale: nothing extra to register
            if (localization == null || localization.Locales.Count <= 1)
                return new NoOpContentService();

            return new LocaleCompositeContentService<TFrontMatter>(localizedService, localization);
        });

        return services;
    }

    /// <summary>
    /// Creates a complete MarkdownContentService for a specific locale by manually
    /// constructing the internal service chain with locale-specific options.
    /// </summary>
    private static MarkdownContentService<TFrontMatter> CreateLocaleContentService<TFrontMatter>(
        IServiceProvider sp,
        MarkdownContentOptions<TFrontMatter> localeOptions)
        where TFrontMatter : class, IFrontMatter, new()
    {
        var fileSystemUtilities = sp.GetRequiredService<FileSystemUtilities>();
        var filePathOps = sp.GetRequiredService<FilePathOperations>();
        var markdownParser = sp.GetRequiredService<MarkdownParserService>();
        var fileSystem = sp.GetRequiredService<IFileSystem>();
        var fileWatcher = sp.GetRequiredService<IContentEngineFileWatcher>();

        var tagService = new TagService<TFrontMatter>(
            localeOptions,
            sp.GetRequiredService<ILogger<TagService<TFrontMatter>>>());

        var contentFilesService = new ContentFilesService<TFrontMatter>(
            localeOptions, fileSystemUtilities, filePathOps,
            sp.GetRequiredService<ILogger<ContentFilesService<TFrontMatter>>>());

        var processor = new MarkdownContentProcessor<TFrontMatter>(
            localeOptions, markdownParser, tagService, contentFilesService,
            fileSystem, fileWatcher,
            sp.GetRequiredService<ILogger<MarkdownContentProcessor<TFrontMatter>>>());

        return new MarkdownContentService<TFrontMatter>(
            localeOptions, tagService, contentFilesService, markdownParser, processor);
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
        services.AddFileWatched<ISymbolExtractionService, SymbolExtractionService>();

        return services;
    }

    /// <summary>
    /// Adds a <see cref="Services.Content.FlatFileRedirectContentService"/> that generates
    /// <c>filename.html</c> redirect files for pages using folder-based output.
    /// This provides backward compatibility for sites that migrated from flat-file to
    /// folder-based URL output (e.g., <c>about.html</c> redirects to <c>about/</c>).
    /// </summary>
    /// <param name="services">The configured service collection.</param>
    /// <returns>The updated service collection for method chaining.</returns>
    public static IConfiguredContentEngineServiceCollection WithFlatFileRedirects(
        this IConfiguredContentEngineServiceCollection services)
    {
        services.AddTransient<IContentService, FlatFileRedirectContentService>();
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

        // Register the API reference content service with file-watch invalidation
        services.AddFileWatched<ApiReferenceContentService>();

        // Register as IContentService (this allows multiple IContentService implementations)
        services.AddTransient<IContentService>(provider => provider.GetRequiredService<ApiReferenceContentService>());

        return services;
    }

    private static void MapContentEngineStaticAssets(this WebApplication app)
    {
        app.UseContentEngineRedirects();

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

        app.UseResponseProcessing();

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