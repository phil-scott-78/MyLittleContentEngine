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
    })
    .WithMarkdownContentService(_ => new MarkdownContentOptions<BlogFrontMatter>()
    {
        // configures individual sections of the blog. PageUrl should match the configured razor pages route,
        // and contentPath should match the location on disk.
        // you can have multiple of these per site.
        ContentPath = "Content",
        BasePageUrl = string.Empty
    })
    .WithConnectedRoslynSolution(_ => new CodeAnalysisOptions()
    {
        // Add Roslyn service for documentation
        SolutionPath = "../../MyLittleContentEngine.slnx"
    });

builder.Services.AddMonorailCss();

var app = builder.Build();
app.UseAntiforgery();
app.UseStaticFiles();
app.MapRazorComponents<App>();
app.UseMonorailCss();

await app.RunOrBuildContent(args);