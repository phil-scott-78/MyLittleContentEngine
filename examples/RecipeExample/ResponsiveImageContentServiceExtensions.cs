using System.IO.Abstractions;
using MyLittleContentEngine;
using MyLittleContentEngine.Services.Content;
using Testably.Abstractions;

namespace RecipeExample;

public static class ResponsiveImageContentServiceExtensions
{
    public static IConfiguredContentEngineServiceCollection AddResponsiveImageContentService(this IConfiguredContentEngineServiceCollection services)
    {
        services.AddSingleton<IFileSystem, RealFileSystem>();
        services.AddSingleton<IResponsiveImageContentService, ResponsiveImageContentService>();
        services.AddSingleton<IContentService>(sp => sp.GetRequiredService<IResponsiveImageContentService>());
        return services;
    }
}