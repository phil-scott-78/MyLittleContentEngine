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
    }).WithMarkdownContentService(_ => new MarkdownContentOptions<BlogFrontMatter>()
    {
        // configures individual sections of the blog. PageUrl should match the configured razor pages route,
// and contentPath should match the location on disk.
// you can have multiple of these per site.
        ContentPath = "Content",
        BasePageUrl = string.Empty
    }).WithConnectedRoslynSolution(_ => new CodeAnalysisOptions()
    {
        // this is required if you are using the API content service

        SolutionPath = "../../MyLittleContentEngine.slnx"
    })
    .WithApiReferenceContentService(_ => new ApiReferenceContentOptions()
    {
        // Add API reference content service

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