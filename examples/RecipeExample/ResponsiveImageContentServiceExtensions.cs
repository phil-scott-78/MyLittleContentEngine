using System.IO.Abstractions;
using MyLittleContentEngine;
using MyLittleContentEngine.Services.Content;

namespace RecipeExample;

public static class ResponsiveImageContentServiceExtensions
{
    public static IConfiguredContentEngineServiceCollection AddResponsiveImageContentService(this IConfiguredContentEngineServiceCollection  services)
    {
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IResponsiveImageContentService, ResponsiveImageContentService>();
        services.AddSingleton<IContentService>(sp => sp.GetRequiredService<IResponsiveImageContentService>());
        return services;
    }
}