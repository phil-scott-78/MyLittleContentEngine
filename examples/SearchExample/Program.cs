using MonorailCss;
using MyLittleContentEngine.DocSite;
using MyLittleContentEngine.Services.Content;
using SearchExample.Services;
using Random = SearchExample.Services.Random;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDocSite(_ => new DocSiteOptions()
{
    // Basic site information
    SiteTitle = "Random Content Site",
    Description = "Random content site for demonstration purposes.",
    CanonicalBaseUrl = "https://mydocs.example.com",
    BaseUrl = "/",

    // Styling and branding
    PrimaryHue = 235, // Blue theme (0-360)
    BaseColorName = ColorNames.Slate, // Base color palette
    AdditionalRoutingAssemblies = [typeof(Random).Assembly]
});

builder.Services.AddSingleton<RandomContentService>();

// Register as IContentService (this allows multiple IContentService implementations)
builder.Services.AddSingleton<IContentService>(provider => provider.GetRequiredService<RandomContentService>());

var app = builder.Build();
app.MapGet("/debug/routes", (IEnumerable<EndpointDataSource> endpointSources) =>
    string.Join("\n", endpointSources.SelectMany(source => source.Endpoints)));
app.UseDocSite();
await app.RunDocSiteAsync(args);
