using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Spa;
using SpaNavigationExample.Slots.Components;

namespace SpaNavigationExample.Slots;

/// <summary>
/// Custom slot renderer that produces a recipe metadata card for the sidebar.
/// Demonstrates a Razor-based slot renderer using <see cref="RazorIslandRenderer{TComponent}"/>
/// to delegate presentation to a <c>.razor</c> component.
/// </summary>
public class RecipeInfoSlotRenderer(
    IMarkdownContentService<RecipeFrontMatter> contentService,
    ComponentRenderer renderer) : RazorIslandRenderer<RecipeInfoCard>(renderer)
{
    public override string IslandName => "recipe-info";

    protected override async Task<IDictionary<string, object?>?> BuildParametersAsync(string url)
    {
        var result = await contentService.GetRenderedContentPageByUrlOrDefault(url);
        if (result is null) return null;

        var fm = result.Value.Page.FrontMatter;

        // Index page has no recipe metadata.
        if (fm is { PrepTime: 0, CookTime: 0, Servings: 0 })
            return null;

        return new Dictionary<string, object?>
        {
            [nameof(RecipeInfoCard.PrepTime)] = fm.PrepTime,
            [nameof(RecipeInfoCard.CookTime)] = fm.CookTime,
            [nameof(RecipeInfoCard.Servings)] = fm.Servings,
            [nameof(RecipeInfoCard.Difficulty)] = fm.Difficulty,
            [nameof(RecipeInfoCard.Tags)] = fm.Tags,
        };
    }
}
