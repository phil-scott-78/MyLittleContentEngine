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
    })
    .WithMarkdownContentService(_ => new MarkdownContentOptions<ContentFrontMatter>()
    {
        ContentPath = "Content",
        BasePageUrl = "",
        ExcludeSubfolders = true,
    }).WithMarkdownContentService(_ => new MarkdownContentOptions<BlogFrontMatter>()
    {
        ContentPath = "Content/blog",
        BasePageUrl = "/blog"
    }).WithMarkdownContentService(_ => new MarkdownContentOptions<DocsFrontMatter>()
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