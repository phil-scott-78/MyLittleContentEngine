using RoslynIntegrationExample;
using RoslynIntegrationExample.Components;
using MyLittleContentEngine;
using MyLittleContentEngine.MonorailCss;
using MyLittleContentEngine.Services.Content.CodeAnalysis.Configuration;

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

// configures individual sections of the blog. PageUrl should match the configured razor pages route,
// and contentPath should match the location on disk.
// you can have multiple of these per site.
builder.Services.AddContentEngineStaticContentService(_ => new MarkdownContentOptions<BlogFrontMatter>()
{
    ContentPath = "Content",
    BasePageUrl = string.Empty
});

builder.Services.AddMonorailCss();

// Add Roslyn service for documentation
builder.Services.AddConnectedRoslynSolution(_ => new CodeAnalysisOptions()
{
    SolutionPath = "../../MyLittleContentEngine.sln"
});

var app = builder.Build();
app.UseAntiforgery();
app.UseStaticFiles();
app.MapRazorComponents<App>();
app.UseMonorailCss();

await app.RunOrBuildContent(args);