using System.Text;
using Microsoft.AspNetCore.Http;

namespace MyLittleContentEngine.Services.Infrastructure;

/// <summary>
/// Captures the response body once and runs all registered <see cref="IResponseProcessor"/>
/// implementations in order. This replaces multiple body-capturing middlewares with a single
/// capture point, improving performance and extensibility.
/// </summary>
public class ResponseProcessingMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Captures the response body and runs all registered processors in order.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IEnumerable<IResponseProcessor> processors)
    {
        var originalBodyStream = context.Response.Body;

        try
        {
            await using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            await next(context);

            // Find processors that want to handle this response
            var applicable = processors
                .Where(p => p.ShouldProcess(context))
                .OrderBy(p => p.Order)
                .ToList();

            if (applicable.Count > 0)
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                var body = await new StreamReader(memoryStream).ReadToEndAsync();

                foreach (var processor in applicable)
                {
                    body = await processor.ProcessAsync(body, context);
                }

                var bytes = Encoding.UTF8.GetBytes(body);
                // Processors may change the body length, and other middlewares
                // (e.g. WordBreakMiddleware) may modify it further downstream.
                // Clear Content-Length so Kestrel uses chunked encoding instead
                // of enforcing a byte count that will be stale.
                context.Response.ContentLength = null;
                await originalBodyStream.WriteAsync(bytes);
            }
            else
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(originalBodyStream);
            }
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}
