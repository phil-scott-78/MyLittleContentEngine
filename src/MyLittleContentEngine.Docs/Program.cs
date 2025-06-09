using MonorailCss;
using MyLittleContentEngine;
using MyLittleContentEngine.Docs;
using MyLittleContentEngine.Docs.Components;
using MyLittleContentEngine.MonorailCss;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.Tabs;
using MyLittleContentEngine.Services.Content.Roslyn;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

// configures site wide settings
// hot reload note - these will not be reflected until the application restarts
builder.Services.AddContentEngineService(() => new ContentEngineOptions
{
    SiteTitle = "My Little Content Engine",
    SiteDescription = "An Inflexible Content Engine for .NET",
    BaseUrl =  Environment.GetEnvironmentVariable("BaseHref") ?? "/",
    ContentRootPath = "Content",
});

// configures individual sections of the blog. PageUrl should match the configured razor pages route,
// and contentPath should match the location on disk.
// you can have multiple of these per site.
builder.Services.AddContentEngineStaticContentService(() => new ContentEngineContentOptions<DocsFrontMatter>()
{
    ContentPath = "Content",
    BasePageUrl = string.Empty
});

builder.Services.AddMonorailCss(new MonorailCssOptions
{
    PrimaryHue = () => 220,
    BaseColorName = () => ColorNames.Neutral,
});
builder.Services.AddRoslynService(() => new RoslynHighlighterOptions()
{
    CodeHighlightRenderOptionsFactory = () => CodeHighlightRenderOptions.MonorailMono,
    TabbedCodeBlockRenderOptionsFactory = () => TabbedCodeBlockRenderOptions.MonorailMono,
});

var app = builder.Build();
app.UseAntiforgery();
app.UseStaticFiles();
app.MapRazorComponents<App>();
app.UseMonorailCss();

await app.RunOrBuildContent(args);