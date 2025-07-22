using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using MyLittleContentEngine.Services.Content;

namespace RecipeExample;

public static class ResponsiveImageContentServiceExtensions
{
    public static IServiceCollection AddResponsiveImageContentService(this IServiceCollection services, RecipeContentOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IResponsiveImageContentService, ResponsiveImageContentService>();
        services.AddSingleton<IContentService>(sp => sp.GetRequiredService<IResponsiveImageContentService>());
        return services;
    }
}