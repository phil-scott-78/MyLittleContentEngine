using Mdazor;
using MonorailCss;
using MyLittleContentEngine;
using MyLittleContentEngine.Docs;
using MyLittleContentEngine.Docs.Components;
using MyLittleContentEngine.MonorailCss;
using MyLittleContentEngine.Services.Content.Roslyn;
using MyLittleContentEngine.UI.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

builder.Services.AddMdazor()
    .AddMdazorComponent<Card>()
    .AddMdazorComponent<CardGrid>()
    .AddMdazorComponent<LinkCard>()
    .AddMdazorComponent<Step>()
    .AddMdazorComponent<Steps>();

// configures site wide settings
// hot reload note - these will not be reflected until the application restarts
builder.Services.AddContentEngineService(_ => new ContentEngineOptions
{
    SiteTitle = "My Little Content Engine",
    SiteDescription = "An Inflexible Content Engine for .NET",
    BaseUrl =  Environment.GetEnvironmentVariable("BaseUrl") ?? "/",
    ContentRootPath = "Content",
    CanonicalBaseUrl = Environment.GetEnvironmentVariable("CanonicalBaseUrl") ?? "https://phil-scott-78.github.io/MyLittleContentEngine/" // for sitemap.xml and RSS feed, 
});

// configures individual sections of the blog. PageUrl should match the configured razor pages route,
// and contentPath should match the location on disk.
// you can have multiple of these per site.
builder.Services.AddContentEngineStaticContentService(_ => new ContentEngineContentOptions<DocsFrontMatter>()
{
    ContentPath = "Content",
    BasePageUrl = string.Empty,
    ExcludeSubfolders = false,
    PostFilePattern = "*.md;*.mdx"
});

builder.Services.AddMonorailCss(_ => new MonorailCssOptions
{
    PrimaryHue = () => 250,
    BaseColorName = () => ColorNames.Zinc,
    CustomCssFrameworkSettings = defaultSettings => defaultSettings with
    {
        DesignSystem = defaultSettings.DesignSystem with
        {
            FontFamilies = defaultSettings.DesignSystem.FontFamilies
                .Add("display", new FontFamilyDefinition("Lexend, sans-serif"))
                .SetItem(
                    "mono", 
                    new FontFamilyDefinition("\"Cascadia Code\"" + defaultSettings.DesignSystem.FontFamilies["mono"].FontFamily))
        },
    },
    ExtraStyles = GoogleFonts.GetLexendStyles(),
});

builder.Services.AddRoslynService(_ => new RoslynHighlighterOptions()
{
    ConnectedSolution = new ConnectedDotNetSolution()
    {
        SolutionPath = "../../MyLittleContentEngine.sln",
    }
});

// Add API reference content service
builder.Services.AddApiReferenceContentService(_ => new ApiReferenceContentOptions()
{
    IncludeNamespace = ["MyLittleContentEngine"],
    ExcludedNamespace = ["MyLittleContentEngine.Tests"],
});

var app = builder.Build();
app.UseAntiforgery();
app.UseStaticFiles();
app.MapRazorComponents<App>();
app.UseMonorailCss();

await app.RunOrBuildContent(args);