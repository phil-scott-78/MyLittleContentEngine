using MonorailCss;
using MyLittleContentEngine.DocSite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDocSite(_ => new DocSiteOptions()
{
    SiteTitle = "My Little Content Engine",
    Description = "An Inflexible Content Engine for .NET",
    PrimaryHue = 235,
    BaseColorName = ColorNames.Zinc,
    GitHubUrl = "https://github.com/phil-scott-78/MyLittleContentEngine",
    BaseUrl = Environment.GetEnvironmentVariable("BaseUrl") ?? "/", 
    CanonicalBaseUrl = Environment.GetEnvironmentVariable("CanonicalBaseUrl") ?? "https://phil-scott-78.github.io/MyLittleContentEngine/",
    SolutionPath = "../../MyLittleContentEngine.sln",
    IncludeNamespaces = ["MyLittleContentEngine"],
    ExcludeNamespaces = ["MyLittleContentEngine.Tests"]
});

var app = builder.Build();
app.UseDocSite();

await app.RunDocSiteAsync(args);