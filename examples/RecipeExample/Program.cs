
using System.Net.Mime;
using MonorailCss;
using MyLittleContentEngine;
using MyLittleContentEngine.MonorailCss;
using RecipeExample;
using RecipeExample.Components;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();


builder.Services.AddContentEngineService(_ => new ContentEngineOptions
{
    SiteTitle = "Recipe Collection",
    SiteDescription = "CookLang Recipe Website",
    BaseUrl =  Environment.GetEnvironmentVariable("BaseUrl") ?? "/",
    ContentRootPath = "recipes",
});

builder.Services.AddRecipeContentService(options =>
{
    options.RecipePath = "recipes";
    options.FilePattern = "*.cook";
});

var recipeOptions = new RecipeContentOptions 
{ 
    RecipePath = "recipes", 
    FilePattern = "*.cook" 
};

builder.Services.AddSingleton(recipeOptions);
builder.Services.AddResponsiveImageContentService(recipeOptions);

builder.Services.AddMonorailCss(provider => new MonorailCssOptions()
{
    PrimaryHue = () => 310,
    BaseColorName = () => ColorNames.Neutral,
    CustomCssFrameworkSettings = settings => settings with
    {
        DesignSystem = settings.DesignSystem with
        {
            FontFamilies =
            settings.DesignSystem.FontFamilies
                .SetItem("sans", new FontFamilyDefinition("Inter, sans-serif"))
                .Add("display", new FontFamilyDefinition("'Montserrat Alternates', sans-serif"))
        }
    }
});

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>();
app.UseMonorailCss();

// Responsive image endpoint
app.MapGet("/images/{filename}-{size}.webp", async (string filename, string size, IResponsiveImageContentService imageService) =>
{
    var imageData = await imageService.ProcessImageAsync(filename, size);
    
    if (imageData == null)
    {
        return Results.NotFound();
    }

    return Results.File(imageData, "image/webp");
});

await app.RunOrBuildContent(args);