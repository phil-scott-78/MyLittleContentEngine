using CooklangSharp.Models;

namespace RecipeExample.Models;

public class RecipeContentPage
{
    public RecipeContentPage(
        Recipe recipe, 
        RecipeFrontMatter frontMatter,
        string fileName,
        string url,
        string originalContent)
    {
        Recipe = recipe;
        FrontMatter = frontMatter;
        FileName = fileName;
        Url = url;
        OriginalContent = originalContent;
    }

    /// <summary>
    /// The parsed CookLang recipe
    /// </summary>
    public Recipe Recipe { get; }

    /// <summary>
    /// The parsed front matter metadata
    /// </summary>
    public RecipeFrontMatter FrontMatter { get; }

    /// <summary>
    /// The original filename without extension
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// The URL path for this recipe
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// The original file content
    /// </summary>
    public string OriginalContent { get; }

    /// <summary>
    /// Gets the display name for the recipe, preferring title from front matter, then filename
    /// </summary>
    public string DisplayName => 
        !string.IsNullOrWhiteSpace(FrontMatter.Title) ? FrontMatter.Title :
        !string.IsNullOrWhiteSpace(FileName) ? FileName.Replace("-", " ").Replace("_", " ") :
        "Unknown Recipe";

    /// <summary>
    /// Gets a URL-friendly slug for the recipe
    /// </summary>
    public string Slug => FileName.ToLowerInvariant();
}