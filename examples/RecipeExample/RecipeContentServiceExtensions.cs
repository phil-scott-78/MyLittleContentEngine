using System.IO.Abstractions;
using MyLittleContentEngine;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Infrastructure;

namespace RecipeExample;

public static class RecipeContentServiceExtensions
{
    public static IServiceCollection AddRecipeContentService(
        this IServiceCollection services,
        Action<RecipeContentOptions>? configureOptions = null)
    {
        var options = new RecipeContentOptions();
        configureOptions?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IContentOptions>(sp => sp.GetRequiredService<RecipeContentOptions>());
        services.AddSingleton<IRecipeContentService, RecipeContentService>();
        services.AddSingleton<IContentService>(provider => provider.GetRequiredService<IRecipeContentService>());
        

        return services;
    }
}
