using MonorailCss.Theme;
using Spectre.Console;
using Spectre.Console.Components;
using MyLittleContentEngine;
using MyLittleContentEngine.MonorailCss;
using MyLittleContentEngine.Services.Content.CodeAnalysis.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

// configures site wide settings
// hot reload note - these will not be reflected until the application restarts
builder.Services.AddContentEngineService(_ => new ContentEngineOptions
{
    SiteTitle = "Spectre.Console Documentation",
    SiteDescription = "Beautiful console applications with Spectre.Console",
    ContentRootPath = "Content",
})
// Console documentation service
.WithMarkdownContentService(_ => new MarkdownContentOptions<SpectreConsoleFrontMatter>()
{
    ContentPath = "Content/console",
    BasePageUrl = "/console",
    TableOfContentsSectionKey = "console"
})
// CLI documentation service
.WithMarkdownContentService(_ => new MarkdownContentOptions<SpectreConsoleCliFrontMatter>()
{
    ContentPath = "Content/cli",
    BasePageUrl = "/cli",
    TableOfContentsSectionKey = "cli"
    
})
// Blog service
.WithMarkdownContentService(_ => new MarkdownContentOptions<BlogFrontMatter>()
{
    ContentPath = "Content/blog",
    BasePageUrl = "/blog",
    ExcludeSubfolders = false,
    PostFilePattern = "*.md;*.mdx"
})
.WithConnectedRoslynSolution(_ => new CodeAnalysisOptions()
{
    SolutionPath = "../../MyLittleContentEngine.slnx",
})
;

builder.Services.AddMonorailCss(_ => new MonorailCssOptions()
{
    ColorScheme = new NamedColorScheme()
    {
        PrimaryColorName = ColorNames.Sky,
        BaseColorName = ColorNames.Zinc,
        AccentColorName = ColorNames.Pink,
        TertiaryOneColorName = ColorNames.Indigo,
        TertiaryTwoColorName = ColorNames.Violet
    }
});

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>();
app.UseMonorailCss();

await app.RunOrBuildContent(args);