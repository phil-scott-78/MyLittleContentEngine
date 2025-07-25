using System.Text;
using Microsoft.AspNetCore.Http;
using MyLittleContentEngine.Services.Content;

namespace MyLittleContentEngine.Services.Infrastructure;

/// <summary>
/// Middleware that processes HTML responses to insert word break opportunities in long technical terms.
/// </summary>
public class WordBreakingMiddleware(RequestDelegate next, WordBreakingMiddlewareOptions options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if we should process this request
        if (!ShouldProcessRequest(context))
        {
            await next(context);
            return;
        }

        // Capture the original response body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            // Replace the response body stream with a memory stream
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            // Call the next middleware
            await next(context);

            // Process the response if it's HTML
            if (ShouldProcessResponse(context))
            {
                await ProcessHtmlResponse(context, responseBodyStream, originalBodyStream);
            }
            else
            {
                // Copy the response as-is
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);
            }
        }
        finally
        {
            // Restore the original response body stream
            context.Response.Body = originalBodyStream;
        }
    }

    private bool ShouldProcessRequest(HttpContext context)
    {
        // Don't process if disabled
        if (!options.Enabled)
            return false;

        // Only process GET requests
        if (context.Request.Method != HttpMethods.Get)
            return false;

        // Check custom predicate
        return options.ShouldProcessRequest?.Invoke(context) ?? true;
    }

    private static bool ShouldProcessResponse(HttpContext context)
    {
        // Only process successful responses
        if (context.Response.StatusCode is < 200 or >= 300)
            return false;

        // Only process HTML content
        var contentType = context.Response.ContentType;
        return !string.IsNullOrEmpty(contentType) &&
               contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private async Task ProcessHtmlResponse(HttpContext context, MemoryStream responseBodyStream,
        Stream originalBodyStream)
    {
        responseBodyStream.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(responseBodyStream, Encoding.UTF8);
        var html = await reader.ReadToEndAsync();

        // Process the HTML content
        var processedHtml = ProcessHtmlContent(html);

        // Write the processed content back
        var processedBytes = Encoding.UTF8.GetBytes(processedHtml);

        // Update content length if it was set
        if (context.Response.Headers.ContentLength.HasValue)
        {
            context.Response.Headers.ContentLength = processedBytes.Length;
        }

        await originalBodyStream.WriteAsync(processedBytes);
    }

    private string ProcessHtmlContent(string html)
    {
        if (string.IsNullOrEmpty(html))
            return html;

        // Simple HTML text processor - we'll process text nodes while preserving HTML structure
        var result = new StringBuilder(html.Length * 2);
        var inTag = false;
        var inScript = false;
        var inStyle = false;
        var inHead = false;
        var inPre = false;
        var inCode = false;
        var currentWord = new StringBuilder();

        for (var i = 0; i < html.Length; i++)
        {
            var c = html[i];

            // Track if we're inside tags, scripts, styles, head, pre, or code elements
            if (c == '<')
            {
                // Process any accumulated word before starting tag
                if (currentWord.Length > 0)
                {
                    var word = currentWord.ToString();
                    result.Append(WordBreaker.ShouldInsertWordBreaks(word, options.MinWordLength)
                        ? WordBreaker.InsertWordBreaks(word)
                        : word);
                    currentWord.Clear();
                }

                inTag = true;
                result.Append(c);

                // Check for script, style, head, pre, or code tags
                if (i + 7 < html.Length && html.Substring(i, 7).Equals("<script", StringComparison.OrdinalIgnoreCase))
                {
                    inScript = true;
                }
                else if (i + 6 < html.Length &&
                                         html.Substring(i, 6).Equals("<style", StringComparison.OrdinalIgnoreCase))
                {
                    inStyle = true;
                }
                else if (i + 5 < html.Length && html.Substring(i, 5).Equals("<head", StringComparison.OrdinalIgnoreCase))
                {
                    inHead = true;
                }
                else if (i + 5 < html.Length && html.Substring(i, 5).Equals("<code", StringComparison.OrdinalIgnoreCase))
                {
                    inCode = true;
                }
                else if (i + 4 < html.Length && html.Substring(i, 4).Equals("<pre", StringComparison.OrdinalIgnoreCase))
                {
                    inPre = true;
                }
            }
            else if (c == '>')
            {
                inTag = false;
                result.Append(c);

                // Check for closing script, style, head, pre, or code tags
                if (inScript && i >= 8 && html.Substring(i - 8, 9).Equals("</script>", StringComparison.OrdinalIgnoreCase))
                {
                    inScript = false;
                }
                else if (inStyle && i >= 7 &&
                                         html.Substring(i - 7, 8).Equals("</style>", StringComparison.OrdinalIgnoreCase))
                {
                    inStyle = false;
                }
                else if (inHead && i >= 6 && html.Substring(i - 6, 7).Equals("</head>", StringComparison.OrdinalIgnoreCase))
                {
                    inHead = false;
                }
                else if (inCode && i >= 6 && html.Substring(i - 6, 7).Equals("</code>", StringComparison.OrdinalIgnoreCase))
                {
                    inCode = false;
                }
                else if (inPre && i >= 5 && html.Substring(i - 5, 6).Equals("</pre>", StringComparison.OrdinalIgnoreCase))
                {
                    inPre = false;
                }
            }
            else if (inTag || inScript || inStyle || inHead || inPre || inCode)
            {
                // Inside tags, scripts, styles, head, pre, or code - don't process
                result.Append(c);
            }
            else if (char.IsWhiteSpace(c))
            {
                // End of word - process it
                if (currentWord.Length > 0)
                {
                    var word = currentWord.ToString();
                    result.Append(WordBreaker.ShouldInsertWordBreaks(word, options.MinWordLength)
                        ? WordBreaker.InsertWordBreaks(word)
                        : word);
                    currentWord.Clear();
                }

                result.Append(c);
            }
            else
            {
                // Building a word
                currentWord.Append(c);
            }
        }

        // Process any remaining word
        if (currentWord.Length > 0)
        {
            var word = currentWord.ToString();
            result.Append(WordBreaker.ShouldInsertWordBreaks(word, options.MinWordLength)
                ? WordBreaker.InsertWordBreaks(word)
                : word);
        }

        return result.ToString();
    }
}

/// <summary>
/// Configuration options for the WordBreakingMiddleware.
/// </summary>
public class WordBreakingMiddlewareOptions
{
    /// <summary>
    /// Gets or sets whether the middleware is enabled. Default is true.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets or sets the minimum word length before considering word breaks. Default is 15.
    /// </summary>
    public int MinWordLength { get; init; } = 15;

    /// <summary>
    /// Gets or sets a custom predicate to determine if a request should be processed.
    /// If null, all eligible requests will be processed.
    /// </summary>
    public Func<HttpContext, bool>? ShouldProcessRequest { get; init; }
}