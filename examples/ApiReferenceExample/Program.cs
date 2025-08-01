using ApiReferenceExample;
using ApiReferenceExample.Components;
using MyLittleContentEngine;
using MyLittleContentEngine.MonorailCss;
using MyLittleContentEngine.Services.Content.CodeAnalysis.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

// configures site wide settings
// hot reload note - these will not be reflected until the application restarts
builder.Services.AddContentEngineService(_ => new ContentEngineOptions
{
    SiteTitle = "API Reference Example",
    SiteDescription = "Demonstrating API Reference Content Service",
    ContentRootPath = "Content",
});

// Register OutputOptions to handle command line arguments and environment variables
builder.Services.AddOutputOptions(args);

// configures individual sections of the blog. PageUrl should match the configured razor pages route,
// and contentPath should match the location on disk.
// you can have multiple of these per site.
builder.Services.AddContentEngineStaticContentService(_ => new MarkdownContentOptions<BlogFrontMatter>()
{
    ContentPath = "Content",
    BasePageUrl = string.Empty
});

// this is required if you are using the API content service
builder.Services.AddConnectedRoslynSolution(_ => new CodeAnalysisOptions()
{
    SolutionPath = "../../MyLittleContentEngine.sln"
});

// Add API reference content service
builder.Services.AddApiReferenceContentService(_ => new ApiReferenceContentOptions()
{
    IncludeNamespace = ["MyLittleContentEngine"],
    ExcludedNamespace = ["MyLittleContentEngine.Tests"],
});

builder.Services.AddMonorailCss();

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>();
app.UseMonorailCss();

await app.RunOrBuildContent(args);