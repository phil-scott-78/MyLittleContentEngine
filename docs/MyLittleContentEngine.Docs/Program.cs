using MonorailCss;
using MyLittleContentEngine.DocSite;
using WordbreakMiddleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDocSite(_ => new DocSiteOptions(args)
{
    SiteTitle = "My Little Content Engine",
    Description = "An Inflexible Content Engine for .NET",
    SocialImageUrl = "/social.png",
    PrimaryHue = 235,
    BaseColorName = ColorNames.Zinc,
    GitHubUrl = "https://github.com/phil-scott-78/MyLittleContentEngine",
    CanonicalBaseUrl = Environment.GetEnvironmentVariable("CanonicalBaseUrl") ?? "https://phil-scott-78.github.io/MyLittleContentEngine/",
    SolutionPath = "../../MyLittleContentEngine.sln",
    IncludeNamespaces = ["MyLittleContentEngine"],
    ExcludeNamespaces = ["MyLittleContentEngine.Tests"],
    DisplayFontFamily = "Lexend, sans-serif",
    BodyFontFamily = "Inter, sans-serif",
    ExtraStyles = """
                  @font-face {
                    font-family: 'Lexend';
                    font-style: normal;
                    font-weight: 100 900;
                    font-display: swap;
                    src: url(fonts/lexend.woff2) format('woff2');
                    unicode-range: U+0000-00FF, U+0131, U+0152-0153, U+02BB-02BC, U+02C6, U+02DA, U+02DC, U+0304, U+0308, U+0329, U+2000-206F, U+20AC, U+2122, U+2191, U+2193, U+2212, U+2215, U+FEFF, U+FFFD;
                  }
                  
                  @font-face {
                    font-family: 'Inter';
                    font-style: normal;
                    font-weight: 100 900;
                    font-display: swap;
                    src: url(fonts/inter.woff2) format('woff2');
                    unicode-range: U+0000-00FF, U+0131, U+0152-0153, U+02BB-02BC, U+02C6, U+02DA, U+02DC, U+0304, U+0308, U+0329, U+2000-206F, U+20AC, U+2122, U+2191, U+2193, U+2212, U+2215, U+FEFF, U+FFFD;
                  }
                  """
    
});

var app = builder.Build();
app.UseDocSite();
app.UseWordBreak();

await app.RunDocSiteAsync(args);