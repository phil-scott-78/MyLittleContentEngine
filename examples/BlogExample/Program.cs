using BlogExample;
using MonorailCss;
using MyLittleContentEngine.BlogSite;
using MyLittleContentEngine.Services.Content;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddBlogSite(_ => new BlogSiteOptions
{
    SiteTitle = "Calvin's Chewing Chronicles",
    Description = "A sophisticated publication for the serious gum enthusiast",
    BaseUrl = Environment.GetEnvironmentVariable("BaseHref") ?? "/",
    CanonicalBaseUrl = Environment.GetEnvironmentVariable("CanonicalBaseHref") ?? "https://calvins-chewing-chronicles.example.com",
    PrimaryHue = 300,
    BaseColorName = ColorNames.Zinc,
    AdditionalRoutingAssemblies = [typeof(Program).Assembly],
    ContentRootPath = "Content",
    BlogContentPath = "Blog",
    BlogBaseUrl = "/blog",
    TagsPageUrl = "/tags",
    DisplayFontFamily = "\"Noto Sans Display\", sans-serif",
    BodyFontFamily = "\"Inter\", sans-serif",
    EnableSocialImages = true,
    EnableRss = true,
    EnableSitemap = true,
    AdditionalHtmlHeadContent = """
                                <link rel="preconnect" href="https://fonts.googleapis.com">
                                <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
                                <link href="https://fonts.googleapis.com/css2?family=Inter:ital,opsz,wght@0,14..32,100..900;1,14..32,100..900&family=Noto+Sans+Display:ital,wght@0,100..900;1,100..900&display=swap" rel="stylesheet">

                                """,
    // Custom hero content
    HeroContent = BlogSnippets.HeroContent,
    HomeSidebarContent = BlogSnippets.Work,
    MainSiteLinks =
    [
        new HeaderLink("About", "/about"),
        new HeaderLink("Sponsor Me", "https://github.com/fake-sponsor-link")
    ]
    

});

Console.WriteLine("Here we go!");

var app = builder.Build();
app.UseBlogSite();
app.MapStaticAssets();

await app.RunBlogSiteAsync(args);
