using MonorailCss;
using MyLittleContentEngine.DocSite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDocSite(options =>
{
    options.SiteTitle = "My Little Content Engine";
    options.Description = "An Inflexible Content Engine for .NET";
    options.PrimaryHue = 235;
    options.BaseColorName = ColorNames.Zinc;
    options.GitHubUrl = "https://github.com/phil-scott-78/MyLittleContentEngine";
    options.BaseUrl = Environment.GetEnvironmentVariable("BaseUrl") ?? "/";
    options.CanonicalBaseUrl = Environment.GetEnvironmentVariable("CanonicalBaseUrl") ?? "https://phil-scott-78.github.io/MyLittleContentEngine/";
    
    // Enable API documentation
    options.SolutionPath = "../../MyLittleContentEngine.sln";
    options.IncludeNamespaces = ["MyLittleContentEngine"];
    options.ExcludeNamespaces = ["MyLittleContentEngine.Tests"];
});

var app = builder.Build();
app.UseDocSite();

await app.RunDocSiteAsync(args);