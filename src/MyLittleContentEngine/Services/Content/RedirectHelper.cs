namespace MyLittleContentEngine.Services.Content;

internal static class RedirectHelper
{
    public static string GetRedirectHtml(string redirectUrl)
    {
        return $"""
                <!DOCTYPE html>
                <html lang="en">
                  <head>
                    <meta charset="utf-8">
                    <meta http-equiv="refresh" content="0; URL='{redirectUrl}'">
                    <title>Redirecting...</title>
                    <meta name="robots" content="noindex">
                  </head>
                  <body>
                    <p>If you are not redirected automatically, <a href="{redirectUrl}">click here</a>.</p>
                  </body>
                </html>
                """;
    }
}