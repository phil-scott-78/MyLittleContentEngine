using System.Collections.Immutable;
using MyLittleContentEngine;
using MyLittleContentEngine.DocSite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDocSite(_ => new DocSiteOptions
{
    SiteTitle = "The Multilingual Tavern",
    Description = "A documentation site that speaks many tongues",
    ContentRootPath = "Content",
    Localization = new LocalizationOptions
    {
        DefaultLocale = "en",
        Locales = ImmutableDictionary<string, LocaleInfo>.Empty
            .Add("en", new LocaleInfo("English"))
            .Add("pl", new LocaleInfo("Pig Latin"))
            .Add("sv", new LocaleInfo("Bork Bork", HtmlLang: "sv-chef"))
            .Add("pi", new LocaleInfo("Pirate", HtmlLang: "en-pirate"))
            .Add("kl", new LocaleInfo("Klingon", HtmlLang: "tlh"))
    },
});

var app = builder.Build();
app.UseDocSite();
await app.RunDocSiteAsync(args);
