extern alias BlogExample;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.BlogSite;

namespace MyLittleContentEngine.IntegrationTests.ExampleProjects;

public class BlogExampleWebApplicationFactory : WebApplicationFactory<BlogExample::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        // Set the content root to the blog example project directory
        var exampleProjectPath = Path.Combine(CurrentFilePath.GetUnitTestProjectRoot(), 
            "..", "..",  "examples", "BlogExample");
        
        builder.UseContentRoot(exampleProjectPath);
        
        // Override content path configuration
        builder.ConfigureServices(services =>
        {
            // Find and replace BlogSiteOptions
            var blogOptionsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(BlogSiteOptions));
            if (blogOptionsDescriptor != null)
            {
                services.Remove(blogOptionsDescriptor);
                services.AddTransient<BlogSiteOptions>(serviceProvider =>
                {
                    var originalFactory = (Func<IServiceProvider, BlogSiteOptions>)blogOptionsDescriptor.ImplementationFactory!;
                    var originalOptions = originalFactory(serviceProvider);
                    
                    return originalOptions with
                    {
                        ContentRootPath = Path.Combine(exampleProjectPath, "Content")
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