using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;

namespace MyLittleContentEngine.Services.Spa;

/// <summary>
/// Builder for configuring SPA navigation slot renderers and options.
/// </summary>
public class SpaNavigationBuilder(IServiceCollection services)
{
    /// <summary>
    /// Registers a custom <see cref="ISpaIslandRenderer"/> implementation.
    /// </summary>
    public SpaNavigationBuilder AddIsland<TRenderer>() where TRenderer : class, ISpaIslandRenderer
    {
        services.AddTransient<ISpaIslandRenderer, TRenderer>();
        return this;
    }

    /// <summary>
    /// Sets the URL path prefix for page data endpoints.
    /// </summary>
    public SpaNavigationBuilder WithDataPath(string path)
    {
        // Remove any existing registration and re-add with the new path.
        var existing = services.FirstOrDefault(d => d.ServiceType == typeof(SpaNavigationOptions));
        if (existing is not null) services.Remove(existing);
        services.AddSingleton(new SpaNavigationOptions { DataPath = path });
        return this;
    }
}

/// <summary>
/// Extension methods for registering and using SPA navigation.
/// </summary>
public static class SpaNavigationExtensions
{
    /// <summary>
    /// Adds SPA navigation services with a metadata provider and page data endpoint.
    /// Register slot renderers via the <paramref name="configure"/> callback.
    /// Call this after <see cref="ContentEngineExtensions.WithMarkdownContentService{TFrontMatter}"/>.
    /// </summary>
    /// <typeparam name="TFrontMatter">The front matter type used by the markdown content service.</typeparam>
    /// <param name="services">The configured content engine service collection.</param>
    /// <param name="configure">Optional builder action to add custom slot renderers or change the data path.</param>
    /// <returns>The service collection for further chaining.</returns>
    public static IConfiguredContentEngineServiceCollection WithSpaNavigation<TFrontMatter>(
        this IConfiguredContentEngineServiceCollection services,
        Action<SpaNavigationBuilder>? configure = null)
        where TFrontMatter : class, IFrontMatter, new()
    {
        // Register options (if not already set by the builder).
        if (services.All(d => d.ServiceType != typeof(SpaNavigationOptions)))
            services.AddSingleton(new SpaNavigationOptions());

        // Component renderer for Razor-based slot renderers.
        services.AddScoped<ComponentRenderer>();

        // Core orchestrator.
        services.AddTransient<SpaPageDataService>();

        // Metadata provider.
        services.AddTransient<ISpaPageMetadataProvider, MarkdownSpaMetadataProvider<TFrontMatter>>();

        // Content service for static generation of _spa-data/*.json files.
        services.AddTransient<IContentService>(provider =>
        {
            var markdownService = provider.GetRequiredService<IMarkdownContentService<TFrontMatter>>();
            var options = provider.GetRequiredService<SpaNavigationOptions>();
            return new SpaNavigationContentService<TFrontMatter>(markdownService, options);
        });

        // Apply user customisations.
        configure?.Invoke(new SpaNavigationBuilder(services));

        return services;
    }

    /// <summary>
    /// Maps the SPA page data endpoint. Call this in the middleware pipeline
    /// after <c>UseStaticFiles</c>.
    /// </summary>
    public static WebApplication UseSpaNavigation(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<SpaNavigationOptions>();
        var dataPath = options.DataPath.TrimStart('/');

        app.MapGet($"/{dataPath}/{{*slug}}", async (string? slug, SpaPageDataService service) =>
        {
            if (string.IsNullOrEmpty(slug))
                return Results.NotFound();

            if (slug.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                slug = slug[..^5];

            var data = await service.GetPageDataAsync(slug);
            return data is null
                ? Results.NotFound()
                : Results.Content(SpaPageDataService.Serialize(data), "application/json");
        });

        return app;
    }
}
