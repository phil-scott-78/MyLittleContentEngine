extern alias MultipleContentSourceExample;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine;
using MultipleContentSourceExample::MultipleContentSourceExample;

namespace MyLittleContentEngine.IntegrationTests.ExampleProjects;

public class MultipleContentSourceExampleWebApplicationFactory : WebApplicationFactory<MultipleContentSourceExample::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        // Set the content root to the example project directory
        var exampleProjectPath = Path.Combine(CurrentFilePath.GetUnitTestProjectRoot(), 
            "..", "..",  "examples", "MultipleContentSourceExample");
        
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
            
            // Override ContentEngineContentOptions services (only public types accessible)
            var contentOptionsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MarkdownContentOptions<ContentFrontMatter>));
            if (contentOptionsDescriptor != null)
            {
                services.Remove(contentOptionsDescriptor);
                services.AddTransient<MarkdownContentOptions<ContentFrontMatter>>(serviceProvider =>
                {
                    var originalFactory = (Func<IServiceProvider, MarkdownContentOptions<ContentFrontMatter>>)contentOptionsDescriptor.ImplementationFactory!;
                    var originalOptions = originalFactory(serviceProvider);
                    
                    return originalOptions with
                    {
                        ContentPath = Path.Combine(exampleProjectPath, "Content")
                    };
                });
            }
            
            var blogOptionsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MarkdownContentOptions<BlogFrontMatter>));
            if (blogOptionsDescriptor != null)
            {
                services.Remove(blogOptionsDescriptor);
                services.AddTransient<MarkdownContentOptions<BlogFrontMatter>>(serviceProvider =>
                {
                    var originalFactory = (Func<IServiceProvider, MarkdownContentOptions<BlogFrontMatter>>)blogOptionsDescriptor.ImplementationFactory!;
                    var originalOptions = originalFactory(serviceProvider);
                    
                    return originalOptions with
                    {
                        ContentPath = Path.Combine(exampleProjectPath, "Content", "blog")
                    };
                });
            }
            
            var docOptionsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MarkdownContentOptions<DocsFrontMatter>));
            if (docOptionsDescriptor != null)
            {
                services.Remove(docOptionsDescriptor);
                services.AddTransient<MarkdownContentOptions<DocsFrontMatter>>(serviceProvider =>
                {
                    var originalFactory = (Func<IServiceProvider, MarkdownContentOptions<DocsFrontMatter>>)docOptionsDescriptor.ImplementationFactory!;
                    var originalOptions = originalFactory(serviceProvider);
                    
                    return originalOptions with
                    {
                        ContentPath = Path.Combine(exampleProjectPath, "Content", "docs")
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