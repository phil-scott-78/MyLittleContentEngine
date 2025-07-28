extern alias RecipeExample;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine;
using RecipeExample::RecipeExample;

namespace MyLittleContentEngine.IntegrationTests.ExampleProjects;

public class RecipeExampleWebApplicationFactory : WebApplicationFactory<RecipeExample::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        // Set the content root to the example project directory
        var exampleProjectPath = Path.Combine(CurrentFilePath.GetUnitTestProjectRoot(), 
            "..", "..",  "examples", "RecipeExample");
        
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
                        ContentRootPath = Path.Combine(exampleProjectPath, "recipes")
                    };
                });
            }
            
            var recipeOptionsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(RecipeContentOptions));
            if (recipeOptionsDescriptor != null)
            {
                services.Remove(recipeOptionsDescriptor);
                services.AddTransient<RecipeContentOptions>(serviceProvider =>
                {
                    var originalOptions = (RecipeContentOptions) recipeOptionsDescriptor.ImplementationInstance!;
                    
                    return originalOptions with
                    {
                        RecipePath = Path.Combine(exampleProjectPath, "recipes"),
                        ContentPath = Path.Combine(exampleProjectPath, "recipes")
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