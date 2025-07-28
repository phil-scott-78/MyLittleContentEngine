extern alias MinimalExample;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalExample::MinimalExample;
using MyLittleContentEngine;
using MyLittleContentEngine.BlogSite;

namespace MyLittleContentEngine.IntegrationTests.ExampleProjects;

public class MinimalExampleWebApplicationFactory : WebApplicationFactory<MinimalExample::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        // Set the content root to the minimal example project directory
        var exampleProjectPath = Path.Combine(CurrentFilePath.GetUnitTestProjectRoot(), "..", "..",  "examples", "MinimalExample");
        
        builder.UseContentRoot(exampleProjectPath);
        
        // Override content path configuration
        builder.ConfigureServices(services =>
        {
            // Find and replace ContentEngineOptions
            var engineOptionsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ContentEngineOptions));
            if (engineOptionsDescriptor != null)
            {
                services.Remove(engineOptionsDescriptor);
                services.AddTransient<ContentEngineOptions>(serviceProvider =>
                {
                    var originalFactory = (Func<IServiceProvider, ContentEngineOptions>)engineOptionsDescriptor.ImplementationFactory!;
                    var originalOptions = originalFactory(serviceProvider);
                    
                    return originalOptions with
                    {
                        ContentRootPath = Path.Combine(exampleProjectPath, "Content")
                    };
                });
            }
            
            // Find and replace ContentEngineOptions
            var contentOptionsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ContentEngineContentOptions<BlogFrontMatter>));
            if (contentOptionsDescriptor != null)
            {
                services.Remove(contentOptionsDescriptor);
                services.AddTransient<ContentEngineContentOptions<BlogFrontMatter>>(serviceProvider =>
                {
                    var originalFactory = (Func<IServiceProvider, ContentEngineContentOptions<BlogFrontMatter>>)contentOptionsDescriptor.ImplementationFactory!;
                    var originalOptions = originalFactory(serviceProvider);
                    
                    return originalOptions with
                    {
                        ContentPath = Path.Combine(exampleProjectPath, "Content")
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