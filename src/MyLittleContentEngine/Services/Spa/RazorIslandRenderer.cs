using Microsoft.AspNetCore.Components;

namespace MyLittleContentEngine.Services.Spa;

/// <summary>
/// Base class for island renderers that delegate presentation to a Razor component.
/// Subclasses provide the island name and build parameters; the base class
/// handles rendering via <see cref="ComponentRenderer"/>.
/// </summary>
/// <typeparam name="TComponent">The Razor component type to render for this island.</typeparam>
/// <param name="renderer">The scoped component renderer.</param>
public abstract class RazorIslandRenderer<TComponent>(
    ComponentRenderer renderer) : ISpaIslandRenderer
    where TComponent : IComponent
{
    /// <inheritdoc />
    public abstract string IslandName { get; }

    /// <summary>
    /// Builds the parameter dictionary for the component, or returns <c>null</c>
    /// to indicate this island has no content for the given URL.
    /// </summary>
    /// <param name="url">The content page URL (e.g. "/" for root, "/pasta-carbonara").</param>
    /// <returns>A parameter dictionary, or null to skip this island.</returns>
    protected abstract Task<IDictionary<string, object?>?> BuildParametersAsync(string url);

    /// <inheritdoc />
    public async Task<string?> RenderAsync(string url)
    {
        var parameters = await BuildParametersAsync(url);
        if (parameters is null)
            return null;

        return await renderer.RenderComponentAsync<TComponent>(parameters);
    }
}
