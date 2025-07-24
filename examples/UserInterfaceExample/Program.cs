using UserInterfaceExample;
using UserInterfaceExample.Components;
using MyLittleContentEngine;
using MyLittleContentEngine.MonorailCss;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

// configures site wide settings
// hot reload note - these will not be reflected until the application restarts
builder.Services.AddContentEngineService(_ => new ContentEngineOptions
{
    SiteTitle = "Daily Life Hub",
    SiteDescription = "Your everyday life, simplified",
    ContentRootPath = "Content",
});

// Register OutputOptions to handle command line arguments and environment variables
builder.Services.AddOutputOptions(args);

builder.Services.AddContentEngineStaticContentService(_ => new ContentEngineContentOptions<DocsFrontMatter>()
{
    ContentPath = "Content",
    BasePageUrl = ""
});

builder.Services.AddMonorailCss();

var app = builder.Build();
app.UseAntiforgery();
app.UseStaticFiles();
app.MapRazorComponents<App>();
app.UseMonorailCss();

await app.RunOrBuildContent(args);