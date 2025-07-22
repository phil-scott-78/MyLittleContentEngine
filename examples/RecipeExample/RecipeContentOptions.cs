using MyLittleContentEngine;

namespace RecipeExample;

public class RecipeContentOptions : IContentOptions
{
    public string RecipePath { get; set; } = "recipes";
    public string FilePattern { get; set; } = "*.cook";
    public string ContentPath { get; init; } = "recipes";
    public string BasePageUrl { get; init; } = "/recipes";
}