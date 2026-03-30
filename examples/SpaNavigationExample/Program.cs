using MyLittleContentEngine;
using MyLittleContentEngine.MonorailCss;
using MyLittleContentEngine.Services.Spa;
using SpaNavigationExample;
using SpaNavigationExample.Components;
using SpaNavigationExample.Slots;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

builder.Services.AddContentEngineService(_ => new ContentEngineOptions
    {
        SiteTitle = "My Recipe Book",
        SiteDescription = "A cookbook powered by SPA slots",
        ContentRootPath = "Content",
    })
    .WithMarkdownContentService(_ => new MarkdownContentOptions<RecipeFrontMatter>
    {
        ContentPath = "Content",
        BasePageUrl = "",
    })
    .WithSpaNavigation<RecipeFrontMatter>(spa =>
    {
        // Replace the built-in content renderer with our recipe-aware one
        // that includes the title header and prose wrapper.
        spa.AddIsland<RecipeContentSlotRenderer>();

        // Add the domain-specific recipe info sidebar slot.
        spa.AddIsland<RecipeInfoSlotRenderer>();
    });

builder.Services.AddMonorailCss();

var app = builder.Build();
app.UseAntiforgery();
app.UseStaticFiles();
app.MapRazorComponents<App>();
app.UseMonorailCss();
app.UseSpaNavigation();

await app.RunOrBuildContent(args);
