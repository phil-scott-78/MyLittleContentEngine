﻿using Mdazor;
using MonorailCss;
using MyLittleContentEngine;
using MyLittleContentEngine.Docs;
using MyLittleContentEngine.Docs.Components;
using MyLittleContentEngine.Docs.Components.Markdown;
using MyLittleContentEngine.MonorailCss;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;
using MyLittleContentEngine.Services.Content.MarkdigExtensions.Tabs;
using MyLittleContentEngine.Services.Content.Roslyn;
using MyLittleContentEngine.UI.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

builder.Services.AddMdazor()
    .AddMdazorComponent<Columns>()
    .AddMdazorComponent<Column>()
    .AddMdazorComponent<Step>()
    .AddMdazorComponent<Steps>();

// configures site wide settings
// hot reload note - these will not be reflected until the application restarts
builder.Services.AddContentEngineService(_ => new ContentEngineOptions
{
    SiteTitle = "My Little Content Engine",
    SiteDescription = "An Inflexible Content Engine for .NET",
    BaseUrl =  Environment.GetEnvironmentVariable("BaseHref") ?? "/",
    ContentRootPath = "Content",
});

// configures individual sections of the blog. PageUrl should match the configured razor pages route,
// and contentPath should match the location on disk.
// you can have multiple of these per site.
builder.Services.AddContentEngineStaticContentService(_ => new ContentEngineContentOptions<DocsFrontMatter>()
{
    ContentPath = "Content",
    BasePageUrl = string.Empty,
    ExcludeSubfolders = false
});

builder.Services.AddMonorailCss(_ => new MonorailCssOptions
{
    PrimaryHue = () => 210,
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
        }
    }
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