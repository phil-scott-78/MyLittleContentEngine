using Mdazor;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MonorailCss;
using MyLittleContentEngine.BlogSite.Components;
using MyLittleContentEngine.MonorailCss;
using MyLittleContentEngine.Services.Content.Roslyn;
using MyLittleContentEngine.UI.Components;

namespace MyLittleContentEngine.BlogSite;

/// <summary>
/// Extension methods for configuring a blog site
/// </summary>
public static class BlogSiteServiceExtensions
{
    /// <summary>
    /// Configures all services required for a blog site
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Configuration action for BlogSiteOptions</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddBlogSite(this IServiceCollection services,
        Func<IServiceProvider,  BlogSiteOptions> configureOptions)
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
            .AddMdazorComponent<Steps>();
        
        // Configure content engine
        services.AddContentEngineService(sp =>
        {
            var o = sp.GetRequiredService<BlogSiteOptions>();

            return new ContentEngineOptions
            {
                SiteTitle = o.SiteTitle,
                SiteDescription = o.Description,
                ContentRootPath = o.ContentRootPath,
                CanonicalBaseUrl = o.CanonicalBaseUrl
            };
        });

        // Register OutputOptions from BlogSiteOptions
        services.AddSingleton<OutputOptions>(sp =>
        {
            var o = sp.GetRequiredService<BlogSiteOptions>();
            return new OutputOptions { BaseUrl = o.BaseUrl, OutputFolderPath = o.OutputPath};
        });

        // Configure content service
        services.AddContentEngineStaticContentService(sp =>
        {
            var o = sp.GetRequiredService<BlogSiteOptions>();

            return new ContentEngineContentOptions<BlogSiteFrontMatter>
            {
                ContentPath = Path.Combine(o.ContentRootPath, o.BlogContentPath),
                BasePageUrl = o.BlogBaseUrl,
                Tags = new TagsOptions
                {
                    TagsPageUrl = o.TagsPageUrl
                },
                ExcludeSubfolders = false,
                PostFilePattern = "*.md;*.mdx"
            };
        });

        
        services.AddRoslynService(sp =>
        {
            var o = sp.GetRequiredService<BlogSiteOptions>();

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
        
        // Configure MonorailCSS
        services.AddMonorailCss(sp =>
        {
            var o = sp.GetRequiredService<BlogSiteOptions>();

            return new MonorailCssOptions
            {
                PrimaryHue = () => o.PrimaryHue,
                BaseColorName = () => o.BaseColorName,
                CustomCssFrameworkSettings = defaultSettings => defaultSettings with
                {
                    DesignSystem = defaultSettings.DesignSystem with
                    {
                        FontFamilies = defaultSettings.DesignSystem.FontFamilies
                            .Add("display", new FontFamilyDefinition(o.DisplayFontFamily ?? "-apple-system, BlinkMacSystemFont, avenir next, avenir, segoe ui, helvetica neue, Adwaita Sans, Cantarell, Ubuntu, roboto, noto, helvetica, arial, sans-serif;"))
                            .SetItem("sans", new FontFamilyDefinition(o.BodyFontFamily ?? "-apple-system, BlinkMacSystemFont, avenir next, avenir, segoe ui, helvetica neue, Adwaita Sans, Cantarell, Ubuntu, roboto, noto, helvetica, arial, sans-serif;"))
                    },
                },
                ExtraStyles = o.ExtraStyles ?? string.Empty
            };
        });

        return services;
    }

    /// <summary>
    /// Configures the web application to use the blog site
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application</returns>
    public static WebApplication UseBlogSite(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<BlogSiteOptions>();

        app.UseAntiforgery();
        app.UseStaticFiles();
        app.MapRazorComponents<App>()
            .AddAdditionalAssemblies(options.AdditionalRoutingAssemblies);

        app.UseMonorailCss();

        return app;
    }

    /// <summary>
    /// Runs the blog site or builds static content based on arguments
    /// </summary>
    /// <param name="app">The web application</param>
    /// <param name="args">Command line arguments</param>
    /// <returns>Task</returns>
    public static async Task RunBlogSiteAsync(this WebApplication app, string[] args)
    {
        await app.RunOrBuildContent(args);
    }
}