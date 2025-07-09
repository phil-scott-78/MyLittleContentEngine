using MinimalExample;
using MinimalExample.Components;
using MyLittleContentEngine;
using MyLittleContentEngine.MonorailCss;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

// configures site wide settings
// hot reload note - these will not be reflected until the application restarts
builder.Services.AddContentEngineService(_ => new ContentEngineOptions
{
    SiteTitle = "My Little Content Engine",
    SiteDescription = "An Inflexible Content Engine for .NET",
    BaseUrl =  Environment.GetEnvironmentVariable("BaseUrl") ?? "/",
    ContentRootPath = "Content",
});

// configures individual sections of the blog. PageUrl should match the configured razor pages route,
// and contentPath should match the location on disk.
// you can have multiple of these per site.
builder.Services.AddContentEngineStaticContentService(_ => new ContentEngineContentOptions<BlogFrontMatter>()
{
    ContentPath = "Content",
    BasePageUrl = string.Empty
});

builder.Services.AddMonorailCss();

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>();
app.UseMonorailCss();

await app.RunOrBuildContent(args);