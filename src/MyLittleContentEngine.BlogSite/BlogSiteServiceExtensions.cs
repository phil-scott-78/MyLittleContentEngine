using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MonorailCss;
using MyLittleContentEngine.BlogSite.Components;
using MyLittleContentEngine.MonorailCss;

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

        // Configure content engine
        services.AddContentEngineService(sp =>
        {
            var o = sp.GetRequiredService<BlogSiteOptions>();

            return new ContentEngineOptions
            {
                SiteTitle = o.SiteTitle,
                SiteDescription = o.Description,
                BaseUrl = o.BaseUrl,
                ContentRootPath = o.ContentRootPath,
                CanonicalBaseUrl = o.CanonicalBaseUrl
            };
        });

        // Configure content service
        services.AddContentEngineStaticContentService(sp =>
        {
            var o = sp.GetRequiredService<BlogSiteOptions>();

            return new ContentEngineContentOptions<BlogSiteFrontMatter>()
            {
                ContentPath = Path.Combine(o.ContentRootPath, o.BlogContentPath),
                BasePageUrl = o.BlogBaseUrl,
                Tags = new TagsOptions()
                {
                    TagsPageUrl = o.TagsPageUrl
                },
                ExcludeSubfolders = false,
                PostFilePattern = "*.md;*.mdx"
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
                            .Add("display", new FontFamilyDefinition(o.DisplayFontFamily ?? "Lexend, sans-serif"))
                            .SetItem("sans", new FontFamilyDefinition(o.BodyFontFamily ?? "Lexend, sans-serif"))
                    },
                },
                ExtraStyles = $"{o.ExtraStyles ?? ""}{Environment.NewLine}{GetFontStyles(o)}",
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

    private static string GetFontStyles(BlogSiteOptions options)
    {
        if (options.DisplayFontFamily?.Contains("Lexend") == true)
        {
            return """
                   @import url('https://fonts.googleapis.com/css2?family=Lexend:wght@100..900&display=swap');
                   """;
        }

        return string.Empty;
    }
}