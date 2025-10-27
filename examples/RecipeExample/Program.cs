using MonorailCss.Theme;
using MyLittleContentEngine;
using MyLittleContentEngine.MonorailCss;
using RecipeExample;
using RecipeExample.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();


builder.Services.AddContentEngineService(_ => new ContentEngineOptions
    {
        SiteTitle = "Recipe Collection",
        SiteDescription = "CookLang Recipe Website",
        ContentRootPath = "recipes",
    })
    .AddRecipeContentService(options =>
    {
        options.RecipePath = "recipes";
        options.FilePattern = "*.cook";
    })
    .AddResponsiveImageContentService();

builder.Services.AddMonorailCss(_ => new MonorailCssOptions()
{
    ColorScheme = new AlgorithmicColorScheme
    {
        PrimaryHue = 55,
        BaseColorName = ColorNames.Neutral
    },
    CustomCssFrameworkSettings = settings => settings with
    {
        Theme = settings.Theme
            .AddFontFamily("display","'Montserrat Alternates', sans-serif")
            .AddFontFamily("sans", "Inter, sans-serif")
    }
});

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>();
app.UseMonorailCss();

// Responsive image endpoint
app.MapGet("/images/{filename}-{size}.webp",
    async (string filename, string size, IResponsiveImageContentService imageService) =>
    {
        var imageData = await imageService.ProcessImageAsync(filename, size);

        if (imageData == null)
        {
            return Results.NotFound();
        }

        return Results.File(imageData, "image/webp");
    });

await app.RunOrBuildContent(args);