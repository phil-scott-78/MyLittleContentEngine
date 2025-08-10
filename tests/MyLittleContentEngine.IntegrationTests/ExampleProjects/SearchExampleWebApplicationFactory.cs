extern alias SearchExample;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.DocSite;
using MyLittleContentEngine.Services.Infrastructure;
using MyLittleContentEngine.IntegrationTests.Infrastructure;

namespace MyLittleContentEngine.IntegrationTests.ExampleProjects;

public class SearchExampleWebApplicationFactory : WebApplicationFactory<SearchExample::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        // Set the content root to the example project directory
        var exampleProjectPath = Path.Combine(CurrentFilePath.GetUnitTestProjectRoot(), 
            "..", "..",  "examples", "SearchExample");
        
        builder.UseContentRoot(exampleProjectPath);
        
        // Override DocSiteOptions configuration and ILocalHttpClient
        builder.ConfigureServices(services =>
        {
            // Find and replace DocSiteOptions
            var docSiteOptionsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DocSiteOptions));
            if (docSiteOptionsDescriptor != null)
            {
                services.Remove(docSiteOptionsDescriptor);
                services.AddTransient<DocSiteOptions>(serviceProvider =>
                {
                    var originalFactory = (Func<IServiceProvider, DocSiteOptions>)docSiteOptionsDescriptor.ImplementationFactory!;
                    var originalOptions = originalFactory(serviceProvider);
                    
                    return originalOptions with
                    {
                        ContentRootPath = Path.Combine(exampleProjectPath, "Content")
                    };
                });
            }

            // Replace ILocalHttpClient with TestServer implementation
            var localHttpClientDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ILocalHttpClient));
            if (localHttpClientDescriptor != null)
            {
                services.Remove(localHttpClientDescriptor);
                services.AddTransient<ILocalHttpClient>(_ =>
                {
                    var httpClient = CreateClient();
                    return new TestServerLocalHttpClient(httpClient);
                });
            }
        });
        
        // Reduce logging noise in tests
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });
    }
}