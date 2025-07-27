using MyLittleContentEngine;
using MyLittleContentEngine.MonorailCss;
using MultipleContentSourceExample;
using MultipleContentSourceExample.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

// configures site wide settings
// hot reload note - these will not be reflected until the application restarts
builder.Services.AddContentEngineService(_ => new ContentEngineOptions
{
    SiteTitle = "My Little Content Engine",
    SiteDescription = "An Inflexible Content Engine for .NET",
    ContentRootPath = "Content",
});

// Register OutputOptions to handle command line arguments and environment variables
builder.Services.AddOutputOptions(args);

builder.Services.AddContentEngineStaticContentService(_ => new ContentEngineContentOptions<ContentFrontMatter>()
{
    ContentPath = "Content",
    BasePageUrl = "",
    ExcludeSubfolders = true,
});

builder.Services.AddContentEngineStaticContentService(_ => new ContentEngineContentOptions<BlogFrontMatter>()
{
    ContentPath = "Content/blog",
    BasePageUrl = "/blog"
});

builder.Services.AddContentEngineStaticContentService(_ => new ContentEngineContentOptions<DocsFrontMatter>()
{
    ContentPath = "Content/docs",
    BasePageUrl = "/docs"
});

builder.Services.AddMonorailCss();

var app = builder.Build();
app.UseAntiforgery();
app.UseStaticFiles();
app.MapRazorComponents<App>();
app.UseMonorailCss();

await app.RunOrBuildContent(args);