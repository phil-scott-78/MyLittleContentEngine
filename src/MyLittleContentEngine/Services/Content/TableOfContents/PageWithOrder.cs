namespace MyLittleContentEngine.Services.Content.TableOfContents;

internal record PageWithOrder(string PageTitle, string Url, int Order, string[] HierarchyParts);