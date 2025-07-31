using Mdazor;
using Microsoft.AspNetCore.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using MonorailCss;
using MyLittleContentEngine.DocSite.Components;
using MyLittleContentEngine.MonorailCss;
using MyLittleContentEngine.Services.Content.CodeAnalysis.Configuration;
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
    public static IServiceCollection AddDocSite(this IServiceCollection services,
        Func<IServiceProvider, DocSiteOptions> configureOptions)
    {
        services.AddTransient(configureOptions);
        services.AddRazorComponents();

        // Add Mdazor with UI components
        services.AddMdazor()
            .AddMdazorComponent<Badge>()
            .AddMdazorComponent<Card>()
            .AddMdazorComponent<CardGrid>()
            .AddMdazorComponent<LinkCard>()
            .AddMdazorComponent<Step>()
            .AddMdazorComponent<Steps>()
            .AddMdazorComponent<BigTable>();

        // Configure content engine
        services.AddContentEngineService(sp =>
        {
            var options = sp.GetRequiredService<DocSiteOptions>();
            return new ContentEngineOptions
            {
                SiteTitle = options.SiteTitle,
                SiteDescription = options.Description,
                ContentRootPath = options.ContentRootPath,
                CanonicalBaseUrl = options.CanonicalBaseUrl
            };
        });


        // Configure content service
        services.AddContentEngineStaticContentService(sp =>
        {
            var options = sp.GetRequiredService<DocSiteOptions>();

            return new MarkdownContentOptions<DocSiteFrontMatter>()
            {
                ContentPath = options.ContentRootPath,
                BasePageUrl = string.Empty,
                ExcludeSubfolders = false,
                PostFilePattern = "*.md;*.mdx"
            };
        });

        // Configure MonorailCSS
        services.AddMonorailCss(sp =>
        {
            var options = sp.GetRequiredService<DocSiteOptions>();

            return new MonorailCssOptions
            {
                PrimaryHue = () => options.PrimaryHue,
                BaseColorName = () => options.BaseColorName,
                CustomCssFrameworkSettings = defaultSettings => defaultSettings with
                {
                    DesignSystem = defaultSettings.DesignSystem with
                    {
                        FontFamilies = defaultSettings.DesignSystem.FontFamilies
                            .Add("display", new FontFamilyDefinition(options.DisplayFontFamily ?? "-apple-system, BlinkMacSystemFont, avenir next, avenir, segoe ui, helvetica neue, Adwaita Sans, Cantarell, Ubuntu, roboto, noto, helvetica, arial, sans-serif;"))
                            .SetItem("sans", new FontFamilyDefinition(options.BodyFontFamily ?? "-apple-system, BlinkMacSystemFont, avenir next, avenir, segoe ui, helvetica neue, Adwaita Sans, Cantarell, Ubuntu, roboto, noto, helvetica, arial, sans-serif;"))
                    },
                },
                ExtraStyles = options.ExtraStyles ?? string.Empty
            };
        });

        // Configure Roslyn service if solution path is provided
        services.AddRoslynService(sp =>
        {
            var o = sp.GetRequiredService<DocSiteOptions>();

            if (string.IsNullOrWhiteSpace(o.SolutionPath))
            {
                return new RoslynHighlighterOptions();
            }

            return new RoslynHighlighterOptions
            {
                ConnectedSolution = new ConnectedDotNetSolution
                {
                    SolutionPath = o.SolutionPath,
                }
            };
        });

        // we need to resolve the options to see if we should add the rest.
        var sp = services.BuildServiceProvider();
        var tempOptions = sp.GetRequiredService<DocSiteOptions>();

        if (tempOptions.ApiReferenceContentOptions != null)
        {
            services.AddApiReferenceContentService(_ => tempOptions.ApiReferenceContentOptions);
        }
        else if (tempOptions.IncludeNamespaces != null || tempOptions.ExcludeNamespaces != null)
        {
            services.AddApiReferenceContentService(_ => new ApiReferenceContentOptions()
            {
                IncludeNamespace = tempOptions.IncludeNamespaces ?? [],
                ExcludedNamespace = tempOptions.ExcludeNamespaces ?? [],
            });
        }

        // add the output options
        services.AddOutputOptions(tempOptions.ApplicationArgs);
        
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