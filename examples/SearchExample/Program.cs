using MonorailCss;
using MyLittleContentEngine.DocSite;
using MyLittleContentEngine.Services.Content;
using SearchExample.Services;
using Random = SearchExample.Services.Random;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDocSite(options =>
{
    // Basic site information
    options.SiteTitle = "Random Content Site";
    options.Description = "Random content site for demonstration purposes.";
    options.CanonicalBaseUrl = "https://mydocs.example.com";
    
    // Styling and branding
    options.PrimaryHue = 235; // Blue theme (0-360)
    options.BaseColorName = ColorNames.Slate; // Base color palette
    options.AdditionalRoutingAssemblies = [typeof(Random).Assembly];
});

builder.Services.AddSingleton<RandomContentService>();

// Register as IContentService (this allows multiple IContentService implementations)
builder.Services.AddSingleton<IContentService>(provider => provider.GetRequiredService<RandomContentService>());

var app = builder.Build();
app.MapGet("/debug/routes", (IEnumerable<EndpointDataSource> endpointSources) =>
    string.Join("\n", endpointSources.SelectMany(source => source.Endpoints)));
app.UseDocSite();
await app.RunDocSiteAsync(args);
