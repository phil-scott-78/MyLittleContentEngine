using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.DocSite;

namespace MyLittleContentEngine.IntegrationTests.DocsSite;

public class DocsWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        // Set the content root to the docs project directory
        var docsProjectPath = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(), 
            "..", "..", "..", "..", "..", "docs", "MyLittleContentEngine.Docs"));
        
        builder.UseContentRoot(docsProjectPath);
        
        // Override the DocSite configuration to use the correct content path
        builder.ConfigureServices(services =>
        {
            // Find and replace the existing DocSiteOptions registration
            var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DocSiteOptions));
            if (descriptor != null)
            {
                services.Remove(descriptor);

                // Re-add with modified factory that overrides paths
                services.AddTransient<DocSiteOptions>(serviceProvider =>
                {
                    // Create the original options using the original factory
                    var originalFactory = (Func<IServiceProvider, DocSiteOptions>)descriptor.ImplementationFactory!;
                    var originalOptions = originalFactory(serviceProvider);

                    return originalOptions with
                    {
                        // Override the content root path to point to the test docs project
                        ContentRootPath = Path.Combine(docsProjectPath, "Content"),
                        SolutionPath = Path.Combine(docsProjectPath, "..", "..", "MyLittleContentEngine.slnx")
                    };
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