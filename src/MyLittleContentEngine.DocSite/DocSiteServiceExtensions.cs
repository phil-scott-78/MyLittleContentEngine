using Mdazor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MonorailCss;
using MyLittleContentEngine.DocSite.Components;
using MyLittleContentEngine.MonorailCss;
using MyLittleContentEngine.Services.Content.Roslyn;
using MyLittleContentEngine.UI.Components;

namespace MyLittleContentEngine.DocSite;

/// <summary>
/// Extension methods for configuring a documentation site
/// </summary>
public static class DocSiteServiceExtensions
{
    /// <summary>
    /// Configures all services required for a documentation site
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Configuration action for DocSiteOptions</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddDocSite(this IServiceCollection services, Action<DocSiteOptions> configureOptions)
    {
        var options = new DocSiteOptions();
        configureOptions(options);
        
        services.AddSingleton(options);
        services.AddRazorComponents();

        // Add Mdazor with UI components
        services.AddMdazor()
            .AddMdazorComponent<Card>()
            .AddMdazorComponent<CardGrid>()
            .AddMdazorComponent<LinkCard>()
            .AddMdazorComponent<Step>()
            .AddMdazorComponent<Steps>();

        // Configure content engine
        services.AddContentEngineService(_ => new ContentEngineOptions
        {
            SiteTitle = options.SiteTitle,
            SiteDescription = options.Description,
            BaseUrl = options.BaseUrl,
            ContentRootPath = options.ContentRootPath,
            CanonicalBaseUrl = options.CanonicalBaseUrl
        });

        // Configure content service
        services.AddContentEngineStaticContentService(_ => new ContentEngineContentOptions<DocSiteFrontMatter>()
        {
            ContentPath = options.ContentRootPath,
            BasePageUrl = string.Empty,
            ExcludeSubfolders = false,
            PostFilePattern = "*.md;*.mdx"
        });

        // Configure MonorailCSS
        services.AddMonorailCss(_ => new MonorailCssOptions
        {
            PrimaryHue = () => options.PrimaryHue,
            BaseColorName = () => options.BaseColorName,
            CustomCssFrameworkSettings = defaultSettings => defaultSettings with
            {
                DesignSystem = defaultSettings.DesignSystem with
                {
                    FontFamilies = defaultSettings.DesignSystem.FontFamilies
                        .Add("display", new FontFamilyDefinition("Lexend, sans-serif"))
                },
            },
            ExtraStyles = $"{options.ExtraStyles ?? ""}{Environment.NewLine}{GoogleFonts.GetLexendStyles()}",
        });

        // Configure Roslyn service if solution path is provided
        if (!string.IsNullOrEmpty(options.SolutionPath))
        {
            services.AddRoslynService(_ => new RoslynHighlighterOptions()
            {
                ConnectedSolution = new ConnectedDotNetSolution()
                {
                    SolutionPath = options.SolutionPath,
                }
            });
        }

        // Configure API reference service if options are provided
        if (options.ApiReferenceContentOptions != null)
        {
            services.AddApiReferenceContentService(_ => options.ApiReferenceContentOptions);
        }
        else if (options.IncludeNamespaces != null || options.ExcludeNamespaces != null)
        {
            services.AddApiReferenceContentService(_ => new ApiReferenceContentOptions()
            {
                IncludeNamespace = options.IncludeNamespaces ?? [],
                ExcludedNamespace = options.ExcludeNamespaces ?? [],
            });
        }

        return services;
    }

    /// <summary>
    /// Configures the web application to use the documentation site
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application</returns>
    public static WebApplication UseDocSite(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<DocSiteOptions>();
        
        app.UseAntiforgery();
        app.UseStaticFiles();
        app.MapRazorComponents<App>()
            .AddAdditionalAssemblies(options.AdditionalRoutingAssemblies);

        app.UseMonorailCss();
        
        return app;
    }

    /// <summary>
    /// Runs the documentation site or builds static content based on arguments
    /// </summary>
    /// <param name="app">The web application</param>
    /// <param name="args">Command line arguments</param>
    /// <returns>Task</returns>
    public static async Task RunDocSiteAsync(this WebApplication app, string[] args)
    {
        await app.RunOrBuildContent(args);
    }

}