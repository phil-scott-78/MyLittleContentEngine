namespace MyLittleContentEngine.Services.Content.TableOfContents;

/// <summary>
/// Represents a Table of Contents entry with hierarchy parts for building navigation structure
/// </summary>
public record ContentTocItem(string Title, string Url, int Order, string[] HierarchyParts);