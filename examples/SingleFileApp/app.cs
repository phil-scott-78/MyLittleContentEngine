#:sdk Microsoft.NET.Sdk.Web
#:package MyLittleContentEngine.DocSite@0.0.0-alpha.0.62
#:package Westwind.AspNetCore.LiveReload@0.5.2

using MonorailCss;
using MyLittleContentEngine.DocSite;
using Westwind.AspNetCore.LiveReload;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLiveReload(config =>
{ 
    config.ClientFileExtensions =  ".css,.js,.md";
    config.FolderToMonitor = Path.Combine(Directory.GetCurrentDirectory(), "Content");
});
builder.Services.AddDocSite(options =>
{
    options.SiteTitle = "My Little Content Engine";
    options.Description = "An Inflexible Content Engine for .NET";
    options.PrimaryHue = 235;
    options.BaseColorName = ColorNames.Zinc;
    options.GitHubUrl = "https://github.com/phil-scott-78/MyLittleContentEngine";
    options.BaseUrl = Environment.GetEnvironmentVariable("BaseUrl") ?? "/";
    options.CanonicalBaseUrl = Environment.GetEnvironmentVariable("CanonicalBaseUrl") ?? "https://phil-scott-78.github.io/MyLittleContentEngine/";
});

var app = builder.Build();
app.UseLiveReload();
app.UseDocSite();

await app.RunDocSiteAsync(args);


