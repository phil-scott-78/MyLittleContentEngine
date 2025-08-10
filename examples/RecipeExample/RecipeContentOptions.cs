using MyLittleContentEngine;
using MyLittleContentEngine.Services;

namespace RecipeExample;

public record RecipeContentOptions : IContentOptions
{
    public string RecipePath { get; set; } = "recipes";
    public string FilePattern { get; set; } = "*.cook";
    public FilePath ContentPath { get; init; } = new FilePath("recipes");
    public UrlPath BasePageUrl { get; init; } = new UrlPath("/recipes");
}