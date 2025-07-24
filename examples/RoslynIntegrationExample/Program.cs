using RoslynIntegrationExample;
using RoslynIntegrationExample.Components;
using MyLittleContentEngine;
using MyLittleContentEngine.MonorailCss;
using MyLittleContentEngine.Services.Content.Roslyn;

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

// configures individual sections of the blog. PageUrl should match the configured razor pages route,
// and contentPath should match the location on disk.
// you can have multiple of these per site.
builder.Services.AddContentEngineStaticContentService(_ => new ContentEngineContentOptions<BlogFrontMatter>()
{
    ContentPath = "Content",
    BasePageUrl = string.Empty
});

builder.Services.AddMonorailCss();

// Add Roslyn service for code syntax highlighting and documentation
builder.Services.AddRoslynService(_ => new RoslynHighlighterOptions()
{
    ConnectedSolution = new ConnectedDotNetSolution()
    {
        SolutionPath = "../../MyLittleContentEngine.sln",
    }
});

var app = builder.Build();
app.UseAntiforgery();
app.UseStaticFiles();
app.MapRazorComponents<App>();
app.UseMonorailCss();

await app.RunOrBuildContent(args);